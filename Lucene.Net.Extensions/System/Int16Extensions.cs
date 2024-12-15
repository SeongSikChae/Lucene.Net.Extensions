namespace System
{
	public static class Int16Extensions
	{
		public static ushort ToUInt16(this short value)
		{
			return (ushort)(value + 32768);
		}
	}
}
