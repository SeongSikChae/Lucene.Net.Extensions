using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// Linux / macOS random-access reads using Lucene.NET <see cref="NIOFSDirectory"/>.
/// (unified name for RID-based runtime selection)
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class RandomAccessDirectory : RandomAccessDirectoryBase
{
	private readonly NIOFSDirectory _inner;

	public RandomAccessDirectory(DirectoryInfo path) : this(path, null) { }
	public RandomAccessDirectory(DirectoryInfo path, LockFactory? lockFactory) : base(path, lockFactory)
	{
		_inner = new NIOFSDirectory(path);
	}

	public RandomAccessDirectory(string path) : this(new DirectoryInfo(path), null) { }

	public override IndexInput OpenInput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return _inner.OpenInput(name, context);
	}

	public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return _inner.CreateSlicer(name, context);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_inner.Dispose();

		base.Dispose(disposing);
	}
}
