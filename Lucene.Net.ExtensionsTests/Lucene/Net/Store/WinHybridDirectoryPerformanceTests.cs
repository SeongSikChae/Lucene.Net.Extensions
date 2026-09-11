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
/// Compares read/search throughput of <see cref="WinHybridDirectory"/> against
/// Lucene.NET <see cref="SimpleFSDirectory"/>, <see cref="NIOFSDirectory"/>, and <see cref="MMapDirectory"/>.
/// </summary>
[TestClass]
[SupportedOSPlatform("windows")]
public class WinHybridDirectoryPerformanceTests
{
	private const int DocumentCount = 20_000;
	private const int WarmupPasses = 2;
	private const int MeasuredPasses = 5;
	private const int SearchIterationsPerPass = 80;
	private const int SequentialReadPasses = 10;
	private const int RandomReadPasses = 10;
	private const int RandomReadOpsPerPass = 8_000;

	private static readonly string[] DirectoryKinds =
		["SimpleFSDirectory", "NIOFSDirectory", "MMapDirectory", "WinHybridDirectory"];

	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[TestCategory("Performance")]
	public void CompareReadAndSearchThroughput()
	{
		if (!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("WinHybridDirectory is Windows-only.");
			return;
		}

		string root = Path.Combine(Path.GetTempPath(), "WinHybridPerf_" + Guid.NewGuid().ToString("N"));
		IODirectory.CreateDirectory(root);

		try
		{
			string sourceIndex = Path.Combine(root, "_source");
			BuildIndex(sourceIndex);

			var results = new List<DirectoryBenchmarkResult>(DirectoryKinds.Length);

			foreach (string kind in DirectoryKinds)
			{
				string indexPath = Path.Combine(root, kind);
				CopyDirectory(sourceIndex, indexPath);
				results.Add(Benchmark(kind, indexPath));
			}

			string report = FormatReport(results);
			string reportPath = Path.Combine(Path.GetTempPath(), "WinHybridDirectory-performance-report.txt");
			System.IO.File.WriteAllText(reportPath, report);
			Trace.WriteLine(report);
			TestContext.WriteLine(report);
			TestContext.AddResultFile(reportPath);

			DirectoryBenchmarkResult? win = results.Find(r => r.Kind == "WinHybridDirectory");
			DirectoryBenchmarkResult? simple = results.Find(r => r.Kind == "SimpleFSDirectory");
			DirectoryBenchmarkResult? nio = results.Find(r => r.Kind == "NIOFSDirectory");
			DirectoryBenchmarkResult? mmap = results.Find(r => r.Kind == "MMapDirectory");
			Assert.IsNotNull(win);
			Assert.IsNotNull(simple);
			Assert.IsNotNull(nio);
			Assert.IsNotNull(mmap);
			Assert.IsTrue(win.HitsPerSearch > 0, "search should return hits");
			Assert.AreEqual(simple.HitsPerSearch, win.HitsPerSearch, "hit counts should match across directories");
			Assert.AreEqual(nio.HitsPerSearch, win.HitsPerSearch, "hit counts should match across directories");
			Assert.AreEqual(mmap.HitsPerSearch, win.HitsPerSearch, "hit counts should match across directories");
		}
		finally
		{
			TryDeleteDirectory(root);
		}
	}

	private static DirectoryBenchmarkResult Benchmark(string kind, string indexPath)
	{
		using LuceneDirectory directory = CreateDirectory(kind, indexPath);

		long sequentialMs = MeasureMs(SequentialReadPasses, () => SequentialReadAllFiles(directory));
		long randomMs = MeasureMs(RandomReadPasses, () => RandomReadFiles(directory));
		(long searchMs, int hitsPerSearch) = MeasureSearch(directory);

		return new DirectoryBenchmarkResult(kind, sequentialMs, randomMs, searchMs, hitsPerSearch);
	}

	private static (long SearchMs, int HitsPerSearch) MeasureSearch(LuceneDirectory directory)
	{
		using DirectoryReader reader = DirectoryReader.Open(directory);
		IndexSearcher searcher = new IndexSearcher(reader);
		Query query = new TermQuery(new Term("tag", "even"));

		for (int i = 0; i < WarmupPasses; i++)
		{
			for (int j = 0; j < SearchIterationsPerPass; j++)
				_ = searcher.Search(query, 100).TotalHits;
		}

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		int hits = 0;
		Stopwatch sw = Stopwatch.StartNew();
		for (int i = 0; i < MeasuredPasses; i++)
		{
			for (int j = 0; j < SearchIterationsPerPass; j++)
				hits = searcher.Search(query, 100).TotalHits;
		}
		sw.Stop();

		return (sw.ElapsedMilliseconds, hits);
	}

	private static long MeasureMs(int passes, Action action)
	{
		for (int i = 0; i < WarmupPasses; i++)
			action();

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Stopwatch sw = Stopwatch.StartNew();
		for (int i = 0; i < passes; i++)
			action();
		sw.Stop();
		return sw.ElapsedMilliseconds;
	}

