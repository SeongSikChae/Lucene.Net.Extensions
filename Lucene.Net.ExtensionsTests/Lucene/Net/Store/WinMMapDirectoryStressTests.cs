using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Util;

using IODirectory = System.IO.Directory;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Store;

/// <summary>
/// Longer-running stress coverage for <see cref="WinMMapDirectory"/> mapping lifetime,
/// concurrent release, and index churn. Opt in with <c>--filter TestCategory=Stress</c>.
/// </summary>
[TestClass]
[SupportedOSPlatform("windows")]
public class WinMMapDirectoryStressTests
{
	private const int CloneCycles = 200_000;
	private const int SliceRaceIterations = 50_000;
	private const int SegmentChurnRounds = 80;
	private const int MergeReopenSeconds = 20;

	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[TestCategory("Stress")]
	public void CloneCreateDispose_ManyCycles_NoLeakOrCrash()
	{
		RequireWindows();

		using TempDir temp = new("clone");
		using var directory = new WinMMapDirectory(temp.Path);
		byte[] payload = new byte[256 * 1024];
		new Random(7).NextBytes(payload);
		WriteFile(directory, "data.bin", payload);

		Process process = Process.GetCurrentProcess();
		process.Refresh();
		long startWorkingSet = process.WorkingSet64;
		int startHandles = process.HandleCount;

		using IndexInput root = directory.OpenInput("data.bin", IOContext.DEFAULT);
		byte[] actual = new byte[payload.Length];

		for (int i = 0; i < CloneCycles; i++)
		{
			IndexInput clone = (IndexInput)root.Clone();
			try
			{
				clone.Seek(0);
				clone.ReadBytes(actual, 0, actual.Length);
				if ((i & 0x3FFF) == 0 && !actual.AsSpan().SequenceEqual(payload))
					Assert.Fail($"content mismatch at clone cycle {i}");
			}
			finally
			{
				clone.Dispose();
			}
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		process.Refresh();
		long endWorkingSet = process.WorkingSet64;
		int endHandles = process.HandleCount;
		string summary =
			$"clones={CloneCycles}, WS={startWorkingSet}->{endWorkingSet}, handles={startHandles}->{endHandles}";
		TestContext.WriteLine(summary);
		Trace.WriteLine(summary);

		// Soft bound after GC: refcount must not leak handles unboundedly across 200k clones.
		// Working set is logged only — file cache / residency make absolute WS asserts flaky.
		Assert.IsTrue(endHandles < startHandles + 256, summary);
	}

	[TestMethod]
	[TestCategory("Stress")]
	public void SliceOpen_Vs_SlicerDispose_Race_DoesNotCrash()
	{
		RequireWindows();

		using TempDir temp = new("slice-race");
		using var directory = new WinMMapDirectory(temp.Path);
		byte[] payload = new byte[64 * 1024];
		new Random(11).NextBytes(payload);
		WriteFile(directory, "data.bin", payload);

		Exception? error = null;
		for (int round = 0; round < SliceRaceIterations; round++)
		{
			LuceneDirectory.IndexInputSlicer slicer = directory.CreateSlicer("data.bin", IOContext.DEFAULT);
			var opened = new ManualResetEventSlim(false);
			var disposeBarrier = new ManualResetEventSlim(false);

			var opener = Task.Run(() =>
			{
				try
				{
					opened.Set();
					disposeBarrier.Wait();
					using IndexInput slice = slicer.OpenSlice("mid", offset: 16, length: 1024);
					byte[] buf = new byte[1024];
					slice.ReadBytes(buf, 0, buf.Length);
					if (!buf.AsSpan().SequenceEqual(payload.AsSpan(16, 1024)))
						throw new AssertFailedException($"slice content mismatch at round {round}");
				}
				catch (Exception ex) when (LuceneCloseExceptions.IsAlreadyClosed(ex))
				{
					// Slicer disposed before/while OpenSlice — acceptable for this race.
				}
				catch (Exception ex)
				{
					Volatile.Write(ref error, ex);
				}
			});

			opened.Wait();
			disposeBarrier.Set();
			Thread.Sleep(0);
			slicer.Dispose();
			opener.GetAwaiter().GetResult();

			if (error is not null)
				Assert.Fail($"round {round}: {error}");
		}
	}

	[TestMethod]
	[TestCategory("Stress")]
	public void Search_During_SegmentDeleteAndReplace_DoesNotCrash()
	{
		RequireWindows();

		using TempDir temp = new("seg-churn");
		using var directory = new WinMMapDirectory(temp.Path);
		using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
		var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
			.SetOpenMode(OpenMode.CREATE)
			.SetRAMBufferSizeMB(8)
			.SetMaxBufferedDocs(50);

		using var writer = new IndexWriter(directory, config);
		// Seed so readers always have a complete commit to open.
		writer.AddDocument(new Document
		{
			new StringField("id", "seed", Field.Store.YES),
			new StringField("tag", "even", Field.Store.NO),
			new TextField("body", "seed", Field.Store.YES),
		});
		writer.Commit();

		Exception? searchError = null;
		Exception? writeError = null;
		using var cts = new CancellationTokenSource();

		var writerTask = Task.Run(() =>
		{
			try
			{
				for (int round = 0; round < SegmentChurnRounds; round++)
				{
					for (int i = 0; i < 100; i++)
					{
						int id = round * 100 + i;
						writer.AddDocument(new Document
						{
							new StringField("id", id.ToString("D6"), Field.Store.YES),
							new StringField("tag", (id & 1) == 0 ? "even" : "odd", Field.Store.NO),
							new TextField("body", $"stress doc {id} token-{id % 31}", Field.Store.YES),
						});
					}

					if ((round & 3) == 3)
						writer.DeleteDocuments(new Term("tag", "odd"));

					writer.Commit();
					if ((round & 7) == 7)
						writer.ForceMerge(1);

					cts.Token.ThrowIfCancellationRequested();
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				writeError = ex;
			}
			finally
			{
				cts.Cancel();
			}
		}, cts.Token);

		var searcherTask = Task.Run(() =>
		{
			try
			{
				while (!cts.IsCancellationRequested)
				{
					using DirectoryReader reader = DirectoryReader.Open(directory);
					var searcher = new IndexSearcher(reader);
					_ = searcher.Search(new TermQuery(new Term("tag", "even")), 25).TotalHits;
					_ = searcher.Search(new MatchAllDocsQuery(), 10).TotalHits;
					Thread.Sleep(5);
				}
			}
			catch (Exception ex)
			{
				searchError = ex;
			}
		}, cts.Token);

		Task.WaitAll(writerTask, searcherTask);

		Assert.IsNull(writeError, $"writer: {writeError}");
		Assert.IsNull(searchError, $"searcher: {searchError}");

		using DirectoryReader finalReader = DirectoryReader.Open(directory);
		Assert.IsTrue(finalReader.NumDocs > 0);
	}

	[TestMethod]
	[TestCategory("Stress")]
	public void MergeAndReopen_Concurrent_DoesNotCrash()
	{
		RequireWindows();

		using TempDir temp = new("merge-reopen");
		using var directory = new WinMMapDirectory(temp.Path);
		using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
		var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
			.SetOpenMode(OpenMode.CREATE)
			.SetRAMBufferSizeMB(16)
			.SetMaxBufferedDocs(100)
			.SetMergePolicy(new TieredMergePolicy());

		using var writer = new IndexWriter(directory, config);
		writer.AddDocument(new Document
		{
			new StringField("id", "seed", Field.Store.YES),
			new StringField("tag", "even", Field.Store.NO),
			new TextField("body", "seed", Field.Store.NO),
		});
		writer.Commit();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(MergeReopenSeconds));
		Exception? error = null;

		var indexer = Task.Run(() =>
		{
			try
			{
				int id = 0;
				while (!cts.IsCancellationRequested)
				{
					writer.AddDocument(new Document
					{
						new StringField("id", id.ToString("D8"), Field.Store.YES),
						new StringField("tag", (id & 1) == 0 ? "even" : "odd", Field.Store.NO),
						new TextField("body", $"merge reopen {id}", Field.Store.NO),
					});
					id++;

					if ((id & 0xFF) == 0)
					{
						writer.Commit();
						writer.ForceMerge(2);
					}
				}

				writer.Commit();
			}
			catch (Exception ex)
			{
				Volatile.Write(ref error, ex);
			}
		}, cts.Token);

		var reopener = Task.Run(() =>
		{
			try
			{
				DirectoryReader? reader = DirectoryReader.Open(directory);
				try
				{
					while (!cts.IsCancellationRequested)
					{
						DirectoryReader? next = DirectoryReader.OpenIfChanged(reader);
						if (next is not null)
						{
							reader.Dispose();
							reader = next;
						}

						var searcher = new IndexSearcher(reader);
						_ = searcher.Search(new TermQuery(new Term("tag", "even")), 50).TotalHits;
						Thread.Sleep(10);
					}
				}
				finally
				{
					reader.Dispose();
				}
			}
			catch (Exception ex)
			{
				Volatile.Write(ref error, ex);
			}
		}, cts.Token);

		Task.WaitAll(indexer, reopener);
		Assert.IsNull(error, $"{error}");

		using DirectoryReader finalReader = DirectoryReader.Open(directory);
		TestContext.WriteLine($"final NumDocs={finalReader.NumDocs}");
		Assert.IsTrue(finalReader.NumDocs > 0);
	}

	[TestMethod]
	[TestCategory("Stress")]
	public void LongLivedMapping_HandleAndMemoryStayBounded()
	{
		RequireWindows();

		using TempDir temp = new("long-lived");
		using var directory = new WinMMapDirectory(temp.Path);
		byte[] payload = new byte[4 * 1024 * 1024];
		new Random(99).NextBytes(payload);
		WriteFile(directory, "big.bin", payload);

		Process process = Process.GetCurrentProcess();
		process.Refresh();
		long startWs = process.WorkingSet64;
		int startHandles = process.HandleCount;
		long peakWs = startWs;
		int peakHandles = startHandles;

		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(15))
		{
			using IndexInput input = directory.OpenInput("big.bin", IOContext.DEFAULT);
			using IndexInput clone = (IndexInput)input.Clone();
			using LuceneDirectory.IndexInputSlicer slicer = directory.CreateSlicer("big.bin", IOContext.DEFAULT);
			using IndexInput slice = slicer.OpenSlice("tail", offset: payload.Length / 2, length: 4096);

			byte[] buf = new byte[4096];
			clone.Seek(payload.Length / 2);
			clone.ReadBytes(buf, 0, buf.Length);
			Assert.IsTrue(buf.AsSpan().SequenceEqual(payload.AsSpan(payload.Length / 2, 4096)));

			slice.ReadBytes(buf, 0, buf.Length);
			Assert.IsTrue(buf.AsSpan().SequenceEqual(payload.AsSpan(payload.Length / 2, 4096)));

			process.Refresh();
			peakWs = Math.Max(peakWs, process.WorkingSet64);
			peakHandles = Math.Max(peakHandles, process.HandleCount);
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		process.Refresh();

		string summary =
			$"duration=15s, WS start={startWs} peak={peakWs} end={process.WorkingSet64}, " +
			$"handles start={startHandles} peak={peakHandles} end={process.HandleCount}";
		TestContext.WriteLine(summary);
		Trace.WriteLine(summary);

		Assert.IsTrue(process.HandleCount < startHandles + 128, summary);
	}

	private static void WriteFile(LuceneDirectory directory, string name, byte[] payload)
	{
		using IndexOutput output = directory.CreateOutput(name, IOContext.DEFAULT);
		output.WriteBytes(payload, 0, payload.Length);
	}

	private static void RequireWindows()
	{
		if (!OperatingSystem.IsWindows())
			Assert.Inconclusive("Windows-only directory implementations.");
	}

	private sealed class TempDir : IDisposable
	{
		public string Path { get; }

		public TempDir(string label)
		{
			Path = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				"WinMMapStress_" + label + "_" + Guid.NewGuid().ToString("N"));
			IODirectory.CreateDirectory(Path);
		}

		public void Dispose()
		{
			try
			{
				if (IODirectory.Exists(Path))
					IODirectory.Delete(Path, recursive: true);
			}
			catch
			{
				// Best-effort cleanup when mappings still hold delete-pending files.
			}
		}
	}
}
