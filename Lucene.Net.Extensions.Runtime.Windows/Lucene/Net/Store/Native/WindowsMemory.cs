using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Lucene.Net.Store.Native;

[SupportedOSPlatform("windows")]
internal static partial class WindowsMemory
{
	[StructLayout(LayoutKind.Sequential)]
	private struct WIN32_MEMORY_RANGE_ENTRY
	{
		public IntPtr VirtualAddress;
		public UIntPtr NumberOfBytes;
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool PrefetchVirtualMemory(
		IntPtr hProcess,
		UIntPtr numberOfEntries,
		ref WIN32_MEMORY_RANGE_ENTRY virtualAddresses,
		uint flags);

	[LibraryImport("kernel32.dll")]
	private static partial IntPtr GetCurrentProcess();

	/// <summary>
	/// Best-effort prefetch of a mapped virtual address range. Failures are ignored.
	/// </summary>
	public static unsafe void TryPrefetch(byte* address, long length)
	{
		if (address is null || length <= 0)
			return;

		try
		{
			WIN32_MEMORY_RANGE_ENTRY entry = new()
			{
				VirtualAddress = (IntPtr)address,
				NumberOfBytes = checked((UIntPtr)(ulong)length),
			};
			_ = PrefetchVirtualMemory(GetCurrentProcess(), (UIntPtr)1, ref entry, 0);
		}
		catch
		{
			// Prefetch is a hint only.
		}
	}
}
