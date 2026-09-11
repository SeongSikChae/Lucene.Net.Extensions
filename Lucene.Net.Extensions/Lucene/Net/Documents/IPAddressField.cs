using Lucene.Net.Search;
using System.Net;

namespace Lucene.Net.Documents
{
	/// <summary>
	/// Indexes IPv4/IPv6 addresses as an ordered Int64 high/low pair for filtering, sorting, and range queries.
	/// IPv4-mapped IPv6 addresses are normalized to IPv4 before storage.
	/// </summary>
	public static class IPAddressField
	{
		/// <summary>
		/// Suffix appended to the base field name for the low Int64 limb (same as <see cref="IPAddressExtensions.LowPartSuffix"/>).
		/// </summary>
		public const string LowPartSuffix = IPAddressExtensions.LowPartSuffix;

		/// <summary>
		/// Creates the high/low Int64 fields used to index <paramref name="value"/>.
		/// </summary>
		/// <param name="name">Base field name (high limb).</param>
		/// <param name="value">Address to index.</param>
		/// <param name="stored">Whether to store the values.</param>
		public static IEnumerable<Field> CreateFields(string name, IPAddress value, Field.Store stored)
		{
			value.NormalizeForStorage().ToInt64Pair(out long high, out long low);
			yield return new Int64Field(name, high, stored);
            yield return new Int64Field(name + LowPartSuffix, low, stored);
		}

		/// <summary>
		/// Creates sort fields (high → low) matching <see cref="CreateFields"/> order.
		/// </summary>
		/// <param name="name">Base field name.</param>
		/// <param name="reverse">Whether to reverse the sort order.</param>
		public static IEnumerable<SortField> CreateSortField(string name, bool reverse = false)
		{
			yield return new SortField(name, SortFieldType.INT64, reverse);
			yield return new SortField(name + LowPartSuffix, SortFieldType.INT64, reverse);
		}

		/// <summary>
		/// Exact match over the high/low Int64 pair (same as an inclusive range with equal bounds).
		/// </summary>
		public static Query NewExactQuery(string name, IPAddress value)
		{
			ArgumentNullException.ThrowIfNull(value);
			return NewRangeQuery(name, value, value);
		}

		/// <summary>
		/// Inclusive/exclusive numeric range over the high/low Int64 pair. Null bounds are open.
		/// </summary>
		public static Query NewRangeQuery(string name, IPAddress? min, IPAddress? max, bool minInclusive = true, bool maxInclusive = true)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			long startHigh;
			long startLow;
			long endHigh;
			long endLow;

			if (min is null)
			{
				startHigh = long.MinValue;
				startLow = long.MinValue;
			}
			else
			{
				min.NormalizeForStorage().ToInt64Pair(out startHigh, out startLow);
			}

			if (max is null)
			{
				endHigh = long.MaxValue;
				endLow = long.MaxValue;
			}
			else
			{
				max.NormalizeForStorage().ToInt64Pair(out endHigh, out endLow);
			}

			if (!minInclusive && min is not null)
			{
				if (!TryIncrement(ref startHigh, ref startLow))
					return new BooleanQuery();
			}

			if (!maxInclusive && max is not null)
			{
				if (!TryDecrement(ref endHigh, ref endLow))
					return new BooleanQuery();
			}

			return CreateLexicographicRangeQuery(name, name + LowPartSuffix, startHigh, startLow, endHigh, endLow);
		}

		/// <summary>
		/// Matches any document that has an IP address pair for <paramref name="name"/>.
		/// </summary>
		public static Query NewExistsQuery(string name)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			return NumericRangeQuery.NewInt64Range(
				name,
				min: null,
				max: null,
				minInclusive: true,
				maxInclusive: true);
		}

		private static bool TryIncrement(ref long high, ref long low)
		{
			if (low < long.MaxValue)
			{
				low++;
				return true;
			}

			if (high < long.MaxValue)
			{
				high++;
				low = long.MinValue;
				return true;
			}

			return false;
		}

		private static bool TryDecrement(ref long high, ref long low)
		{
			if (low > long.MinValue)
			{
				low--;
				return true;
			}

			if (high > long.MinValue)
			{
				high--;
				low = long.MaxValue;
				return true;
			}

			return false;
		}

		private static BooleanQuery CreateLexicographicRangeQuery(string highField, string lowField, long startHigh, long startLow, long endHigh, long endLow)
		{
			int cmpHigh = startHigh.CompareTo(endHigh);
			if (cmpHigh > 0)
				return [];

			if (cmpHigh == 0)
			{
				int cmpLow = startLow.CompareTo(endLow);
				if (cmpLow > 0)
					return [];

				return new BooleanQuery
				{
					{ NumericRangeQuery.NewInt64Range(highField, startHigh, startHigh, true, true), Occur.MUST },
					{ NumericRangeQuery.NewInt64Range(lowField, startLow, endLow, true, true), Occur.MUST },
				};
			}

			// startHigh < endHigh:
			//   (high == startHigh AND low >= startLow)
			//   OR (startHigh < high < endHigh)
			//   OR (high == endHigh AND low <= endLow)
			BooleanQuery result = new() { MinimumNumberShouldMatch = 1 };

			result.Add(
				new BooleanQuery
				{
					{ NumericRangeQuery.NewInt64Range(highField, startHigh, startHigh, true, true), Occur.MUST },
					{ NumericRangeQuery.NewInt64Range(lowField, startLow, long.MaxValue, true, true), Occur.MUST },
				},
				Occur.SHOULD);

			long midMin = startHigh + 1;
			long midMax = endHigh - 1;
			if (midMin <= midMax)
			{
				result.Add(
					NumericRangeQuery.NewInt64Range(highField, midMin, midMax, true, true),
					Occur.SHOULD);
			}

			result.Add(
				new BooleanQuery
				{
					{ NumericRangeQuery.NewInt64Range(highField, endHigh, endHigh, true, true), Occur.MUST },
					{ NumericRangeQuery.NewInt64Range(lowField, long.MinValue, endLow, true, true), Occur.MUST },
				},
				Occur.SHOULD);

			return result;
		}
	}
}
