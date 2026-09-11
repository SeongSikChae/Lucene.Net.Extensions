namespace System
{
	/// <summary>
	/// Order-preserving conversions between <see cref="long"/> and <see cref="ulong"/> for Lucene numeric fields.
	/// </summary>
	public static class Int64Extensions
	{
		/// <summary>
		/// Converts a signed 64-bit value to unsigned so Lucene signed order matches unsigned order.
		/// </summary>
		public static ulong ToUInt64(this long value)
		{
			return (ulong)value + 0x8000000000000000L;
		}
	}
}
