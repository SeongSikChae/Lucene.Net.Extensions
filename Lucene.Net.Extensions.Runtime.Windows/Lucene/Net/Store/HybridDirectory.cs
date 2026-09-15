using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// Windows hybrid implementation: large files use memory mapping and smaller files use random access.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class RuntimeHybridDirectory : HybridDirectoryBase
{
	/// <inheritdoc />
	public RuntimeHybridDirectory(DirectoryInfo path) : this(path, null, DefaultMmapThresholdBytes) { }
	/// <inheritdoc />
	public RuntimeHybridDirectory(DirectoryInfo path, LockFactory? lockFactory, long mmapThresholdBytes = DefaultMmapThresholdBytes)
		: base(path, lockFactory, mmapThresholdBytes)
	{
	}

	/// <inheritdoc />
	public RuntimeHybridDirectory(string path) : this(new DirectoryInfo(path), null, DefaultMmapThresholdBytes) { }

	/// <inheritdoc />
	protected override IndexInput OpenMmapInput(string name, string fullPath, IOContext context) =>
		RuntimeMapDirectory.MapIndexInput.Open(fullPath, context);

	/// <inheritdoc />
	protected override IndexInput OpenRandomAccessInput(string name, string fullPath, IOContext context)
	{
		RuntimeRandomAccessDirectory.StreamOwner owner = RuntimeRandomAccessDirectory.StreamOwner.Open(fullPath);
		try
		{
			int bufferSize = RuntimeRandomAccessDirectory.ResolveBufferSize(context);
			return new RuntimeRandomAccessDirectory.RandomAccessIndexInput(
				$"Hybrid/RA(path=\"{fullPath}\")",
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
	protected override IndexInputSlicer CreateMmapSlicer(string name, string fullPath, IOContext context) =>
		RuntimeMapDirectory.CreateSlicerCore(fullPath, context);

	/// <inheritdoc />
	protected override IndexInputSlicer CreateRandomAccessSlicer(string name, string fullPath, IOContext context) =>
		RuntimeRandomAccessDirectory.CreateSlicerCore(fullPath, context);
}
