using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// Linux / macOS hybrid — large files use <see cref="MapDirectory"/>, smaller files use <see cref="RandomAccessDirectory"/>.
/// (unified name for RID-based runtime selection)
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class HybridDirectory : HybridDirectoryBase
{
	public const long DefaultMmapThresholdBytes = HybridDirectoryBase.DefaultMmapThresholdBytes;

	private readonly RandomAccessDirectory _raDirectory;

	public HybridDirectory(DirectoryInfo path) : this(path, null, DefaultMmapThresholdBytes) { }

	public HybridDirectory(DirectoryInfo path, LockFactory? lockFactory, long mmapThresholdBytes = DefaultMmapThresholdBytes)
		: base(path, lockFactory, mmapThresholdBytes)
	{
		_raDirectory = new RandomAccessDirectory(path);
	}

	public HybridDirectory(string path) : this(new DirectoryInfo(path), null, DefaultMmapThresholdBytes) { }

	public override IndexInput OpenInput(string name, IOContext context)
	{
		// HybridDirectoryBase already implements the ShouldUseMmap logic.
		return base.OpenInput(name, context);
	}

	protected override IndexInput OpenMmapInput(string name, string fullPath, IOContext context) =>
		MapDirectory.MapIndexInput.Open(fullPath, context);

	protected override IndexInput OpenRandomAccessInput(string name, string fullPath, IOContext context) =>
		_raDirectory.OpenInput(name, context);

	protected override IndexInputSlicer CreateMmapSlicer(string name, string fullPath, IOContext context) =>
		MapDirectory.CreateSlicerCore(fullPath, context);

	protected override IndexInputSlicer CreateRandomAccessSlicer(string name, string fullPath, IOContext context) =>
		_raDirectory.CreateSlicer(name, context);

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_raDirectory.Dispose();
		base.Dispose(disposing);
	}
}
