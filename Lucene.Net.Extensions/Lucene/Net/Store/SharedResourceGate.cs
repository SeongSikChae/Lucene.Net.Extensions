namespace Lucene.Net.Store;

/// <summary>
/// Shared lifetime for a native/file resource used by multiple IndexInput clones.
/// Ownership refs keep the resource alive; active readers block final teardown.
/// </summary>
public sealed class SharedResourceGate
{
	private int _owners = 1;
	private int _readers;
	private int _closed; // 0 open, 1 closed
	private readonly ManualResetEventSlim _noReaders = new(initialState: true);

	public void AddOwner()
	{
		if (Volatile.Read(ref _closed) != 0)
			throw LuceneCloseExceptions.AlreadyClosed(nameof(SharedResourceGate), "Resource is closed.");

		int next = Interlocked.Increment(ref _owners);
		if (next <= 1 || Volatile.Read(ref _closed) != 0)
		{
			Interlocked.Decrement(ref _owners);
			throw LuceneCloseExceptions.AlreadyClosed(nameof(SharedResourceGate), "Resource is closed.");
		}
	}

	/// <summary>
	/// Returns true when this call should run the final cleanup.
	/// </summary>
	public bool ReleaseOwner()
	{
		int remaining = Interlocked.Decrement(ref _owners);
		if (remaining > 0)
			return false;
		if (remaining < 0)
			throw new InvalidOperationException("SharedResourceGate owner underflow.");

		Interlocked.Exchange(ref _closed, 1);
		while (Volatile.Read(ref _readers) != 0)
			_noReaders.Wait();
		return true;
	}

	public ReaderScope EnterRead()
	{
		if (Volatile.Read(ref _closed) != 0)
			throw LuceneCloseExceptions.AlreadyClosed(nameof(SharedResourceGate), "Resource is closed.");

		int readers = Interlocked.Increment(ref _readers);
		if (readers == 1)
			_noReaders.Reset();

		if (Volatile.Read(ref _closed) != 0)
		{
			LeaveRead();
			throw LuceneCloseExceptions.AlreadyClosed(nameof(SharedResourceGate), "Resource is closed.");
		}

		return new ReaderScope(this);
	}

	private void LeaveRead()
	{
		if (Interlocked.Decrement(ref _readers) == 0)
			_noReaders.Set();
	}

	public ref struct ReaderScope
	{
		private SharedResourceGate? _gate;

		public ReaderScope(SharedResourceGate gate) => _gate = gate;

		public void Dispose()
		{
			SharedResourceGate? gate = _gate;
			_gate = null;
			gate?.LeaveRead();
		}
	}
}
