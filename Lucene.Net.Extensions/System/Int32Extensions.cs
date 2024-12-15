using System.Net;

namespace System
{
	public static class Int32Extensions
	{
		public static uint ToUInt32(this int value)
		{
			return (uint)(value + 0x80000000);
		}

		public static IPAddress ToIPAddress(this int value)
		{
			uint v = value.ToUInt32();
			return v.ToIPAddress();
		}
	}
}
