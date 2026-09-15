namespace Lucene.Net.Store;

/// <summary>
/// OS별 구현체로 위임되는 메인 패키지의 public Directory 타입.
/// </summary>
public sealed class RandomAccessDirectory : RandomAccessDirectoryBase
{
	private readonly Directory _impl;

	public const int DefaultBufferSize = RandomAccessDirectoryBase.DefaultBufferSize;

	public RandomAccessDirectory(DirectoryInfo path) : this(path, null) { }

	public RandomAccessDirectory(DirectoryInfo path, LockFactory? lockFactory) : base(path, lockFactory)
	{
		_impl = (Directory)RuntimeDirectoryLoader.CreateInstance(
			runtimeTypeFullName: "Lucene.Net.Store.RandomAccessDirectory",
			path,
			lockFactory);
	}

	public RandomAccessDirectory(string path) : this(new DirectoryInfo(path), null) { }

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
