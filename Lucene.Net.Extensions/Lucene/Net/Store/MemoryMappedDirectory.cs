using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// OS별 구현체로 위임하는 메인 패키지의 public Directory 래퍼입니다.
/// </summary>
public sealed class MemoryMappedDirectory : MemoryMappedDirectoryBase
{
	private readonly Directory _impl;

	/// <inheritdoc />
	public new const int DefaultBufferSize = MemoryMappedDirectoryBase.DefaultBufferSize;

	/// <inheritdoc />
	public MemoryMappedDirectory(DirectoryInfo path) : this(path, null) { }

	/// <inheritdoc />
	public MemoryMappedDirectory(DirectoryInfo path, LockFactory? lockFactory) : base(path, lockFactory)
	{
		_impl = (Directory)RuntimeDirectoryLoader.CreateInstance(
			runtimeTypeFullName: "Lucene.Net.Store.RuntimeMemoryMappedDirectory",
			path,
			lockFactory);
	}

	/// <inheritdoc />
	public MemoryMappedDirectory(string path) : this(new DirectoryInfo(path), null) { }

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

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_impl.Dispose();

		base.Dispose(disposing);
	}
}