	private static void SequentialReadAllFiles(LuceneDirectory directory)
	{
		foreach (string name in directory.ListAll())
		{
			using IndexInput input = directory.OpenInput(name, IOContext.DEFAULT);
			byte[] buffer = new byte[16 * 1024];
			long remaining = input.Length;
			while (remaining > 0)
			{
				int toRead = (int)Math.Min(buffer.Length, remaining);
				input.ReadBytes(buffer, 0, toRead);
				remaining -= toRead;
			}
		}
	}

	private static void RandomReadFiles(LuceneDirectory directory)
	{
		string[] names = directory.ListAll();
		if (names.Length == 0)
			return;

		var rng = new Random(42);
		byte[] buffer = new byte[64];
		IndexInput[] inputs = new IndexInput[names.Length];

		try
		{
			for (int i = 0; i < names.Length; i++)
				inputs[i] = directory.OpenInput(names[i], IOContext.DEFAULT);

			for (int i = 0; i < RandomReadOpsPerPass; i++)
			{
				IndexInput input = inputs[rng.Next(inputs.Length)];
				if (input.Length == 0)
					continue;

				long pos = NextInt64(rng, input.Length);
				input.Seek(pos);
				int len = (int)Math.Min(buffer.Length, input.Length - pos);
				if (len > 0)
					input.ReadBytes(buffer, 0, len);
			}
		}
		finally
		{
			foreach (IndexInput? input in inputs)
				input?.Dispose();
		}
	}

	private static long NextInt64(Random rng, long maxExclusive) => rng.NextInt64(maxExclusive);

	private static void BuildIndex(string indexPath)
	{
		IODirectory.CreateDirectory(indexPath);
		using LuceneDirectory directory = new SimpleFSDirectory(indexPath);
		using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
		var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
			.SetOpenMode(OpenMode.CREATE)
			.SetRAMBufferSizeMB(64)
			.SetMergePolicy(new TieredMergePolicy());

		using (var writer = new IndexWriter(directory, config))
		{
			for (int i = 0; i < DocumentCount; i++)
			{
				var doc = new Document
				{
					new StringField("id", i.ToString("D8"), Field.Store.YES),
					new StringField("tag", (i & 1) == 0 ? "even" : "odd", Field.Store.NO),
					new TextField(
						"body",
						$"document {i} lucene windows hybrid performance sample text token-{i % 97} token-{(i * 13) % 193}",
						Field.Store.YES),
				};
				writer.AddDocument(doc);
			}

			writer.ForceMerge(1);
			writer.Commit();
		}
	}

	private static LuceneDirectory CreateDirectory(string kind, string indexPath) => kind switch
	{
		"SimpleFSDirectory" => new SimpleFSDirectory(indexPath),
		"NIOFSDirectory" => new NIOFSDirectory(indexPath),
		"MMapDirectory" => new MMapDirectory(indexPath),
		"WinHybridDirectory" => new WinHybridDirectory(indexPath),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown directory kind."),
	};

	private static void CopyDirectory(string source, string destination)
	{
		IODirectory.CreateDirectory(destination);
		foreach (string file in IODirectory.EnumerateFiles(source))
		{
			string name = Path.GetFileName(file);
			System.IO.File.Copy(file, Path.Combine(destination, name), overwrite: true);
		}
	}

	private static void TryDeleteDirectory(string path)
	{
		try
		{
			if (IODirectory.Exists(path))
				IODirectory.Delete(path, recursive: true);
		}
		catch
		{
			// Best-effort cleanup for locked files after mmap dispose.
		}
	}

	private static string FormatReport(IReadOnlyList<DirectoryBenchmarkResult> results)
	{
		var sb = new StringBuilder();
		sb.AppendLine("WinHybridDirectory performance comparison");
		sb.AppendLine($"Documents={DocumentCount}, warmup={WarmupPasses}, measuredPasses={MeasuredPasses}, searchIters/pass={SearchIterationsPerPass}");
		sb.AppendLine();
		sb.AppendLine($"{"Directory",-22} {"Sequential(ms)",14} {"Random(ms)",12} {"Search(ms)",12} {"Hits",8}");
		sb.AppendLine(new string('-', 72));

		DirectoryBenchmarkResult? baseline = results.FirstOrDefault(r => r.Kind == "SimpleFSDirectory");
		foreach (DirectoryBenchmarkResult r in results)
		{
			sb.AppendLine($"{r.Kind,-22} {r.SequentialReadMs,14} {r.RandomReadMs,12} {r.SearchMs,12} {r.HitsPerSearch,8}");
		}

		if (baseline is not null)
		{
			sb.AppendLine();
			sb.AppendLine("Relative to SimpleFSDirectory (lower is faster):");
			foreach (DirectoryBenchmarkResult r in results)
			{
				sb.AppendLine(
					$"{r.Kind,-22} seq={Ratio(r.SequentialReadMs, baseline.SequentialReadMs)}  " +
					$"rand={Ratio(r.RandomReadMs, baseline.RandomReadMs)}  " +
					$"search={Ratio(r.SearchMs, baseline.SearchMs)}");
			}
		}

		return sb.ToString();
	}

	private static string Ratio(long value, long baseline)
	{
		if (baseline <= 0)
			return "n/a";
		return (value / (double)baseline).ToString("0.00x");
	}

	private sealed record DirectoryBenchmarkResult(
		string Kind,
		long SequentialReadMs,
		long RandomReadMs,
		long SearchMs,
		int HitsPerSearch);
}
