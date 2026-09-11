namespace System
{
	/// <summary>
	/// Order-preserving conversions between <see cref="ushort"/> and <see cref="short"/> for Lucene numeric fields.
	/// </summary>
	public static class UInt16Extensions
	{
		/// <summary>
		/// Converts an unsigned 16-bit value to signed so Lucene signed order matches unsigned order.
		/// </summary>
		public static short ToInt16(this ushort value)
		{
			return (short)(value - 32768);
		}
	}
}
