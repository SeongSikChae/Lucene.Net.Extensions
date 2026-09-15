using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Lucene.Net.Store.Native;

/// <summary>
/// Best-effort page hints for mmap regions on Linux / macOS.
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal static partial class UnixMemory
{
	// POSIX_MADV_WILLNEED — same numeric value on Linux and macOS.
	private const int PosixMadvWillNeed = 3;

	[LibraryImport("libc", EntryPoint = "posix_madvise", SetLastError = true)]
	private static partial int PosixMAdvise(IntPtr addr, nuint length, int advice);

	public static unsafe void TryPrefetch(byte* address, long length)
	{
		if (address is null || length <= 0)
			return;

		try
		{
			nint pageSize = Environment.SystemPageSize;
			if (pageSize <= 0)
				pageSize = 4096;

			long addr = (long)address;
			long aligned = addr & ~(pageSize - 1);
			long end = addr + length;
			long alignedLength = end - aligned;
			if (alignedLength <= 0)
				return;

			_ = PosixMAdvise((IntPtr)aligned, (nuint)alignedLength, PosixMadvWillNeed);
		}
		catch
		{
			// Prefetch is a hint only.
		}
	}
}
