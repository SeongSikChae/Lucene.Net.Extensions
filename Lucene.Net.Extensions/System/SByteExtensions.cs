namespace System
{
	/// <summary>
	/// Order-preserving conversions between <see cref="sbyte"/> and <see cref="byte"/> for Lucene numeric fields.
	/// </summary>
	public static class SByteExtensions
	{
		/// <summary>
		/// Converts a signed byte to an unsigned byte so Lucene signed order matches unsigned order.
		/// </summary>
		public static byte ToByte(this sbyte value)
		{
			return (byte)(value + 128);
		}
	}
}
