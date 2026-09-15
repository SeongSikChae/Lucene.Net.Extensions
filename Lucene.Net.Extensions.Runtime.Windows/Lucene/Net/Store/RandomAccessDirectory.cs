using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Lucene.Net.Store;

/// <summary>
/// Windows-tuned random-access reads (unified name for RID-based runtime selection).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RandomAccessDirectory : RandomAccessDirectoryBase
{
	/// <summary>Default <see cref="BufferedIndexInput"/> buffer size for random-access reads.</summary>
	public const int DefaultBufferSize = 8 * 1024;

	public RandomAccessDirectory(DirectoryInfo path) : base(path, null) { }
	public RandomAccessDirectory(DirectoryInfo path, LockFactory? lockFactory) : base(path, lockFactory) { }
	public RandomAccessDirectory(string path) : this(new DirectoryInfo(path)) { }

	public override IndexInput OpenInput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		string fullPath = Path.Combine(Directory.FullName, name);
		StreamOwner owner = StreamOwner.Open(fullPath);
		try
		{
			int bufferSize = ResolveBufferSize(context);
			return new RandomAccessIndexInput(
				$"RandomAccessIndexInput(path=\"{fullPath}\")",
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

	public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return CreateSlicerCore(Path.Combine(Directory.FullName, name), context);
	}

	internal static IndexInputSlicer CreateSlicerCore(string fullPath, IOContext context)
	{
		StreamOwner owner = StreamOwner.Open(fullPath);
		try
		{
			return new RandomAccessSlicer(owner, fullPath, context);
		}
		catch
		{
			owner.Dispose();
			throw;
		}
	}

	internal static int ResolveBufferSize(IOContext context) =>
		Math.Max(BufferedIndexInput.GetBufferSize(context), DefaultBufferSize);

	internal static FileStream OpenReadStream(string path) =>
		new(path,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite | FileShare.Delete,
			bufferSize: 1,
			FileOptions.RandomAccess);

	internal sealed class StreamOwner : IDisposable
	{
		private readonly SharedResourceGate _gate = new();
		private readonly FileStream _stream;
		private bool _cleaned;

		public SafeFileHandle Handle { get; }
		public long Length { get; }

		private StreamOwner(FileStream stream)
		{
			_stream = stream;
			Handle = stream.SafeFileHandle;
			Length = stream.Length;
		}

		public static StreamOwner Open(string path)
		{
			FileStream? stream = null;
			try
			{
				stream = OpenReadStream(path);
				StreamOwner owner = new(stream);
				stream = null;
				return owner;
			}
			catch
			{
				stream?.Dispose();
				throw;
			}
		}

		public void AddRef() => _gate.AddOwner();

		public int Read(Span<byte> destination, long fileOffset)
		{
			using (_gate.EnterRead())
			{
				return System.IO.RandomAccess.Read(Handle, destination, fileOffset);
			}
		}

		public void Dispose()
		{
			if (!_gate.ReleaseOwner())
				return;
			if (_cleaned)
				return;
			_cleaned = true;
			_stream.Dispose();
		}
	}

	internal sealed class RandomAccessSlicer : Directory.IndexInputSlicer
	{
		private readonly StreamOwner _owner;
		private readonly string _path;
		private readonly IOContext _context;
		private int _disposed;

		public RandomAccessSlicer(StreamOwner owner, string path, IOContext context)
		{
			_owner = owner;
			_path = path;
			_context = context;
		}

		public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
		{
			ValidateSlice(_owner.Length, offset, length);
			_owner.AddRef();
			try
			{
				int bufferSize = ResolveBufferSize(_context);
				return new RandomAccessIndexInput(
					$"RandomAccessIndexInput({sliceDescription} in path=\"{_path}\" slice={offset}:{offset + length})",
					_owner,
					offset,
					length,
					bufferSize);
			}
			catch
			{
				_owner.Dispose();
				throw;
			}
		}

		[Obsolete("Only for reading CFS files from 3.x indexes.")]
		public override IndexInput OpenFullSlice() => OpenSlice("full-slice", 0, _owner.Length);

		protected override void Dispose(bool disposing)
		{
			if (!disposing || Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;
			_owner.Dispose();
		}
	}

	internal sealed class RandomAccessIndexInput : BufferedIndexInput
	{
		private readonly StreamOwner _owner;
		private readonly long _off;
		private readonly long _end;
		private int _disposed;

		public RandomAccessIndexInput(string resourceDesc, StreamOwner owner, long offset, long length, int bufferSize)
			: base(resourceDesc, bufferSize)
		{
			_owner = owner;
			_off = offset;
			_end = offset + length;
		}

		public override long Length => _end - _off;

		protected override void ReadInternal(byte[] b, int offset, int len)
		{
			long position = _off + Position;
			if (len < 0 || position < _off || position > _end || len > _end - position)
				throw new IOException("read past EOF: " + this);

			int total = 0;
			while (total < len)
			{
				int read = _owner.Read(b.AsSpan(offset + total, len - total), position + total);
				if (read <= 0)
					throw new IOException("read past EOF: " + this);
				total += read;
			}
		}

		protected override void SeekInternal(long pos)
		{
		}

		public override object Clone()
		{
			_owner.AddRef();
			try
			{
				RandomAccessIndexInput clone = (RandomAccessIndexInput)base.Clone();
				clone._disposed = 0;
				return clone;
			}
			catch
			{
				_owner.Dispose();
				throw;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing || Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;
			_owner.Dispose();
		}
	}
}
