using System.Net;

namespace System
{
	/// <summary>
	/// Order-preserving conversions for <see cref="int"/> used by Lucene numeric fields and IP helpers.
	/// </summary>
	public static class Int32Extensions
	{
		/// <summary>
		/// Converts a signed 32-bit value to unsigned so Lucene signed order matches unsigned order.
		/// </summary>
		public static uint ToUInt32(this int value)
		{
			return (uint)(value + 0x80000000);
		}

		/// <summary>
		/// Interprets the order-preserving signed encoding as an IPv4 <see cref="IPAddress"/>.
		/// </summary>
		public static IPAddress ToIPAddress(this int value)
		{
			uint v = value.ToUInt32();
			return v.ToIPAddress();
		}
	}
}
