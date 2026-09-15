using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;

using Lucene.Net.Store.Native;

namespace Lucene.Net.Store;

/// <summary>
/// Windows memory-mapped reads (unified name for RID-based runtime selection).
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class RuntimeMapDirectory : MMapDirectoryBase
{
	/// <inheritdoc />
	public RuntimeMapDirectory(DirectoryInfo path) : base(path, null) { }
	/// <inheritdoc />
	public RuntimeMapDirectory(DirectoryInfo path, LockFactory? lockFactory) : base(path, lockFactory) { }
	/// <inheritdoc />
	public RuntimeMapDirectory(string path) : this(new DirectoryInfo(path)) { }

	/// <inheritdoc />
	public override IndexInput OpenInput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		string fullPath = Path.Combine(Directory.FullName, name);
		return MapIndexInput.Open(fullPath, context);
	}

	/// <inheritdoc />
	public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanRead(name);
		return CreateSlicerCore(Path.Combine(Directory.FullName, name), context);
	}

	internal static IndexInputSlicer CreateSlicerCore(string fullPath, IOContext context)
	{
		long fileLength = new FileInfo(fullPath).Length;
		MappingOwner owner = MappingOwner.Open(fullPath, prefetchBytes: Math.Min(1L << 20, fileLength));
		try
		{
			return new MapSlicer(owner, context);
		}
		catch
		{
			owner.Dispose();
			throw;
		}
	}

	internal sealed unsafe class MappingOwner : IDisposable
	{
		private readonly SharedResourceGate _gate = new();
		private readonly FileStream _stream;
		private readonly MemoryMappedFile? _mmf;
		private readonly MemoryMappedViewAccessor? _accessor;
		private byte* _pointer;
		private bool _pointerAcquired;
		private bool _cleaned;

		public long Length { get; }

		private MappingOwner(FileStream stream, MemoryMappedFile? mmf, MemoryMappedViewAccessor? accessor, long length, byte* pointer, bool pointerAcquired)
		{
			_stream = stream;
			_mmf = mmf;
			_accessor = accessor;
			Length = length;
			_pointer = pointer;
			_pointerAcquired = pointerAcquired;
		}

		public static MappingOwner Open(string path, long prefetchBytes)
		{
			FileStream? stream = null;
			MemoryMappedFile? mmf = null;
			MemoryMappedViewAccessor? accessor = null;
			byte* ptr = null;
			bool acquired = false;

			try
			{
				stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1, FileOptions.RandomAccess);
				long length = stream.Length;

				// Windows cannot create a memory map for a zero-length file.
				if (length == 0)
				{
					MappingOwner emptyOwner = new(
						stream,
						mmf: null,
						accessor: null,
						length: 0,
						pointer: null,
						pointerAcquired: false);
					stream = null;
					return emptyOwner;
				}

				mmf = MemoryMappedFile.CreateFromFile(
					stream,
					mapName: null,
					capacity: 0,
					MemoryMappedFileAccess.Read,
					HandleInheritability.None,
					leaveOpen: true);

				accessor = mmf.CreateViewAccessor(0, length, MemoryMappedFileAccess.Read);
				accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
				acquired = ptr is not null;
				if (acquired && prefetchBytes > 0)
					WindowsMemory.TryPrefetch(ptr, Math.Min(prefetchBytes, length));

				MappingOwner owner = new(stream, mmf, accessor, length, ptr, acquired);
				stream = null;
				mmf = null;
				accessor = null;
				acquired = false;
				return owner;
			}
			catch
			{
				if (acquired && accessor is not null)
					accessor.SafeMemoryMappedViewHandle.ReleasePointer();

				accessor?.Dispose();
				mmf?.Dispose();
				stream?.Dispose();
				throw;
			}
		}

		public void AddRef() => _gate.AddOwner();

		public void TryPrefetchRange(long offset, long length)
		{
			using (_gate.EnterRead())
			{
				if (!_pointerAcquired || _pointer is null)
					return;
				if (offset < 0 || offset > Length || length <= 0)
					return;

				long available = Length - offset;
				WindowsMemory.TryPrefetch(_pointer + offset, Math.Min(length, available));
			}
		}

		public void Copy(long position, byte[] destination, int offset, int length)
		{
			if (length == 0)
				return;

			using (_gate.EnterRead())
			{
				if (!_pointerAcquired || _pointer is null)
					throw new IOException("read past EOF on empty mapping");
				if (position < 0 || length < 0 || position > Length || length > Length - position)
					throw new IOException("read past EOF");

				fixed (byte* dest = &destination[offset])
				{
					Buffer.MemoryCopy(_pointer + position, dest, length, length);
				}
			}
		}

		public void Dispose()
		{
			if (!_gate.ReleaseOwner())
				return;
			if (_cleaned)
				return;
			_cleaned = true;

			if (_pointerAcquired && _accessor is not null)
			{
				_accessor.SafeMemoryMappedViewHandle.ReleasePointer();
				_pointerAcquired = false;
				_pointer = null;
			}

			_accessor?.Dispose();
			_mmf?.Dispose();
			_stream.Dispose();
		}
	}

	internal sealed class MapSlicer : Lucene.Net.Store.Directory.IndexInputSlicer
	{
		private readonly MappingOwner _owner;
		private readonly IOContext _context;
		private int _disposed;

		public MapSlicer(MappingOwner owner, IOContext context)
		{
			_owner = owner;
			_context = context;
		}

		public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
		{
			ValidateSlice(_owner.Length, offset, length);
			_owner.AddRef();
			try
			{
				_owner.TryPrefetchRange(offset, Math.Min(length, 1L << 20));
				int bufferSize = Math.Max(BufferedIndexInput.GetBufferSize(_context), DefaultBufferSize);
				return new MapIndexInput(
					$"MapIndexInput({sliceDescription} slice={offset}:{offset + length})",
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

	internal sealed class MapIndexInput : BufferedIndexInput
	{
		private readonly MappingOwner _owner;
		private readonly long _off;
		private readonly long _end;
		private int _disposed;

		public static MapIndexInput Open(string path, IOContext context)
		{
			long fileLength = new FileInfo(path).Length;
			MappingOwner owner = MappingOwner.Open(path, prefetchBytes: Math.Min(1L << 20, fileLength));
			try
			{
				int bufferSize = Math.Max(BufferedIndexInput.GetBufferSize(context), DefaultBufferSize);
				return new MapIndexInput(
					$"MapIndexInput(path=\"{path}\")",
					owner,
					0,
					owner.Length,
					bufferSize);
			}
			catch
			{
				owner.Dispose();
				throw;
			}
		}

		public MapIndexInput(string resourceDesc, MappingOwner owner, long off, long length, int bufferSize)
			: base(resourceDesc, bufferSize)
		{
			_owner = owner;
			_off = off;
			_end = off + length;
		}

		public override long Length => _end - _off;

		protected override void ReadInternal(byte[] b, int offset, int len)
		{
			long position = _off + Position;
			if (len < 0 || position < _off || position > _end || len > _end - position)
				throw new IOException("read past EOF: " + this);
			_owner.Copy(position, b, offset, len);
		}

		protected override void SeekInternal(long pos)
		{
		}

		public override object Clone()
		{
			_owner.AddRef();
			try
			{
				MapIndexInput clone = (MapIndexInput)base.Clone();
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
