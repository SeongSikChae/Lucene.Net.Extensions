using System.Runtime.Versioning;
using System.Text;

using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Util;

using LuceneDirectory = Lucene.Net.Store.Directory;
using IODirectory = System.IO.Directory;

namespace Lucene.Net.Store;

/// <summary>
/// Correctness coverage for Windows FSDirectory implementations
/// (<see cref="WinMMapDirectory"/>, <see cref="WinRandomAccessDirectory"/>, <see cref="WinHybridDirectory"/>).
/// </summary>
[TestClass]
[SupportedOSPlatform("windows")]
public class WinDirectoryCorrectnessTests
{
	private static readonly string[] DirectoryKinds =
	[
		"WinMMapDirectory",
		"WinRandomAccessDirectory",
		"WinHybridDirectory",
		"WinHybridDirectory(threshold=0)",
	];

	/// <summary>
	/// All directory kinds that should produce/consume the same on-disk Lucene format,
	/// including stock Lucene.NET <see cref="SimpleFSDirectory"/> as a baseline.
	/// </summary>
	private static readonly string[] InteropDirectoryKinds =
	[
		"SimpleFSDirectory",
		"WinMMapDirectory",
		"WinRandomAccessDirectory",
		"WinHybridDirectory",
		"WinHybridDirectory(threshold=0)",
	];

	[TestMethod]
	public void TruncateOnRecreate_ShrinksFileLength()
	{
		RequireWindows();

		foreach (string kind in DirectoryKinds)
		{
			using TempDir temp = new(kind);
			using LuceneDirectory directory = CreateDirectory(kind, temp.Path);

			byte[] longPayload = Encoding.UTF8.GetBytes(new string('A', 4096));
			WriteFile(directory, "data.bin", longPayload);

			byte[] shortPayload = Encoding.UTF8.GetBytes("short");
			WriteFile(directory, "data.bin", shortPayload);

			using IndexInput input = directory.OpenInput("data.bin", IOContext.DEFAULT);
			Assert.AreEqual(shortPayload.Length, input.Length, kind);

			byte[] actual = new byte[shortPayload.Length];
			input.ReadBytes(actual, 0, actual.Length);
			CollectionAssert.AreEqual(shortPayload, actual, kind);
		}
	}

	[TestMethod]
	public void EmptyFile_OpenLengthAndEof()
	{
		RequireWindows();

		foreach (string kind in DirectoryKinds)
		{
			using TempDir temp = new(kind);
			using LuceneDirectory directory = CreateDirectory(kind, temp.Path);

			WriteFile(directory, "empty.bin", []);

			using IndexInput input = directory.OpenInput("empty.bin", IOContext.DEFAULT);
			Assert.AreEqual(0, input.Length, kind);
			Assert.AreEqual(0, input.Position, kind);

			Assert.ThrowsException<EndOfStreamException>(
				() => input.ReadByte(),
				$"expected EOF on empty file for {kind}");
		}
	}

