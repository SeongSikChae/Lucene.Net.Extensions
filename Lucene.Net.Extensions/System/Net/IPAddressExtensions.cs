using System.Buffers.Binary;
using System.Net.Sockets;

namespace System.Net
{
	public static class IPAddressExtensions
	{
		public const string LowPartSuffix = "_L";

		public static IPAddress NormalizeForStorage(this IPAddress address)
		{
			if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
				return address.MapToIPv4();
			return address;
		}

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
