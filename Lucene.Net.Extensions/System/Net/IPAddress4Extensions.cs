using System.Buffers.Binary;

namespace System.Net
{
	public static class IPAddress4Extensions
	{
		public static int ToInt32(this IPAddress address)
		{
			Span<byte> span = address.GetAddressBytes().AsSpan();
			uint v = BinaryPrimitives.ReadUInt32BigEndian(span);
			return v.ToInt32();
		}
	}
}
