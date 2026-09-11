using System.Buffers.Binary;
using System.Net.Sockets;

namespace System.Net
{
	/// <summary>
	/// Helpers for encoding <see cref="IPAddress"/> values as ordered Int64 pairs for Lucene.
	/// </summary>
	public static class IPAddressExtensions
	{
		/// <summary>
		/// Suffix appended to the base field name for the low Int64 limb.
		/// </summary>
		public const string LowPartSuffix = "_L";

		/// <summary>
		/// Maps IPv4-mapped IPv6 addresses to IPv4 so the same address has a single storage form.
		/// </summary>
		public static IPAddress NormalizeForStorage(this IPAddress address)
		{
			if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
				return address.MapToIPv4();
			return address;
		}

		/// <summary>
		/// Splits an IPv4/IPv6 address into order-preserving high/low Int64 limbs.
		/// </summary>
		/// <param name="address">Address to encode (IPv4 or IPv6).</param>
		/// <param name="high">Most significant 64 bits (0 for IPv4).</param>
		/// <param name="low">Least significant 64 bits.</param>
		public static void ToInt64Pair(this IPAddress address, out long high, out long low)
		{
			if (!(address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6))
				throw new ArgumentException("IPv4 or IPv6 address required.", nameof(address));

			ReadOnlySpan<byte> bytes = address.GetAddressBytes();
			if (!(bytes.Length == 4 || bytes.Length == 16))
				throw new ArgumentException("Invalid IPv4 or IPv6 address length.", nameof(address));

			ulong hi;
			ulong lo;
            if (bytes.Length == 4)
			{
				hi = 0;
				lo = BinaryPrimitives.ReadUInt64BigEndian([0,0,0,0,..bytes]);
			}
			else
			{
                hi = BinaryPrimitives.ReadUInt64BigEndian(bytes);
                lo = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);
            }

			high = hi.ToInt64();
			low = lo.ToInt64();
		}

		/// <summary>
		/// Reconstructs an <see cref="IPAddress"/> from high/low Int64 limbs produced by <see cref="ToInt64Pair"/>.
		/// </summary>
		public static IPAddress ToIPAddress(long high, long low)
		{
            ulong h = high.ToUInt64();
            ulong l = low.ToUInt64();
            if (h == 0)
			{
				Span<byte> span = stackalloc byte[4];
				BinaryPrimitives.WriteUInt32BigEndian(span, (uint)l);
				return new IPAddress(span);
			}
			else
			{
                Span<byte> span = stackalloc byte[16];
                BinaryPrimitives.WriteUInt64BigEndian(span, h);
				BinaryPrimitives.WriteUInt64BigEndian(span[8..], l);
                return new IPAddress(span).NormalizeForStorage();
            }
		}
	}
}
