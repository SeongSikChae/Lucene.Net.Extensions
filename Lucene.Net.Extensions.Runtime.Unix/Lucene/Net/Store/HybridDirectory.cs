using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// Linux / macOS hybrid implementation: large files use memory mapping and smaller files use random access.
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal sealed class RuntimeHybridDirectory : HybridDirectoryBase
{
	private readonly RuntimeRandomAccessDirectory _raDirectory;

	/// <inheritdoc />
	public RuntimeHybridDirectory(DirectoryInfo path) : this(path, null, DefaultMmapThresholdBytes) { }

	/// <inheritdoc />
	public RuntimeHybridDirectory(DirectoryInfo path, LockFactory? lockFactory, long mmapThresholdBytes = DefaultMmapThresholdBytes)
		: base(path, lockFactory, mmapThresholdBytes)
	{
		_raDirectory = new RuntimeRandomAccessDirectory(path);
	}
	/// <inheritdoc />
	public RuntimeHybridDirectory(string path) : this(new DirectoryInfo(path), null, DefaultMmapThresholdBytes) { }

	/// <inheritdoc />
	public override IndexInput OpenInput(string name, IOContext context)
	{
		// HybridDirectoryBase already implements the ShouldUseMmap logic.
		return base.OpenInput(name, context);
	}

	/// <inheritdoc />
	protected override IndexInput OpenMmapInput(string name, string fullPath, IOContext context) =>
		RuntimeMemoryMappedDirectory.MemoryMappedIndexInput.Open(fullPath, context);

	/// <inheritdoc />
	protected override IndexInput OpenRandomAccessInput(string name, string fullPath, IOContext context) =>
		_raDirectory.OpenInput(name, context);

	/// <inheritdoc />
	protected override IndexInputSlicer CreateMmapSlicer(string name, string fullPath, IOContext context) =>
		RuntimeMemoryMappedDirectory.CreateSlicerCore(fullPath, context);
	/// <inheritdoc />
	protected override IndexInputSlicer CreateRandomAccessSlicer(string name, string fullPath, IOContext context) =>
		_raDirectory.CreateSlicer(name, context);

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_raDirectory.Dispose();
		base.Dispose(disposing);
	}
}
