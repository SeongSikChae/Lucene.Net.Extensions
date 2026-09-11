using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Net;

namespace Lucene.Net.Index
{
	[TestClass]
	public class SortableBytesRangeAccuracyTests
	{
		private const int RandomIterations = 10_000;

		[TestMethod]
		public void DecimalFieldRangeMatchesManagedComparison()
		{
			Random random = new(42);
			decimal[] values = new decimal[256];
			for (int i = 0; i < values.Length; i++)
				values[i] = NextDecimal(random);

			using RAMDirectory directory = new();
			using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE)
				.SetRAMBufferSizeMB(32);
			using (IndexWriter writer = new(directory, config))
			{
				for (int i = 0; i < values.Length; i++)
				{
					Document doc =
					[
						.. DecimalField.CreateFields("amount", values[i], Field.Store.YES),
						new StringField("id", i.ToString(), Field.Store.YES),
					];
					writer.AddDocument(doc);
				}
				writer.Commit();
				writer.ForceMerge(1);
			}

			using SearcherManager searcherManager = new(directory, null);
			using SearcherLease lease = new(searcherManager);
			IndexSearcher searcher = lease.Searcher;
			for (int iter = 0; iter < RandomIterations; iter++)
				{
					decimal a = values[random.Next(values.Length)];
					decimal b = values[random.Next(values.Length)];
					decimal min = a <= b ? a : b;
					decimal max = a <= b ? b : a;
					bool minInclusive = random.Next(2) == 0;
					bool maxInclusive = random.Next(2) == 0;

					HashSet<int> expected = [];
					for (int i = 0; i < values.Length; i++)
					{
						decimal v = values[i];
						bool geMin = minInclusive ? v >= min : v > min;
						bool leMax = maxInclusive ? v <= max : v < max;
						if (geMin && leMax)
							expected.Add(i);
					}

					Query query = DecimalField.NewRangeQuery("amount", min, max, minInclusive, maxInclusive);
					TopDocs hits = searcher.Search(query, values.Length + 1);
					HashSet<int> actual = [];
					foreach (ScoreDoc scoreDoc in hits.ScoreDocs)
					{
						string id = searcher.Doc(scoreDoc.Doc).Get("id");
						actual.Add(int.Parse(id));
					}

					CollectionAssert.AreEquivalent(
						expected.ToArray(),
						actual.ToArray(),
						$"iter={iter}, min={min}, max={max}, minInc={minInclusive}, maxInc={maxInclusive}");
				}
		}

		[TestMethod]
		public void IPAddressFieldRangeMatchesManagedComparison()
		{
			Random random = new(42);
			IPAddress[] values = new IPAddress[256];
			for (int i = 0; i < values.Length; i++)
				values[i] = NextIPAddress(random);

			using RAMDirectory directory = new();
			using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE)
				.SetRAMBufferSizeMB(32);
			using (IndexWriter writer = new(directory, config))
			{
				for (int i = 0; i < values.Length; i++)
				{
					Document doc =
					[
						.. IPAddressField.CreateFields("ip", values[i], Field.Store.YES),
						new StringField("id", i.ToString(), Field.Store.YES),
					];
					writer.AddDocument(doc);
				}
				writer.Commit();
				writer.ForceMerge(1);
			}

			using SearcherManager searcherManager = new(directory, null);
			using SearcherLease lease = new(searcherManager);
			IndexSearcher searcher = lease.Searcher;
			for (int iter = 0; iter < RandomIterations; iter++)
				{
					IPAddress a = values[random.Next(values.Length)];
					IPAddress b = values[random.Next(values.Length)];
					byte[] aBytes = a.ToSortableBytes();
					byte[] bBytes = b.ToSortableBytes();
					int cmp = aBytes.AsSpan().SequenceCompareTo(bBytes);
					IPAddress min = cmp <= 0 ? a : b;
					IPAddress max = cmp <= 0 ? b : a;
					bool minInclusive = random.Next(2) == 0;
					bool maxInclusive = random.Next(2) == 0;
					byte[] minBytes = min.ToSortableBytes();
					byte[] maxBytes = max.ToSortableBytes();

					HashSet<int> expected = [];
					for (int i = 0; i < values.Length; i++)
					{
						byte[] v = values[i].ToSortableBytes();
						int cMin = v.AsSpan().SequenceCompareTo(minBytes);
						int cMax = v.AsSpan().SequenceCompareTo(maxBytes);
						bool geMin = minInclusive ? cMin >= 0 : cMin > 0;
						bool leMax = maxInclusive ? cMax <= 0 : cMax < 0;
						if (geMin && leMax)
							expected.Add(i);
					}

					Query query = IPAddressField.NewRangeQuery("ip", min, max, minInclusive, maxInclusive);
					TopDocs hits = searcher.Search(query, values.Length + 1);
					HashSet<int> actual = [];
					foreach (ScoreDoc scoreDoc in hits.ScoreDocs)
					{
						string id = searcher.Doc(scoreDoc.Doc).Get("id");
						actual.Add(int.Parse(id));
					}

					CollectionAssert.AreEquivalent(
						expected.ToArray(),
						actual.ToArray(),
						$"iter={iter}, min={min}, max={max}, minInc={minInclusive}, maxInc={maxInclusive}");
				}
		}

		private static int NextInt32Bits(Random random)
		{
			Span<byte> bytes = stackalloc byte[4];
			random.NextBytes(bytes);
			return BitConverter.ToInt32(bytes);
		}

		private static decimal NextDecimal(Random random)
		{
			int lo = NextInt32Bits(random);
			int mid = NextInt32Bits(random);
			int hi = NextInt32Bits(random);
			bool negative = random.Next(2) == 0;
			byte scale = (byte)random.Next(0, 29);
			return new decimal(lo, mid, hi, negative, scale);
		}

		private static IPAddress NextIPAddress(Random random)
		{
			if (random.Next(2) == 0)
			{
				byte[] bytes = new byte[4];
				random.NextBytes(bytes);
				return new IPAddress(bytes);
			}

			byte[] v6 = new byte[16];
			random.NextBytes(v6);
			// Avoid IPv4-mapped form so family stays IPv6 after NormalizeForStorage.
			if (v6[0] == 0 && v6[1] == 0 && v6[2] == 0 && v6[3] == 0
				&& v6[4] == 0 && v6[5] == 0 && v6[6] == 0 && v6[7] == 0
				&& v6[8] == 0 && v6[9] == 0 && v6[10] == 0xff && v6[11] == 0xff)
			{
				v6[10] = 0xfe;
			}
			return new IPAddress(v6);
		}
	}
}
