using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System.Numerics;

namespace Lucene.Net.Documents
{
	/// <summary>
	/// Indexes fixed-width sortable bytes as a prefix-coded trie (multi-value safe range/exact)
	/// and stores optional raw payloads. Sorting uses a separate single-value SortedDocValues field.
	/// </summary>
	public static class SortableBytesField
	{
		/// <summary>Suffix for the single-value sort field (reserved; avoid colliding user field names).</summary>
		public const string SortFieldSuffix = "_$Sort";

		/// <summary>Suffix for the exists marker field (reserved; avoid colliding user field names).</summary>
		public const string ExistsFieldSuffix = "_$Exists";

		/// <summary>Indexed marker term used by <see cref="NewExistsQuery"/>.</summary>
		public const string ExistsMarker = "1";

		private static readonly FieldType IndexedType = CreateIndexedType();
		private static readonly FieldType ExistsType = CreateExistsType();

		private static FieldType CreateIndexedType()
		{
			FieldType type = new FieldType
			{
				IsIndexed = true,
				IsStored = false,
				IsTokenized = true,
				OmitNorms = true,
				IndexOptions = IndexOptions.DOCS_ONLY,
			};
			type.Freeze();
			return type;
		}

		private static FieldType CreateExistsType()
		{
			FieldType type = new FieldType(StringField.TYPE_NOT_STORED)
			{
				OmitNorms = true,
				IndexOptions = IndexOptions.DOCS_ONLY,
			};
			type.Freeze();
			return type;
		}

		/// <inheritdoc />
		public static string GetSortFieldName(string name)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return name + SortFieldSuffix;
		}

		/// <inheritdoc />
		public static string GetExistsFieldName(string name)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return name + ExistsFieldSuffix;
		}

		/// <summary>
		/// Creates indexed trie terms, an exists marker, and optional stored payload for search/round-trip.
		/// Does not write the sort field ??call <see cref="CreateSortValueFields"/> separately.
		/// </summary>
		/// <remarks>
		/// <paramref name="storedBytes"/> may differ in length from <paramref name="indexedBytes"/>
		/// (e.g. decimal stores 16-byte GetBits while indexing 24-byte sortable bytes).
		/// When provided, <paramref name="storedBytes"/> must be non-empty.
		/// </remarks>
		public static IEnumerable<Field> CreateFields(string name, byte[] indexedBytes, byte[]? storedBytes, Field.Store stored)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentNullException.ThrowIfNull(indexedBytes);
			if (indexedBytes.Length == 0)
				throw new ArgumentException("Indexed bytes must not be empty.", nameof(indexedBytes));
			if (storedBytes is not null && storedBytes.Length == 0)
				throw new ArgumentException("storedBytes must not be empty when provided.", nameof(storedBytes));

			if (stored == Field.Store.YES)
			{
				byte[] payload = storedBytes is { Length: > 0 } ? storedBytes : indexedBytes;
				yield return new StoredField(name, payload);
			}

			yield return new Field(GetExistsFieldName(name), ExistsMarker, ExistsType);

			int bitLength = checked(indexedBytes.Length * 8);
			SortableBytesTokenStream stream = new SortableBytesTokenStream(bitLength).SetValue(indexedBytes);
			yield return new Field(name, stream, IndexedType);
		}

		/// <inheritdoc />
		public static IEnumerable<Field> CreateFields(string name, byte[] bytes, Field.Store stored)
			=> CreateFields(name, bytes, bytes, stored);

		/// <summary>
		/// Creates a single-value SortedDocValues field used by <see cref="CreateSortField"/>.
		/// Only one sort value per document is supported for a given base name.
		/// </summary>
		public static IEnumerable<Field> CreateSortValueFields(string name, byte[] indexedBytes)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentNullException.ThrowIfNull(indexedBytes);
			if (indexedBytes.Length == 0)
				throw new ArgumentException("Indexed bytes must not be empty.", nameof(indexedBytes));

			yield return new SortedDocValuesField(GetSortFieldName(name), new BytesRef(indexedBytes));
		}

		/// <summary>
		/// String sort over the single-value sort field written by <see cref="CreateSortValueFields"/>.
		/// </summary>
		public static SortField CreateSortField(string name, bool reverse = false)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return new SortField(GetSortFieldName(name), SortFieldType.STRING, reverse);
		}

		/// <inheritdoc />
		public static Query NewExactQuery(string name, byte[] bytes)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentNullException.ThrowIfNull(bytes);
			if (bytes.Length == 0)
				throw new ArgumentException("Bytes must not be empty.", nameof(bytes));

			int bitLength = checked(bytes.Length * 8);
			BigInteger value = SortableBytesNumericUtils.ToUnsignedBigInteger(bytes);
			BytesRef term = new BytesRef();
			SortableBytesNumericUtils.ToPrefixCoded(value, shift: 0, bitLength, term);
			return new TermQuery(new Term(name, term));
		}

		/// <inheritdoc />
		public static Query NewRangeQuery(string name, byte[]? minBytes, byte[]? maxBytes, bool minInclusive = true, bool maxInclusive = true, int precisionStep = SortableBytesNumericUtils.PrecisionStepDefault)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			ArgumentOutOfRangeException.ThrowIfLessThan(precisionStep, 1);
			if (minBytes is null && maxBytes is null)
				return NewExistsQuery(name);
			if (minBytes is { Length: 0 })
				throw new ArgumentException("minBytes must not be empty.", nameof(minBytes));
			if (maxBytes is { Length: 0 })
				throw new ArgumentException("maxBytes must not be empty.", nameof(maxBytes));
			if (minBytes is not null && maxBytes is not null && minBytes.Length != maxBytes.Length)
				throw new ArgumentException("minBytes and maxBytes must have the same length.");

			int byteLength = minBytes?.Length ?? maxBytes!.Length;
			int bitLength = checked(byteLength * 8);
			BigInteger minBound = minBytes is null
				? BigInteger.Zero
				: SortableBytesNumericUtils.ToUnsignedBigInteger(minBytes);
			BigInteger maxBound = maxBytes is null
				? SortableBytesNumericUtils.MaxValue(bitLength)
				: SortableBytesNumericUtils.ToUnsignedBigInteger(maxBytes);

			if (minBytes is not null && !minInclusive && !SortableBytesNumericUtils.TryIncrement(ref minBound, bitLength))
				return new BooleanQuery();
			if (maxBytes is not null && !maxInclusive && !SortableBytesNumericUtils.TryDecrement(ref maxBound, bitLength))
				return new BooleanQuery();
			if (minBound > maxBound)
				return new BooleanQuery();

			BooleanQuery query = new() { MinimumNumberShouldMatch = 1 };
			BytesRef minTerm = new BytesRef();
			BytesRef maxTerm = new BytesRef();

			SortableBytesNumericUtils.SplitRange(bitLength, precisionStep, minBound, maxBound, (min, max, shift) =>
			{
				SortableBytesNumericUtils.ToPrefixCoded(min, shift, bitLength, minTerm);
				SortableBytesNumericUtils.ToPrefixCoded(max, shift, bitLength, maxTerm);
				query.Add(
					new TermRangeQuery(
						name,
						BytesRef.DeepCopyOf(minTerm),
						BytesRef.DeepCopyOf(maxTerm),
						includeLower: true,
						includeUpper: true),
					Occur.SHOULD);
			});

			return query;
		}

		/// <summary>
		/// Matches any document that has an exists marker for <paramref name="name"/>.
		/// </summary>
		public static Query NewExistsQuery(string name)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(name);
			return new TermQuery(new Term(GetExistsFieldName(name), ExistsMarker));
		}
	}
}
