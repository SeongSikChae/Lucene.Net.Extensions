namespace System
{
	/// <summary>
	/// Order-preserving conversions between <see cref="byte"/> and <see cref="sbyte"/> for Lucene numeric fields.
	/// </summary>
	public static class ByteExtensions
	{
		/// <summary>
		/// Converts an unsigned byte to a signed byte so Lucene signed order matches unsigned order.
		/// </summary>
		public static sbyte ToSByte(this byte value)
		{
			return (sbyte)(value - 128);
		}
	}
}
