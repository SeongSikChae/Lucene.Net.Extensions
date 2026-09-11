using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
	[TestClass]
	public class DecimalFieldBoundaryTests
	{
		private static readonly decimal NegativeZero = new(0, 0, 0, isNegative: true, scale: 0);
		private static readonly decimal NegativeZeroScale28 = new(0, 0, 0, isNegative: true, scale: 28);
		private static readonly decimal TinyPositive = 0.0000000000000000000000000001m;
		private static readonly decimal TinyNegative = -0.0000000000000000000000000001m;

		[TestMethod]
		public void BoundaryValuesRoundTripAndRangeEdges()
		{
			decimal[] values =
			[
				decimal.MinValue,
				decimal.MaxValue,
				0m,
				NegativeZero,
				NegativeZeroScale28,
				TinyPositive,
				TinyNegative,
				1m,
				1.0m,
				1.00m,
			];

			using RAMDirectory directory = new();
			using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE);
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

			Assert.AreEqual(1, searcher.Search(DecimalField.NewRangeQuery("amount", decimal.MinValue, decimal.MinValue), 10).TotalHits);
			Assert.AreEqual(0, searcher.Search(DecimalField.NewRangeQuery("amount", decimal.MinValue, decimal.MinValue, minInclusive: false, maxInclusive: true), 10).TotalHits);

			Assert.AreEqual(1, searcher.Search(DecimalField.NewRangeQuery("amount", decimal.MaxValue, decimal.MaxValue), 10).TotalHits);
			Assert.AreEqual(0, searcher.Search(DecimalField.NewRangeQuery("amount", decimal.MaxValue, decimal.MaxValue, minInclusive: true, maxInclusive: false), 10).TotalHits);

			Assert.AreEqual(1, searcher.Search(DecimalField.NewRangeQuery("amount", null, decimal.MinValue), 10).TotalHits);
			Assert.AreEqual(1, searcher.Search(DecimalField.NewRangeQuery("amount", decimal.MaxValue, null), 10).TotalHits);

			Assert.AreEqual(values.Length, searcher.Search(DecimalField.NewExistsQuery("amount"), 10).TotalHits);

			// Zero variants share the same sortable encoding; 1m / 1.0m / 1.00m share numeric equality.
			Assert.AreEqual(3, searcher.Search(DecimalField.NewExactQuery("amount", 0m), 10).TotalHits);
			Assert.AreEqual(3, searcher.Search(DecimalField.NewExactQuery("amount", 1m), 10).TotalHits);
			Assert.AreEqual(1, searcher.Search(DecimalField.NewExactQuery("amount", TinyPositive), 10).TotalHits);
			Assert.AreEqual(1, searcher.Search(DecimalField.NewExactQuery("amount", TinyNegative), 10).TotalHits);

			for (int i = 0; i < values.Length; i++)
			{
				TopDocs hits = searcher.Search(new TermQuery(new Term("id", i.ToString())), 1);
				decimal? stored = searcher.Doc(hits.ScoreDocs[0].Doc).GetDecimalValue("amount");
				Assert.IsNotNull(stored);
				CollectionAssert.AreEqual(
					decimal.GetBits(values[i]),
					decimal.GetBits(stored.Value),
					$"GetBits round-trip failed for index {i}, value={values[i]}");
			}
		}
	}
}
