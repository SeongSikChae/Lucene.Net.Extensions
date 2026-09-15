using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// Linux / macOS random-access reads using Lucene.NET <see cref="NIOFSDirectory"/>.
/// (unified name for RID-based runtime selection)
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal sealed class RuntimeRandomAccessDirectory : RandomAccessDirectoryBase
{
	private readonly NIOFSDirectory _inner;

	/// <inheritdoc />
	public RuntimeRandomAccessDirectory(DirectoryInfo path) : this(path, null) { }
	/// <inheritdoc />
	public RuntimeRandomAccessDirectory(DirectoryInfo path, LockFactory? lockFactory) : base(path, lockFactory)
	{
		_inner = new NIOFSDirectory(path);
	}

	/// <inheritdoc />
	public RuntimeRandomAccessDirectory(string path) : this(new DirectoryInfo(path), null) { }

	/// <inheritdoc />
	public override IndexInput OpenInput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return _inner.OpenInput(name, context);
	}

	/// <inheritdoc />
	public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return _inner.CreateSlicer(name, context);
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_inner.Dispose();

		base.Dispose(disposing);
	}
}
