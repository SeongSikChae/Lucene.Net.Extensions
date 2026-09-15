namespace Lucene.Net.Store;

/// <summary>
/// Cross-platform abstract directory for random-access file reads.
/// </summary>
public abstract class RandomAccessDirectoryBase : NativeFSDirectoryBase
{
	/// <summary>Default <see cref="BufferedIndexInput"/> buffer size for random-access reads.</summary>
	public const int DefaultBufferSize = 8 * 1024;

	/// <inheritdoc />
	protected RandomAccessDirectoryBase(DirectoryInfo path, LockFactory? lockFactory)
		: base(path, lockFactory)
	{
	}
}
