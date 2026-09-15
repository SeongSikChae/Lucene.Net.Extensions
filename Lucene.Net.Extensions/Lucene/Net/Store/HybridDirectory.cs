namespace Lucene.Net.Store;

/// <summary>
/// OS별 구현체로 위임하는 메인 패키지의 public Directory 래퍼입니다.
/// </summary>
public sealed class HybridDirectory : HybridDirectoryBase
{
	private readonly Directory _impl;

	/// <inheritdoc />
	public new const long DefaultMmapThresholdBytes = HybridDirectoryBase.DefaultMmapThresholdBytes;

	/// <inheritdoc />
	public HybridDirectory(DirectoryInfo path) : this(path, null, DefaultMmapThresholdBytes) { }

	/// <inheritdoc />
	public HybridDirectory(DirectoryInfo path, LockFactory? lockFactory, long mmapThresholdBytes = DefaultMmapThresholdBytes)
		: base(path, lockFactory, mmapThresholdBytes)
	{
		_impl = (Directory)RuntimeDirectoryLoader.CreateInstance(
			runtimeTypeFullName: "Lucene.Net.Store.RuntimeHybridDirectory",
			path,
			lockFactory,
			mmapThresholdBytes);
	}

	/// <inheritdoc />
	public HybridDirectory(string path) : this(new DirectoryInfo(path), null, DefaultMmapThresholdBytes) { }

	/// <inheritdoc />
	public override IndexInput OpenInput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return _impl.OpenInput(name, context);
	}

	/// <inheritdoc />
	public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return _impl.CreateSlicer(name, context);
	}

	// HybridDirectoryBase가 결정하는 읽기 API를 실제 런타임 구현으로 위임합니다.
	/// <inheritdoc />
	protected override IndexInput OpenMmapInput(string name, string fullPath, IOContext context) =>
		_impl.OpenInput(name, context);

	/// <inheritdoc />
	protected override IndexInput OpenRandomAccessInput(string name, string fullPath, IOContext context) =>
		_impl.OpenInput(name, context);

	/// <inheritdoc />
	protected override IndexInputSlicer CreateMmapSlicer(string name, string fullPath, IOContext context) =>
		_impl.CreateSlicer(name, context);

	/// <inheritdoc />
	protected override IndexInputSlicer CreateRandomAccessSlicer(string name, string fullPath, IOContext context) =>
		_impl.CreateSlicer(name, context);

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_impl.Dispose();

		base.Dispose(disposing);
	}
}
