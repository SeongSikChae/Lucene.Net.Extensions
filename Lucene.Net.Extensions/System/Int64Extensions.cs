namespace System
{
	public static class Int64Extensions
	{
		public static ulong ToUInt64(this long value)
		{
			return (ulong)value + 0x8000000000000000L;
		}
	}
}
