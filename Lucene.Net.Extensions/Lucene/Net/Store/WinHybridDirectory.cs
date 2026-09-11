using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// Phase C: Hybrid — large files use WinMMap (A), smaller files use WinRA (B).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WinHybridDirectory : WinFSDirectoryBase
{
	/// <summary>Default size threshold (1 MiB) above which mmap reads are preferred.</summary>
	public const long DefaultMmapThresholdBytes = 1L * 1024 * 1024;

	/// <summary>Byte length at or above which files are opened via <see cref="WinMMapDirectory"/>.</summary>
	public long MmapThresholdBytes { get; }

	/// <summary>
	/// Creates a hybrid directory at <paramref name="path"/> using <see cref="DefaultMmapThresholdBytes"/>.
	/// </summary>
	public WinHybridDirectory(DirectoryInfo path) : this(path, null, DefaultMmapThresholdBytes) { }

	/// <summary>
	/// Creates a hybrid directory at <paramref name="path"/>.
	/// </summary>
	/// <param name="path">Index directory on disk.</param>
	/// <param name="lockFactory">Lock factory, or <c>null</c> for the default.</param>
	/// <param name="mmapThresholdBytes">Minimum file size for mmap; must be non-negative.</param>
	public WinHybridDirectory(DirectoryInfo path, LockFactory? lockFactory, long mmapThresholdBytes = DefaultMmapThresholdBytes)
		: base(path, lockFactory)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(mmapThresholdBytes);
		MmapThresholdBytes = mmapThresholdBytes;
	}

	/// <summary>
	/// Creates a hybrid directory at <paramref name="path"/> using <see cref="DefaultMmapThresholdBytes"/>.
	/// </summary>
	public WinHybridDirectory(string path) : this(new DirectoryInfo(path), null, DefaultMmapThresholdBytes) { }

	/// <inheritdoc />
	public override IndexInput OpenInput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		string fullPath = Path.Combine(Directory.FullName, name);
		long length = new FileInfo(fullPath).Length;

		if (ShouldUseMmap(length, context))
			return WinMMapDirectory.WinMMapIndexInput.Open(fullPath, context);

		WinRandomAccessDirectory.StreamOwner owner = WinRandomAccessDirectory.StreamOwner.Open(fullPath);
		try
		{
			int bufferSize = WinRandomAccessDirectory.ResolveBufferSize(context);
			return new WinRandomAccessDirectory.WinRandomAccessIndexInput(
				$"WinHybrid/RA(path=\"{fullPath}\")",
				owner,
				offset: 0,
				length: owner.Length,
				bufferSize);
		}
		catch
		{
			owner.Dispose();
			throw;
		}
	}

	/// <inheritdoc />
	public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		string fullPath = Path.Combine(Directory.FullName, name);
		long length = new FileInfo(fullPath).Length;

		if (ShouldUseMmap(length, context))
			return WinMMapDirectory.CreateSlicerCore(fullPath, context);

		return WinRandomAccessDirectory.CreateSlicerCore(fullPath, context);
	}

	private bool ShouldUseMmap(long length, IOContext context)
	{
		// Empty files cannot be memory-mapped on Windows; always use RandomAccess.
		if (length == 0)
			return false;

		if (length >= MmapThresholdBytes)
			return true;

		if (context.Context == IOContext.UsageContext.MERGE && length >= MmapThresholdBytes / 4)
			return true;

		return false;
	}
}
