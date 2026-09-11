using System.Buffers.Binary;
using System.Net;

namespace System
{
	/// <summary>
	/// Order-preserving conversions for <see cref="uint"/> used by Lucene numeric fields and IP helpers.
	/// </summary>
	public static class UInt32Extensions
	{
		/// <summary>
		/// Converts an unsigned 32-bit value to signed so Lucene signed order matches unsigned order.
		/// </summary>
		public static int ToInt32(this uint value)
		{
			return (int)(value - 0x80000000);
		}

		/// <summary>
		/// Creates an IPv4 <see cref="IPAddress"/> from a big-endian 32-bit value.
		/// </summary>
		public static IPAddress ToIPAddress(this uint value)
		{
			Span<byte> span = stackalloc byte[4];
			BinaryPrimitives.WriteUInt32BigEndian(span, value);
			return new IPAddress(span);
		}
	}
}
