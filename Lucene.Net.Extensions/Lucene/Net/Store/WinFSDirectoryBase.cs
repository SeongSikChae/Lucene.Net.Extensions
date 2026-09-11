using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Lucene.Net.Store;

/// <summary>
/// Shared Windows FSDirectory base: custom read in subclasses, common sequential write path.
/// Write path mirrors Lucene.NET FSIndexOutput (direct FileStream + CRC, no extra Lucene buffer),
/// with Windows <see cref="FileOptions.SequentialScan"/> and a larger OS write buffer.
/// </summary>
[SupportedOSPlatform("windows")]
public abstract class WinFSDirectoryBase : FSDirectory
{
	/// <summary>FileStream buffer size (OS-side). Larger than Lucene default for sequential segment writes.</summary>
	public const int WriteBufferSize = 256 * 1024;

	/// <summary>
	/// Initializes a new instance for the given directory path.
	/// </summary>
	/// <param name="path">Index directory on disk.</param>
	/// <param name="lockFactory">Lock factory, or <c>null</c> for the default.</param>
	protected WinFSDirectoryBase(DirectoryInfo path, LockFactory? lockFactory)
		: base(path, lockFactory!)
	{
	}

	/// <summary>
	/// Validates a slice against <paramref name="fileLength"/> without addition overflow.
	/// </summary>
	internal static void ValidateSlice(long fileLength, long offset, long length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(offset);
		ArgumentOutOfRangeException.ThrowIfNegative(length);
		if (offset > fileLength || length > fileLength - offset)
			throw new ArgumentOutOfRangeException(nameof(length));
	}

	/// <inheritdoc />
	public override IndexOutput CreateOutput(string name, IOContext context)
	{
		EnsureOpen();
		EnsureCanWrite(name);
		return new WinFSIndexOutput(this, name);
	}

	internal void MarkOutputClosed(string name)
	{
		lock (m_syncLock)
		{
			m_staleFiles.Add(name);
		}
	}

	/// <summary>
	/// Direct-to-FileStream <see cref="IndexOutput"/> (same strategy as Lucene.NET FSIndexOutput),
	/// tuned with SequentialScan + large FileStream buffer.
	/// </summary>
	internal sealed class WinFSIndexOutput : IndexOutput
	{
		private readonly WinFSDirectoryBase _parent;
		private readonly string _name;
		private readonly FileStream _file;
		private readonly ZlibCrc32 _crc = new();
		private int _disposed;

		public WinFSIndexOutput(WinFSDirectoryBase parent, string name)
		{
			_parent = parent;
			_name = name;
			_file = new FileStream(
				path: Path.Combine(parent.m_directory.FullName, name),
				mode: FileMode.Create,
				access: FileAccess.Write,
				share: FileShare.ReadWrite | FileShare.Delete,
				bufferSize: WriteBufferSize,
				options: FileOptions.SequentialScan);
		}

		public override void WriteByte(byte b)
		{
			EnsureOpen();
			_crc.Update(b);
			_file.WriteByte(b);
		}

		public override void WriteBytes(byte[] b, int offset, int length)
		{
			EnsureOpen();
			_crc.Update(b, offset, length);
			_file.Write(b, offset, length);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override void Flush()
		{
			EnsureOpen();
			_file.Flush();
		}

		[Obsolete("(4.1) this method will be removed in Lucene 5.0")]
		public override void Seek(long pos)
		{
			EnsureOpen();
			// Same as FSDirectory.FSIndexOutput: seek file pointer only.
			// Lucene 4.x does not recompute checksum on Seek; callers should not rely on Checksum after Seek.
			_file.Seek(pos, SeekOrigin.Begin);
		}

		public override long Length
		{
			get
			{
				EnsureOpen();
				return _file.Length;
			}
			set
			{
				EnsureOpen();
				_file.SetLength(value);
			}
		}

		public override long Checksum
		{
			get
			{
				EnsureOpen();
				return _crc.Value;
			}
		}

		public override long Position
		{
			get
			{
				EnsureOpen();
				return _file.Position;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing || Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			_parent.MarkOutputClosed(_name);

			Exception? prior = null;
			try
			{
				_file.Flush(flushToDisk: false);
			}
			catch (Exception ex) when (ex is IOException)
			{
				prior = ex;
			}
			finally
			{
				try
				{
					_file.Dispose();
				}
				catch (Exception ex) when (ex is IOException)
				{
					prior ??= ex;
				}
			}

			if (prior is not null)
				throw prior;
		}

		private void EnsureOpen()
		{
			if (Volatile.Read(ref _disposed) != 0)
				throw LuceneCloseExceptions.AlreadyClosed(GetType().FullName, "This WinFSIndexOutput is disposed.");
		}
	}
}
