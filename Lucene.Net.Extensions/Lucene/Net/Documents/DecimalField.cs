using Lucene.Net.Search;
using System.Buffers.Binary;
using System.Numerics;

namespace Lucene.Net.Documents
{
	/// <summary>
	/// Indexes a <see cref="decimal"/> as a prefix-coded sortable trie (multi-value safe)
	/// and optionally stores exact <c>decimal.GetBits</c> as a 16-byte payload for round-trip.
	/// Sorting requires a separate single-value field via <see cref="CreateSortValueFields"/>.
	/// </summary>
	public static class DecimalField
	{
		/// <summary>Number of bytes in the sortable encoding.</summary>
		public const int SortableLength = 24;

		/// <summary>Number of bytes in the stored GetBits payload (4 × Int32).</summary>
		public const int StoredBitsLength = 16;

		/// <summary>Suffix for the single-value sort field.</summary>
		public const string SortFieldSuffix = SortableBytesField.SortFieldSuffix;

		/// <summary>Suffix for the exists marker field.</summary>
		public const string ExistsFieldSuffix = SortableBytesField.ExistsFieldSuffix;

		/// <summary>2^191 — midpoint of the unsigned 192-bit space.</summary>
		private static readonly BigInteger Offset = BigInteger.One << 191;

		/// <summary>
		/// Creates an indexed sortable trie and optional stored GetBits payload for <paramref name="value"/>.
		/// </summary>
		/// <param name="name">Field name.</param>
		/// <param name="value">Value to index.</param>
		/// <param name="stored">Whether to store exact GetBits for round-trip.</param>
		public static IEnumerable<Field> CreateFields(string name, decimal value, Field.Store stored)
		{
			byte[] sortable = new byte[SortableLength];
			WriteSortableBytes(value, sortable);

			if (stored == Field.Store.YES)
			{
				byte[] bits = new byte[StoredBitsLength];
				WriteStoredBits(value, bits);
				return SortableBytesField.CreateFields(name, sortable, bits, stored);
			}

			return SortableBytesField.CreateFields(name, sortable, Field.Store.NO);
		}

		/// <summary>
		/// Creates the single-value sort field for <paramref name="value"/> (one per document).
		/// </summary>
		public static IEnumerable<Field> CreateSortValueFields(string name, decimal value)
		{
			byte[] sortable = new byte[SortableLength];
			WriteSortableBytes(value, sortable);
			return SortableBytesField.CreateSortValueFields(name, sortable);
		}

		/// <summary>
		/// Creates a string sort field over <see cref="CreateSortValueFields"/>.
		/// </summary>
		public static IEnumerable<SortField> CreateSortField(string name, bool reverse = false)
		{
			yield return SortableBytesField.CreateSortField(name, reverse);
		}

		/// <summary>
		/// Exact match over the sortable term (scale-normalized numeric equality).
		/// </summary>
		public static Query NewExactQuery(string name, decimal value)
		{
			byte[] sortable = new byte[SortableLength];
			WriteSortableBytes(value, sortable);
			return SortableBytesField.NewExactQuery(name, sortable);
		}

		/// <summary>
		/// Inclusive/exclusive numeric range over sortable terms. Null bounds are open.
		/// </summary>
		public static Query NewRangeQuery(string name, decimal? min, decimal? max, bool minInclusive = true, bool maxInclusive = true)
		{
			byte[]? minBytes = null;
			byte[]? maxBytes = null;

			if (min is not null)
			{
				minBytes = new byte[SortableLength];
				WriteSortableBytes(min.Value, minBytes);
			}

			if (max is not null)
			{
				maxBytes = new byte[SortableLength];
				WriteSortableBytes(max.Value, maxBytes);
			}

			return SortableBytesField.NewRangeQuery(name, minBytes, maxBytes, minInclusive, maxInclusive);
		}

		/// <summary>
		/// Matches any document that has a sortable decimal term for <paramref name="name"/>.
		/// </summary>
		public static Query NewExistsQuery(string name)
			=> SortableBytesField.NewExistsQuery(name);

		/// <summary>
		/// Writes the 24-byte order-preserving encoding of <paramref name="value"/>.
		/// </summary>
		public static void WriteSortableBytes(decimal value, Span<byte> destination)
		{
			if (destination.Length < SortableLength)
				throw new ArgumentException($"Destination must be at least {SortableLength} bytes.", nameof(destination));

			WriteUnsigned192BigEndian(ToSortableInteger(value), destination[..SortableLength]);
		}

		/// <summary>
		/// Writes the 16-byte <c>decimal.GetBits</c> payload of <paramref name="value"/>.
		/// </summary>
		public static void WriteStoredBits(decimal value, Span<byte> destination)
		{
			if (destination.Length < StoredBitsLength)
				throw new ArgumentException($"Destination must be at least {StoredBitsLength} bytes.", nameof(destination));

			int[] parts = decimal.GetBits(value);
			for (int i = 0; i < 4; i++)
				BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(i * sizeof(int), sizeof(int)), parts[i]);
		}

		/// <summary>
		/// Reconstructs a <see cref="decimal"/> from a 16-byte GetBits payload.
		/// </summary>
		public static decimal FromStoredBits(ReadOnlySpan<byte> encoded)
		{
			if (encoded.Length != StoredBitsLength)
				throw new ArgumentException($"Stored bits must be {StoredBitsLength} bytes.", nameof(encoded));

			int[] parts =
			[
				BinaryPrimitives.ReadInt32LittleEndian(encoded),
				BinaryPrimitives.ReadInt32LittleEndian(encoded.Slice(4)),
				BinaryPrimitives.ReadInt32LittleEndian(encoded.Slice(8)),
				BinaryPrimitives.ReadInt32LittleEndian(encoded.Slice(12)),
			];
			return new decimal(parts);
		}

		private static BigInteger ToSortableInteger(decimal value)
		{
			int[] bits = decimal.GetBits(value);
			bool negative = (bits[3] & unchecked((int)0x80000000)) != 0;
			int scale = (bits[3] >> 16) & 0xFF;

			BigInteger mantissa =
				(uint)bits[0]
				| ((BigInteger)(uint)bits[1] << 32)
				| ((BigInteger)(uint)bits[2] << 64);

			BigInteger scaled = mantissa * BigInteger.Pow(10, 28 - scale);
			return negative ? Offset - scaled : Offset + scaled;
		}

		private static void WriteUnsigned192BigEndian(BigInteger value, Span<byte> destination)
		{
			if (destination.Length != SortableLength)
				throw new ArgumentException($"Destination must be {SortableLength} bytes.", nameof(destination));
			if (value.Sign < 0)
				throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative sortable value.");

			byte[] encoded = value.ToByteArray(isUnsigned: true, isBigEndian: true);
			destination.Clear();
			if (encoded.Length > destination.Length)
				throw new InvalidOperationException("Sortable decimal encoding overflowed 192 bits.");

			encoded.CopyTo(destination[(destination.Length - encoded.Length)..]);
		}
	}
}
