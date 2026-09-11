using System.Numerics;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
{
	/// <summary>
	/// Prefix-coded trie helpers for fixed-width unsigned big-endian sortable bytes,
	/// modeled after <see cref="NumericUtils"/> so range queries can use precision steps.
	/// </summary>
	internal static class SortableBytesNumericUtils
	{
		/// <summary>Default precision step in bits (one byte).</summary>
		public const int PrecisionStepDefault = 8;

		/// <summary>First-byte base so shift 0..valBits stays distinguishable and UTF-8 safe (&lt; 0x80).</summary>
		/// <remarks>
		/// Shifts are stored as <c>SHIFT_START + shift</c>. For 192-bit values max shift is 191,
		/// so SHIFT_START must be 0 and we only support valBits &lt;= 127 if we require ASCII.
		/// Instead we store shift in the first byte as raw 0..255 and index via BytesTermAttribute
		/// (not CharTerm), so values &gt;= 0x80 are fine.
		/// </remarks>
		public const int MaxSupportedBitLength = 255;

		public static BigInteger ToUnsignedBigInteger(ReadOnlySpan<byte> bytes)
		{
			if (bytes.IsEmpty)
				throw new ArgumentException("Bytes must not be empty.", nameof(bytes));

			return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
		}

		public static void WriteUnsignedBigInteger(BigInteger value, Span<byte> destination)
		{
			if (destination.IsEmpty)
				throw new ArgumentException("Destination must not be empty.", nameof(destination));
			if (value.Sign < 0)
				throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative value.");

			byte[] encoded = value.ToByteArray(isUnsigned: true, isBigEndian: true);
			destination.Clear();
			if (encoded.Length > destination.Length)
				throw new InvalidOperationException("Value overflowed fixed-width byte buffer.");

			encoded.CopyTo(destination[(destination.Length - encoded.Length)..]);
		}

		public static BigInteger MaxValue(int bitLength)
		{
			if (bitLength < 1 || bitLength > MaxSupportedBitLength)
				throw new ArgumentOutOfRangeException(nameof(bitLength));

			return (BigInteger.One << bitLength) - 1;
		}

		/// <summary>
		/// Prefix-codes <paramref name="value"/> after clearing the lowest <paramref name="shift"/> bits.
		/// Layout: <c>[shift]</c> followed by 7-bit little-endian chunks (same idea as NumericUtils).
		/// </summary>
		public static void ToPrefixCoded(BigInteger value, int shift, int bitLength, BytesRef bytes)
		{
			if (shift < 0 || shift >= bitLength)
				throw new ArgumentOutOfRangeException(nameof(shift));
			if (bitLength < 1 || bitLength > MaxSupportedBitLength)
				throw new ArgumentOutOfRangeException(nameof(bitLength));
			if (value.Sign < 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			BigInteger shifted = value >> shift;
			int remainingBits = bitLength - shift;
			int nChars = (remainingBits + 6) / 7;
			int length = nChars + 1;
			if (bytes.Bytes.Length < length)
				bytes.Bytes = new byte[length];

			bytes.Offset = 0;
			bytes.Length = length;
			bytes.Bytes[0] = (byte)shift;

			for (int i = nChars; i > 0; i--)
			{
				bytes.Bytes[i] = (byte)(int)(shifted & 0x7F);
				shifted >>= 7;
			}
		}

		public static int GetShift(BytesRef term)
		{
			if (term.Length < 1)
				throw new FormatException("Invalid prefix-coded sortable bytes term.");
			return term.Bytes[term.Offset];
		}

		/// <summary>
		/// Splits an inclusive unsigned range into trie cells (same algorithm as NumericUtils.SplitRange).
		/// </summary>
		public static void SplitRange(int bitLength, int precisionStep, BigInteger minBound, BigInteger maxBound, Action<BigInteger, BigInteger, int> addRange)
		{
			if (precisionStep < 1)
				throw new ArgumentOutOfRangeException(nameof(precisionStep), "precisionStep must be >= 1");
			if (bitLength < 1 || bitLength > MaxSupportedBitLength)
				throw new ArgumentOutOfRangeException(nameof(bitLength));
			if (minBound > maxBound)
				return;

			BigInteger valMask = MaxValue(bitLength);
			minBound &= valMask;
			maxBound &= valMask;

			int shift = 0;
			while (true)
			{
				BigInteger diff = BigInteger.One << (shift + precisionStep);
				BigInteger mask = ((BigInteger.One << precisionStep) - 1) << shift;
				bool hasLower = (minBound & mask) != 0;
				bool hasUpper = (maxBound & mask) != mask;
				BigInteger nextMinBound = (hasLower ? minBound + diff : minBound) & ~mask;
				BigInteger nextMaxBound = (hasUpper ? maxBound - diff : maxBound) & ~mask;
				nextMinBound &= valMask;
				nextMaxBound &= valMask;

				bool lowerWrapped = nextMinBound < minBound;
				bool upperWrapped = nextMaxBound > maxBound;
				if (shift + precisionStep >= bitLength || nextMinBound > nextMaxBound || lowerWrapped || upperWrapped)
					break;

				if (hasLower)
					AddRange(addRange, minBound, minBound | mask, shift, valMask);
				if (hasUpper)
					AddRange(addRange, maxBound & ~mask, maxBound, shift, valMask);

				minBound = nextMinBound;
				maxBound = nextMaxBound;
				shift += precisionStep;
			}

			AddRange(addRange, minBound, maxBound, shift, valMask);
		}

		private static void AddRange(Action<BigInteger, BigInteger, int> addRange, BigInteger minBound, BigInteger maxBound, int shift, BigInteger valMask)
		{
			BigInteger lowerBits = (BigInteger.One << shift) - 1;
			maxBound = (maxBound | lowerBits) & valMask;
			addRange(minBound, maxBound, shift);
		}

		public static bool TryIncrement(ref BigInteger value, int bitLength)
		{
			BigInteger max = MaxValue(bitLength);
			if (value >= max)
				return false;
			value += 1;
			return true;
		}

		public static bool TryDecrement(ref BigInteger value, int bitLength)
		{
			if (value <= 0)
				return false;
			value -= 1;
			return true;
		}
	}
}
