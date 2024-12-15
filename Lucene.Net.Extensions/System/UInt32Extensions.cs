using System.Buffers.Binary;
using System.Net;

namespace System
{
	public static class UInt32Extensions
	{
		public static int ToInt32(this uint value)
		{
			return (int)(value - 0x80000000);
		}

		public static IPAddress ToIPAddress(this uint value)
		{
			Span<byte> span = stackalloc byte[4];
			BinaryPrimitives.WriteUInt32BigEndian(span, value);
			return new IPAddress(span);
		}
	}
}
