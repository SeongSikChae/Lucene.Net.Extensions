namespace System
{
	public static class UInt16Extensions
	{
		public static short ToInt16(this ushort value)
		{
			return (short)(value - 32768);
		}
	}
}
