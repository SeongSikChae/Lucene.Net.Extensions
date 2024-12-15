namespace System
{
	public static class ByteExtensions
	{
		public static sbyte ToSByte(this byte value)
		{
			return (sbyte)(value - 128);
		}
	}
}
