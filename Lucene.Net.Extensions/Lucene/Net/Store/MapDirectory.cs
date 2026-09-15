using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// OS별 구현체로 위임되는 메인 패키지의 public Directory 타입.
/// </summary>
public sealed class MapDirectory : MMapDirectoryBase
{
	private readonly Directory _impl;

	public const int DefaultBufferSize = MMapDirectoryBase.DefaultBufferSize;

	public MapDirectory(DirectoryInfo path) : this(path, null) { }

	public MapDirectory(DirectoryInfo path, LockFactory? lockFactory) : base(path, lockFactory)
	{
		_impl = (Directory)RuntimeDirectoryLoader.CreateInstance(
			runtimeTypeFullName: "Lucene.Net.Store.MapDirectory",
			path,
			lockFactory);
	}

	public MapDirectory(string path) : this(new DirectoryInfo(path), null) { }

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

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_impl.Dispose();

		base.Dispose(disposing);
	}
}