	[TestMethod]
	public void Slice_NegativeOffsetOrLength_Throws()
	{
		RequireWindows();

		foreach (string kind in DirectoryKinds)
		{
			using TempDir temp = new(kind);
			using LuceneDirectory directory = CreateDirectory(kind, temp.Path);
			WriteFile(directory, "data.bin", [1, 2, 3, 4, 5]);

			using LuceneDirectory.IndexInputSlicer slicer = directory.CreateSlicer("data.bin", IOContext.DEFAULT);

			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => slicer.OpenSlice("neg-off", offset: -1, length: 1),
				kind);
			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => slicer.OpenSlice("neg-len", offset: 0, length: -1),
				kind);
		}
	}

	[TestMethod]
	public void Slice_PastEnd_Throws()
	{
		RequireWindows();

		foreach (string kind in DirectoryKinds)
		{
			using TempDir temp = new(kind);
			using LuceneDirectory directory = CreateDirectory(kind, temp.Path);
			WriteFile(directory, "data.bin", [1, 2, 3, 4, 5]);

			using LuceneDirectory.IndexInputSlicer slicer = directory.CreateSlicer("data.bin", IOContext.DEFAULT);

			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => slicer.OpenSlice("past-end", offset: 3, length: 3),
				kind);
			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => slicer.OpenSlice("offset-past", offset: 6, length: 0),
				kind);
		}
	}

	[TestMethod]
	public void Slice_OffsetLengthOverflow_Throws()
	{
		RequireWindows();

		foreach (string kind in DirectoryKinds)
		{
			using TempDir temp = new(kind);
			using LuceneDirectory directory = CreateDirectory(kind, temp.Path);
			WriteFile(directory, "data.bin", [1, 2, 3, 4, 5]);

			using LuceneDirectory.IndexInputSlicer slicer = directory.CreateSlicer("data.bin", IOContext.DEFAULT);

			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => slicer.OpenSlice("overflow", offset: long.MaxValue - 1, length: 2),
				kind);
			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => slicer.OpenSlice("huge-length", offset: 0, length: long.MaxValue),
				kind);
		}
	}

	[TestMethod]
	public void Clone_SurvivesOriginalDispose()
	{
		RequireWindows();

		foreach (string kind in DirectoryKinds)
		{
			using TempDir temp = new(kind);
			using LuceneDirectory directory = CreateDirectory(kind, temp.Path);
			byte[] payload = Encoding.UTF8.GetBytes("clone-survives");
			WriteFile(directory, "data.bin", payload);

			IndexInput original = directory.OpenInput("data.bin", IOContext.DEFAULT);
			IndexInput clone = (IndexInput)original.Clone();
			original.Dispose();

			try
			{
				byte[] actual = new byte[payload.Length];
				clone.ReadBytes(actual, 0, actual.Length);
				CollectionAssert.AreEqual(payload, actual, kind);
			}
			finally
			{
				clone.Dispose();
			}
		}
	}

	[TestMethod]
	public void Slice_SurvivesSlicerDispose()
	{
		RequireWindows();

		foreach (string kind in DirectoryKinds)
		{
			using TempDir temp = new(kind);
			using LuceneDirectory directory = CreateDirectory(kind, temp.Path);
			byte[] payload = [10, 20, 30, 40, 50, 60];
			WriteFile(directory, "data.bin", payload);

			LuceneDirectory.IndexInputSlicer slicer = directory.CreateSlicer("data.bin", IOContext.DEFAULT);
			IndexInput slice = slicer.OpenSlice("mid", offset: 2, length: 3);
			slicer.Dispose();

			try
			{
				byte[] actual = new byte[3];
				slice.ReadBytes(actual, 0, actual.Length);
				CollectionAssert.AreEqual(new byte[] { 30, 40, 50 }, actual, kind);
			}
			finally
			{
				slice.Dispose();
			}
		}
	}

	/// <summary>
	/// Concurrent Read + Dispose on the same IndexInput is outside Lucene's concurrency model.
	/// This is a best-effort race (not a deterministic interleaving): we assert no crash/AV,
	/// and that any successful read matches the original payload.
	/// </summary>
	[TestMethod]
	public void Concurrent_DisposeAndRead_DoesNotCrashOrCorruptSuccessfulRead()
	{
		RequireWindows();

		foreach (string kind in DirectoryKinds)
		{
			using TempDir temp = new(kind);
			using LuceneDirectory directory = CreateDirectory(kind, temp.Path);
			byte[] payload = new byte[64 * 1024];
			new Random(123).NextBytes(payload);
			WriteFile(directory, "data.bin", payload);

			IndexInput original = directory.OpenInput("data.bin", IOContext.DEFAULT);
			IndexInput clone = (IndexInput)original.Clone();
			original.Dispose(); // clone is now the last owner

			Exception? readError = null;
			var readStarted = new ManualResetEventSlim(false);
			var disposeBarrier = new ManualResetEventSlim(false);

			var reader = Task.Run(() =>
			{
				try
				{
					byte[] actual = new byte[payload.Length];
					readStarted.Set();
					disposeBarrier.Wait();
					for (int i = 0; i < 50; i++)
					{
						clone.Seek(0);
						clone.ReadBytes(actual, 0, actual.Length);
						if (!actual.AsSpan().SequenceEqual(payload))
							throw new AssertFailedException($"{kind}: content mismatch on pass {i}");
					}
				}
				catch (Exception ex)
				{
					// Only already-closed during teardown is acceptable; other IOExceptions are real failures.
					if (!LuceneCloseExceptions.IsAlreadyClosed(ex))
						readError = ex;
				}
			});

			readStarted.Wait();
			disposeBarrier.Set();
			Thread.Sleep(1);
			clone.Dispose();
			reader.GetAwaiter().GetResult();

			Assert.IsNull(readError, $"{kind}: unexpected exception {readError}");
		}
	}

	[TestMethod]
	public void MmapOpen_AllowsDeleteAndReplace()
	{
		RequireWindows();

		using TempDir temp = new("replace");
		string fullPath = Path.Combine(temp.Path, "data.bin");
		File.WriteAllBytes(fullPath, Encoding.UTF8.GetBytes("original-content"));

		using var directory = new WinMMapDirectory(temp.Path);
		using IndexInput input = directory.OpenInput("data.bin", IOContext.DEFAULT);

		byte[] first = new byte[(int)input.Length];
		input.ReadBytes(first, 0, first.Length);
		CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("original-content"), first);

		// FileShare.Delete should allow replacing while the mapping remains open.
		File.Delete(fullPath);
		File.WriteAllBytes(fullPath, Encoding.UTF8.GetBytes("replaced"));

		input.Seek(0);
		byte[] stillMapped = new byte[first.Length];
		input.ReadBytes(stillMapped, 0, stillMapped.Length);
		CollectionAssert.AreEqual(first, stillMapped, "open mapping should keep original bytes");

		using IndexInput replaced = directory.OpenInput("data.bin", IOContext.DEFAULT);
		byte[] second = new byte[(int)replaced.Length];
		replaced.ReadBytes(second, 0, second.Length);
		CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("replaced"), second);
	}

	[TestMethod]
	public void Checksum_MatchesSimpleFSDirectory()
	{
		RequireWindows();

		byte[][] payloads =
		[
			[],
			[0x00],
			Encoding.UTF8.GetBytes("hello checksum"),
			Enumerable.Range(0, 10_000).Select(i => (byte)i).ToArray(),
		];

		foreach (string kind in DirectoryKinds)
		{
			foreach (byte[] payload in payloads)
			{
				using TempDir expectedTemp = new("expected");
				using TempDir actualTemp = new(kind);

				long expected;
				using (LuceneDirectory simple = new SimpleFSDirectory(expectedTemp.Path))
				{
					expected = WriteAndGetChecksum(simple, "c.bin", payload);
				}

				long actual;
				using (LuceneDirectory directory = CreateDirectory(kind, actualTemp.Path))
				{
					actual = WriteAndGetChecksum(directory, "c.bin", payload);
				}

				Assert.AreEqual(expected, actual, $"{kind} payloadLen={payload.Length}");
			}
		}
	}

	[TestMethod]
	public void HybridThresholdZero_EmptyFileUsesRandomAccessPath()
	{
		RequireWindows();

		using TempDir temp = new("hybrid0");
		using var directory = new WinHybridDirectory(new DirectoryInfo(temp.Path), null, mmapThresholdBytes: 0);
		WriteFile(directory, "empty.bin", []);

		using IndexInput input = directory.OpenInput("empty.bin", IOContext.DEFAULT);
		Assert.AreEqual(0, input.Length);
		Assert.ThrowsException<EndOfStreamException>(() => input.ReadByte());
	}

	[TestMethod]
	public void CrossDirectory_WriteThenRead_BytesMatch()
	{
		RequireWindows();

		byte[][] payloads =
		[
			[],
			Encoding.UTF8.GetBytes("cross-dir"),
			Enumerable.Range(0, 4096).Select(i => (byte)(i * 17)).ToArray(),
			// Straddle WinHybrid default mmap threshold (1 MiB).
			Enumerable.Range(0, 1_048_576 + 64).Select(i => (byte)i).ToArray(),
		];

		foreach (string writerKind in InteropDirectoryKinds)
		{
			foreach (string readerKind in InteropDirectoryKinds)
			{
				foreach (byte[] payload in payloads)
				{
					using TempDir temp = new($"{writerKind}_to_{readerKind}");
					string label = $"{writerKind} -> {readerKind}, len={payload.Length}";

					using (LuceneDirectory writer = CreateDirectory(writerKind, temp.Path))
						WriteFile(writer, "data.bin", payload);

					using LuceneDirectory reader = CreateDirectory(readerKind, temp.Path);
					using IndexInput input = reader.OpenInput("data.bin", IOContext.DEFAULT);

					Assert.AreEqual(payload.Length, input.Length, label);

					byte[] actual = new byte[payload.Length];
					if (payload.Length > 0)
						input.ReadBytes(actual, 0, actual.Length);
					CollectionAssert.AreEqual(payload, actual, label);

					if (payload.Length >= 5)
					{
						using LuceneDirectory.IndexInputSlicer slicer = reader.CreateSlicer("data.bin", IOContext.DEFAULT);
						using IndexInput slice = slicer.OpenSlice("mid", offset: 2, length: 3);
						byte[] sliceActual = new byte[3];
						slice.ReadBytes(sliceActual, 0, 3);
						CollectionAssert.AreEqual(
							new[] { payload[2], payload[3], payload[4] },
							sliceActual,
							label + " slice");
					}
				}
			}
		}
	}

	[TestMethod]
	public void CrossDirectory_WriteIndexThenSearch_HitsMatch()
	{
		RequireWindows();

		const int documentCount = 200;
		const int expectedEvenHits = documentCount / 2;

		foreach (string writerKind in InteropDirectoryKinds)
		{
			foreach (string readerKind in InteropDirectoryKinds)
			{
				using TempDir temp = new($"idx_{writerKind}_to_{readerKind}");
				string label = $"{writerKind} -> {readerKind}";

				using (LuceneDirectory writerDir = CreateDirectory(writerKind, temp.Path))
				{
					using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
					var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
						.SetOpenMode(OpenMode.CREATE)
						.SetRAMBufferSizeMB(16);

					using var writer = new IndexWriter(writerDir, config);
					for (int i = 0; i < documentCount; i++)
					{
						writer.AddDocument(new Document
						{
							new StringField("id", i.ToString("D4"), Field.Store.YES),
							new StringField("tag", (i & 1) == 0 ? "even" : "odd", Field.Store.NO),
							new TextField("body", $"doc {i} token-{i % 17}", Field.Store.YES),
						});
					}

					writer.ForceMerge(1);
					writer.Commit();
				}

				using LuceneDirectory readerDir = CreateDirectory(readerKind, temp.Path);
				using DirectoryReader reader = DirectoryReader.Open(readerDir);
				var searcher = new IndexSearcher(reader);

				Assert.AreEqual(documentCount, reader.NumDocs, label + " NumDocs");

				int evenHits = searcher.Search(new TermQuery(new Term("tag", "even")), documentCount).TotalHits;
				Assert.AreEqual(expectedEvenHits, evenHits, label + " even hits");

				TopDocs byId = searcher.Search(new TermQuery(new Term("id", "0042")), 1);
				Assert.AreEqual(1, byId.TotalHits, label + " id=0042");
				Document hit = searcher.Doc(byId.ScoreDocs[0].Doc);
				Assert.AreEqual("0042", hit.Get("id"), label);
			}
		}
	}

	private static long WriteAndGetChecksum(LuceneDirectory directory, string name, byte[] payload)
	{
		using IndexOutput output = directory.CreateOutput(name, IOContext.DEFAULT);
		if (payload.Length > 0)
			output.WriteBytes(payload, 0, payload.Length);
		return output.Checksum;
	}

	private static void WriteFile(LuceneDirectory directory, string name, byte[] payload)
	{
		using IndexOutput output = directory.CreateOutput(name, IOContext.DEFAULT);
		if (payload.Length > 0)
			output.WriteBytes(payload, 0, payload.Length);
	}

	private static LuceneDirectory CreateDirectory(string kind, string path) => kind switch
	{
		"SimpleFSDirectory" => new SimpleFSDirectory(path),
		"WinMMapDirectory" => new WinMMapDirectory(path),
		"WinRandomAccessDirectory" => new WinRandomAccessDirectory(path),
		"WinHybridDirectory" => new WinHybridDirectory(path),
		"WinHybridDirectory(threshold=0)" => new WinHybridDirectory(new DirectoryInfo(path), null, mmapThresholdBytes: 0),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
	};

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
				"WinDirCorrectness_" + label + "_" + Guid.NewGuid().ToString("N"));
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
