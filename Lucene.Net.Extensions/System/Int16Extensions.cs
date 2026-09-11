namespace System
{
	/// <summary>
	/// Order-preserving conversions between <see cref="short"/> and <see cref="ushort"/> for Lucene numeric fields.
	/// </summary>
	public static class Int16Extensions
	{
		/// <summary>
		/// Converts a signed 16-bit value to unsigned so Lucene signed order matches unsigned order.
		/// </summary>
		public static ushort ToUInt16(this short value)
		{
			return (ushort)(value + 32768);
		}
	}
}
