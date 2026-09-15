namespace Lucene.Net.Store;

/// <summary>
/// Cross-platform abstract hybrid directory: large files use mmap, smaller files use random-access.
/// </summary>
public abstract class HybridDirectoryBase : NativeFSDirectoryBase
{
	/// <summary>Default size threshold (1 MiB) above which mmap reads are preferred.</summary>
	public const long DefaultMmapThresholdBytes = 1L * 1024 * 1024;

	public long MmapThresholdBytes { get; }

	protected HybridDirectoryBase(DirectoryInfo path, LockFactory? lockFactory, long mmapThresholdBytes = DefaultMmapThresholdBytes)
		: base(path, lockFactory)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(mmapThresholdBytes);
		MmapThresholdBytes = mmapThresholdBytes;
	}

	protected virtual bool ShouldUseMmap(long length, IOContext context)
	{
		if (length == 0)
			return false;

		if (length >= MmapThresholdBytes)
			return true;

		if (context.Context == IOContext.UsageContext.MERGE && length >= MmapThresholdBytes / 4)
			return true;

		return false;
	}

	public override IndexInput OpenInput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		string fullPath = Path.Combine(Directory.FullName, name);
		long length = new FileInfo(fullPath).Length;

		return ShouldUseMmap(length, context)
			? OpenMmapInput(name, fullPath, context)
			: OpenRandomAccessInput(name, fullPath, context);
	}

	public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		string fullPath = Path.Combine(Directory.FullName, name);
		long length = new FileInfo(fullPath).Length;

		return ShouldUseMmap(length, context)
			? CreateMmapSlicer(name, fullPath, context)
			: CreateRandomAccessSlicer(name, fullPath, context);
	}

	protected abstract IndexInput OpenMmapInput(string name, string fullPath, IOContext context);
	protected abstract IndexInput OpenRandomAccessInput(string name, string fullPath, IOContext context);
	protected abstract IndexInputSlicer CreateMmapSlicer(string name, string fullPath, IOContext context);
	protected abstract IndexInputSlicer CreateRandomAccessSlicer(string name, string fullPath, IOContext context);
}
