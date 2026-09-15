namespace Lucene.Net.Store;

/// <summary>
/// Cross-platform abstract directory for memory-mapped reads.
/// </summary>
public abstract class MMapDirectoryBase : NativeFSDirectoryBase
{
	/// <summary>Default <see cref="BufferedIndexInput"/> buffer size for mmap reads.</summary>
	public const int DefaultBufferSize = 8 * 1024;

	/// <inheritdoc />
	protected MMapDirectoryBase(DirectoryInfo path, LockFactory? lockFactory)
		: base(path, lockFactory)
	{
	}
}
