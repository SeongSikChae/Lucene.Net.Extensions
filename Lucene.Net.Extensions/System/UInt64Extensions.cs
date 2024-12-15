namespace System
{
	public static class UInt64Extensions
	{
		public static long ToInt64(this ulong value)
		{
			return (long)(value - 0x8000000000000000L);
		}
	}
}
