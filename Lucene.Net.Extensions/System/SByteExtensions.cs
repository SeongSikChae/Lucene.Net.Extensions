namespace System
{
	public static class SByteExtensions
	{
		public static byte ToByte(this sbyte value)
		{
			return (byte)(value + 128);
		}
	}
}
