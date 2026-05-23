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

		public static int ToInt32(this IPAddress address)
		{
			if (address.AddressFamily != AddressFamily.InterNetwork)
				throw new ArgumentException("IPv4 address required.", nameof(address));

			Span<byte> span = address.GetAddressBytes().AsSpan();
			uint v = BinaryPrimitives.ReadUInt32BigEndian(span);
			return v.ToInt32();
		}

		public static void ToInt64Pair(this IPAddress address, out long high, out long low)
		{
			if (address.AddressFamily != AddressFamily.InterNetworkV6)
				throw new ArgumentException("IPv6 address required.", nameof(address));

			ReadOnlySpan<byte> bytes = address.GetAddressBytes();
			if (bytes.Length != 16)
				throw new ArgumentException("Invalid IPv6 address length.", nameof(address));

			ulong hi = BinaryPrimitives.ReadUInt64BigEndian(bytes);
			ulong lo = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);
			high = hi.ToInt64();
			low = lo.ToInt64();
		}

		public static IPAddress ToIPv6Address(long high, long low)
		{
			Span<byte> span = stackalloc byte[16];
			BinaryPrimitives.WriteUInt64BigEndian(span, high.ToUInt64());
			BinaryPrimitives.WriteUInt64BigEndian(span[8..], low.ToUInt64());
			return new IPAddress(span);
		}
	}
}
