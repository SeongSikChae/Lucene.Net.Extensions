namespace Lucene.Net.Store;

/// <summary>
/// OS별 구현체로 위임되는 메인 패키지의 public Directory 타입.
/// </summary>
public sealed class HybridDirectory : HybridDirectoryBase
{
	private readonly Directory _impl;

	public const long DefaultMmapThresholdBytes = HybridDirectoryBase.DefaultMmapThresholdBytes;

	public HybridDirectory(DirectoryInfo path) : this(path, null, DefaultMmapThresholdBytes) { }

	public HybridDirectory(DirectoryInfo path, LockFactory? lockFactory, long mmapThresholdBytes = DefaultMmapThresholdBytes)
		: base(path, lockFactory, mmapThresholdBytes)
	{
		_impl = (Directory)RuntimeDirectoryLoader.CreateInstance(
			runtimeTypeFullName: "Lucene.Net.Store.HybridDirectory",
			path,
			lockFactory,
			mmapThresholdBytes);
	}

	public HybridDirectory(string path) : this(new DirectoryInfo(path), null, DefaultMmapThresholdBytes) { }

	public override IndexInput OpenInput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return _impl.OpenInput(name, context);
	}

	public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return _impl.CreateSlicer(name, context);
	}

	// HybridDirectoryBase가 요구하는 세부 전략 API를 래퍼의 런타임 구현으로 위임합니다.
	protected override IndexInput OpenMmapInput(string name, string fullPath, IOContext context) =>
		_impl.OpenInput(name, context);

	protected override IndexInput OpenRandomAccessInput(string name, string fullPath, IOContext context) =>
		_impl.OpenInput(name, context);

	protected override IndexInputSlicer CreateMmapSlicer(string name, string fullPath, IOContext context) =>
		_impl.CreateSlicer(name, context);

	protected override IndexInputSlicer CreateRandomAccessSlicer(string name, string fullPath, IOContext context) =>
		_impl.CreateSlicer(name, context);

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_impl.Dispose();

		base.Dispose(disposing);
	}
}
