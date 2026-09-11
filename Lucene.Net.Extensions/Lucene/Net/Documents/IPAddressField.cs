using Lucene.Net.Search;
using System.Net;

namespace Lucene.Net.Documents
{
	/// <summary>
	/// Indexes IPv4/IPv6 addresses as a prefix-coded sortable trie (multi-value safe).
	/// Indexed/sort encoding is always 17 bytes; stored payloads are compact (IPv4=5, IPv6=17).
	/// IPv4-mapped IPv6 addresses are normalized to IPv4 before storage; family is the leading encode byte.
	/// Sorting requires a separate single-value field via <see cref="CreateSortValueFields"/>.
	/// </summary>
	public static class IPAddressField
	{
		/// <summary>Suffix for the single-value sort field.</summary>
		public const string SortFieldSuffix = SortableBytesField.SortFieldSuffix;

		/// <summary>Suffix for the exists marker field.</summary>
		public const string ExistsFieldSuffix = SortableBytesField.ExistsFieldSuffix;

		/// <summary>
		/// Creates the indexed (and optional stored) fields for <paramref name="value"/>.
		/// Indexed terms use the fixed 17-byte sortable encoding; stored uses compact 5/17-byte payloads.
		/// </summary>
		public static IEnumerable<Field> CreateFields(string name, IPAddress value, Field.Store stored)
		{
			ArgumentNullException.ThrowIfNull(value);
			byte[] indexed = value.ToSortableBytes();
			if (stored == Field.Store.NO)
				return SortableBytesField.CreateFields(name, indexed, Field.Store.NO);

			byte[] compact = value.ToStoredBytes();
			return SortableBytesField.CreateFields(name, indexed, compact, stored);
		}

		/// <summary>
		/// Creates the single-value sort field for <paramref name="value"/> (one per document).
		/// </summary>
		public static IEnumerable<Field> CreateSortValueFields(string name, IPAddress value)
		{
			ArgumentNullException.ThrowIfNull(value);
			return SortableBytesField.CreateSortValueFields(name, value.ToSortableBytes());
		}

		/// <summary>
		/// Creates a string sort field over <see cref="CreateSortValueFields"/>.
		/// </summary>
		public static IEnumerable<SortField> CreateSortField(string name, bool reverse = false)
		{
			yield return SortableBytesField.CreateSortField(name, reverse);
		}

		/// <summary>
		/// Exact match on the encoded address term.
		/// </summary>
		public static Query NewExactQuery(string name, IPAddress value)
		{
			ArgumentNullException.ThrowIfNull(value);
			return SortableBytesField.NewExactQuery(name, value.ToSortableBytes());
		}

		/// <summary>
		/// Inclusive/exclusive range over encoded address terms. Null bounds are open.
		/// </summary>
		public static Query NewRangeQuery(string name, IPAddress? min, IPAddress? max, bool minInclusive = true, bool maxInclusive = true)
		{
			byte[]? minBytes = min?.ToSortableBytes();
			byte[]? maxBytes = max?.ToSortableBytes();
			return SortableBytesField.NewRangeQuery(name, minBytes, maxBytes, minInclusive, maxInclusive);
		}

		/// <summary>
		/// Matches any document that has an indexed IP address term for <paramref name="name"/>.
		/// </summary>
		public static Query NewExistsQuery(string name)
			=> SortableBytesField.NewExistsQuery(name);
	}
}
