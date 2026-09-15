using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// Windows hybrid — large files use <see cref="MapDirectory"/>, smaller files use <see cref="RandomAccessDirectory"/>.
/// (unified name for RID-based runtime selection)
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class HybridDirectory : HybridDirectoryBase
{
	public const long DefaultMmapThresholdBytes = HybridDirectoryBase.DefaultMmapThresholdBytes;

	public HybridDirectory(DirectoryInfo path) : this(path, null, DefaultMmapThresholdBytes) { }
	public HybridDirectory(DirectoryInfo path, LockFactory? lockFactory, long mmapThresholdBytes = DefaultMmapThresholdBytes)
		: base(path, lockFactory, mmapThresholdBytes)
	{
	}

	public HybridDirectory(string path) : this(new DirectoryInfo(path), null, DefaultMmapThresholdBytes) { }

	protected override IndexInput OpenMmapInput(string name, string fullPath, IOContext context) =>
		MapDirectory.MapIndexInput.Open(fullPath, context);

	protected override IndexInput OpenRandomAccessInput(string name, string fullPath, IOContext context)
	{
		RandomAccessDirectory.StreamOwner owner = RandomAccessDirectory.StreamOwner.Open(fullPath);
		try
		{
			int bufferSize = RandomAccessDirectory.ResolveBufferSize(context);
			return new RandomAccessDirectory.RandomAccessIndexInput(
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

	protected override IndexInputSlicer CreateMmapSlicer(string name, string fullPath, IOContext context) =>
		MapDirectory.CreateSlicerCore(fullPath, context);

	protected override IndexInputSlicer CreateRandomAccessSlicer(string name, string fullPath, IOContext context) =>
		RandomAccessDirectory.CreateSlicerCore(fullPath, context);
}
