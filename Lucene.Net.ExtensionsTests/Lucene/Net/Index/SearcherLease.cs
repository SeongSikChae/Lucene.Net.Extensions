using Lucene.Net.Search;

namespace Lucene.Net.Index
{
	/// <summary>
	/// Ensures <see cref="SearcherManager.Acquire"/> is paired with <see cref="SearcherManager.Release"/>.
	/// </summary>
	internal sealed class SearcherLease : IDisposable
	{
		private readonly SearcherManager _manager;
		private bool _disposed;

		public IndexSearcher Searcher { get; }

		public SearcherLease(SearcherManager manager)
		{
			_manager = manager;
			Searcher = manager.Acquire();
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			_manager.Release(Searcher);
		}
	}
}
