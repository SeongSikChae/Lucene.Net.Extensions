namespace System
{
	/// <summary>
	/// Order-preserving conversions between <see cref="ulong"/> and <see cref="long"/> for Lucene numeric fields.
	/// </summary>
	public static class UInt64Extensions
	{
		/// <summary>
		/// Converts an unsigned 64-bit value to signed so Lucene signed order matches unsigned order.
		/// </summary>
		public static long ToInt64(this ulong value)
		{
			return (long)(value - 0x8000000000000000L);
		}
	}
}
