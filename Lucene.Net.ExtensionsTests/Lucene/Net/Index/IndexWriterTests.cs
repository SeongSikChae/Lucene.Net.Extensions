using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System.Net;

namespace Lucene.Net.Index
{
	[TestClass]
	public class IndexWriterTests
	{
		private static readonly decimal[] DecimalRangeInclusiveExpected = [10.5m, 20.0m, 30.25m];
		private static readonly decimal[] DecimalRangeOpenMinExpected = [-5.5m, 10.5m];
		private static readonly decimal[] DecimalRangeOpenMaxExpected = [30.25m, 100.99m];
		private static readonly string[] IPAddressRangeInclusiveExpected = ["192.168.0.1", "192.168.0.10", "192.168.0.100"];
		private static readonly string[] IPAddressIPv6RangeInclusiveExpected = ["2001:db8::1", "2001:db8::10", "2001:db8::100"];
		[TestMethod]
		public void SByteFieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new SByteField("A", sbyte.MinValue, Field.Store.YES), new SByteField("B", sbyte.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.INT32);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			sbyte? aValue = aField.GetSByteValue();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(sbyte.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			sbyte? bValue = bField.GetSByteValue();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(sbyte.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void ByteFieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new ByteField("A", byte.MinValue, Field.Store.YES), new ByteField("B", byte.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.INT32);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			byte? aValue = aField.GetByteValue();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(byte.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			byte? bValue = bField.GetByteValue();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(byte.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void Int16FieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new Int16Field("A", short.MinValue, Field.Store.YES), new Int16Field("B", short.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.INT32);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			short? aValue = aField.GetInt16Value();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(short.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			short? bValue = bField.GetInt16Value();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(short.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void UInt16FieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new UInt16Field("A", ushort.MinValue, Field.Store.YES), new UInt16Field("B", ushort.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.INT32);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			ushort? aValue = aField.GetUInt16Value();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(ushort.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			ushort? bValue = bField.GetUInt16Value();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(ushort.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void Int32FieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new Int32Field("A", int.MinValue, Field.Store.YES), new Int32Field("B", int.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.INT32);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			int? aValue = aField.GetInt32Value();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(int.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			int? bValue = bField.GetInt32Value();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(int.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void UInt32FieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new UInt32Field("A", uint.MinValue, Field.Store.YES), new UInt32Field("B", uint.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.INT32);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			uint? aValue = aField.GetUInt32Value();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(uint.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			uint? bValue = bField.GetUInt32Value();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(uint.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void Int64FieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new Int64Field("A", long.MinValue, Field.Store.YES), new Int64Field("B", long.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.INT64);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			long? aValue = aField.GetInt64Value();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(long.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			long? bValue = bField.GetInt64Value();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(long.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void UInt64FieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new UInt64Field("A", ulong.MinValue, Field.Store.YES), new UInt64Field("B", ulong.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.INT64);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			ulong? aValue = aField.GetUInt64Value();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(ulong.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			ulong? bValue = bField.GetUInt64Value();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(ulong.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void HalfFieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new HalfField("A", Half.MinValue, Field.Store.YES), new HalfField("B", Half.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.SINGLE);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			Half? aValue = aField.GetHalfValue();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(Half.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			Half? bValue = bField.GetHalfValue();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(Half.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void SingleFieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new SingleField("A", float.MinValue, Field.Store.YES), new SingleField("B", float.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.SINGLE);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			float? aValue = aField.GetSingleValue();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(float.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			float? bValue = bField.GetSingleValue();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(float.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void DoubleFieldTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new DoubleField("A", double.MinValue, Field.Store.YES), new DoubleField("B", double.MaxValue, Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			SortField field = new SortField("A", SortFieldType.DOUBLE);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			double? aValue = aField.GetDoubleValue();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(double.MinValue, aValue.Value);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			double? bValue = bField.GetDoubleValue();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(double.MaxValue, bValue.Value);
		}

		[TestMethod]
		public void IPAddressFieldIPv4Test()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
                ..IPAddressField.CreateFields("A", IPAddress.Parse("0.0.0.0"), Field.Store.YES),
                ..IPAddressField.CreateSortValueFields("A", IPAddress.Parse("0.0.0.0")),
                ..IPAddressField.CreateFields("B", IPAddress.Parse("255.255.255.255"), Field.Store.YES),
                ..IPAddressField.CreateSortValueFields("B", IPAddress.Parse("255.255.255.255")),
			];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			Sort sort = new Sort([..IPAddressField.CreateSortField("A")]);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			Document storedDoc = searcher.Doc(topDocs.ScoreDocs[0].Doc);
			IPAddress? aValue = storedDoc.GetIPAddressValue("A");

			Assert.IsNotNull(aValue);
			Assert.AreEqual(IPAddress.Parse("0.0.0.0"), aValue);

			IPAddress? bValue = storedDoc.GetIPAddressValue("B");

			Assert.IsNotNull(bValue);
			Assert.AreEqual(IPAddress.Parse("255.255.255.255"), bValue);
		}

		[TestMethod]
		public void IPAddressFieldIPv6Test()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			IPAddress ipv6 = IPAddress.Parse("2001:db8::1");
			Document doc =
			[
				.. IPAddressField.CreateFields("A", ipv6, Field.Store.YES),
				.. IPAddressField.CreateSortValueFields("A", ipv6),
			];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			Sort sort = new Sort([..IPAddressField.CreateSortField("A")]);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			Document storedDoc = searcher.Doc(topDocs.ScoreDocs[0].Doc);
			IPAddress? aValue = storedDoc.GetIPAddressValue("A");

			Assert.IsNotNull(aValue);
			Assert.AreEqual(ipv6, aValue);
		}

        [TestMethod]
        public void IPAddressFieldSortTest()
        {
            using RAMDirectory directory = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
                .SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
                .SetMergePolicy(new TieredMergePolicy());
            using IndexWriter writer = new IndexWriter(directory, config);

            writer.Commit();
			{
                Document doc = [
					..IPAddressField.CreateFields("A", IPAddress.Parse("192.168.0.4"), Field.Store.YES),
					..IPAddressField.CreateSortValueFields("A", IPAddress.Parse("192.168.0.4")),
				];
                writer.AddDocument(doc);
            }
            {
                Document doc = [
                    ..IPAddressField.CreateFields("A", IPAddress.Parse("192.168.0.6"), Field.Store.YES),
                    ..IPAddressField.CreateSortValueFields("A", IPAddress.Parse("192.168.0.6")),
                ];
                writer.AddDocument(doc);
            }
            {
                Document doc = [
                    ..IPAddressField.CreateFields("A", IPAddress.Parse("192.168.0.5"), Field.Store.YES),
                    ..IPAddressField.CreateSortValueFields("A", IPAddress.Parse("192.168.0.5")),
                ];
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.ForceMerge(1);

            using SearcherManager searcherManager = new SearcherManager(directory, null);
            using SearcherLease lease = new SearcherLease(searcherManager);
            IndexSearcher searcher = lease.Searcher;

            Sort sort = new Sort([.. IPAddressField.CreateSortField("A")]);
            TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
            searcher.Search(new MatchAllDocsQuery(), collector);
            TopDocs topDocs = collector.GetTopDocs();
            Document storedDoc = searcher.Doc(topDocs.ScoreDocs[1].Doc);
            IPAddress? aValue = storedDoc.GetIPAddressValue("A");

            Assert.IsNotNull(aValue);
            Assert.AreEqual("192.168.0.5", aValue.ToString());
        }

        [TestMethod]
		public void DecimalFieldTest()
		{
            using RAMDirectory directory = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
                .SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
                .SetMergePolicy(new TieredMergePolicy());
            using IndexWriter writer = new IndexWriter(directory, config);

            writer.Commit();

			Document doc =
			[
				.. DecimalField.CreateFields("A", 123, Field.Store.YES),
				.. DecimalField.CreateSortValueFields("A", 123),
			];
            writer.AddDocument(doc);
            writer.Commit();
            writer.ForceMerge(1);

            using SearcherManager searcherManager = new SearcherManager(directory, null);
            using SearcherLease lease = new SearcherLease(searcherManager);
            IndexSearcher searcher = lease.Searcher;

            Sort sort = new Sort([.. DecimalField.CreateSortField("A")]);
            TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
            searcher.Search(new MatchAllDocsQuery(), collector);
            TopDocs topDocs = collector.GetTopDocs();
            Document storedDoc = searcher.Doc(topDocs.ScoreDocs[0].Doc);

			decimal? value = storedDoc.GetDecimalValue("A");
			Assert.IsNotNull(value);
			Assert.AreEqual(123, value.Value);
        }

		[TestMethod]
		public void DecimalFieldRangeQueryTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			foreach (decimal amount in new[] { -5.5m, 10.5m, 20.0m, 30.25m, 100.99m })
			{
				Document doc = [.. DecimalField.CreateFields("amount", amount, Field.Store.YES)];
				writer.AddDocument(doc);
			}
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			Query inclusive = DecimalField.NewRangeQuery("amount", 10.5m, 30.25m);
			TopDocs inclusiveHits = searcher.Search(inclusive, 10);
			Assert.AreEqual(3, inclusiveHits.TotalHits);
			HashSet<decimal> inclusiveValues = GetDecimalValues(searcher, inclusiveHits, "amount");
			CollectionAssert.AreEquivalent(DecimalRangeInclusiveExpected, inclusiveValues.ToArray());

			Query exclusive = DecimalField.NewRangeQuery("amount", 10.5m, 30.25m, minInclusive: false, maxInclusive: false);
			TopDocs exclusiveHits = searcher.Search(exclusive, 10);
			Assert.AreEqual(1, exclusiveHits.TotalHits);
			Assert.AreEqual(20.0m, searcher.Doc(exclusiveHits.ScoreDocs[0].Doc).GetDecimalValue("amount"));

			Query openMin = DecimalField.NewRangeQuery("amount", null, 10.5m);
			TopDocs openMinHits = searcher.Search(openMin, 10);
			Assert.AreEqual(2, openMinHits.TotalHits);
			CollectionAssert.AreEquivalent(
				DecimalRangeOpenMinExpected,
				GetDecimalValues(searcher, openMinHits, "amount").ToArray());

			Query openMax = DecimalField.NewRangeQuery("amount", 30.25m, null);
			TopDocs openMaxHits = searcher.Search(openMax, 10);
			Assert.AreEqual(2, openMaxHits.TotalHits);
			CollectionAssert.AreEquivalent(
				DecimalRangeOpenMaxExpected,
				GetDecimalValues(searcher, openMaxHits, "amount").ToArray());

			Query exact = DecimalField.NewExactQuery("amount", 20.0m);
			TopDocs exactHits = searcher.Search(exact, 10);
			Assert.AreEqual(1, exactHits.TotalHits);
			Assert.AreEqual(20.0m, searcher.Doc(exactHits.ScoreDocs[0].Doc).GetDecimalValue("amount"));
			Assert.AreEqual(1, searcher.Search(DecimalField.NewExactQuery("amount", 20.00m), 10).TotalHits);

			Query exists = DecimalField.NewExistsQuery("amount");
			TopDocs existsHits = searcher.Search(exists, 10);
			Assert.AreEqual(5, existsHits.TotalHits);
		}

		[TestMethod]
		public void IPAddressFieldRangeQueryTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			foreach (string ip in new[] { "10.0.0.1", "192.168.0.1", "192.168.0.10", "192.168.0.100", "192.168.1.1" })
			{
				Document doc = [.. IPAddressField.CreateFields("ip", IPAddress.Parse(ip), Field.Store.YES)];
				writer.AddDocument(doc);
			}
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			Query inclusive = IPAddressField.NewRangeQuery(
				"ip",
				IPAddress.Parse("192.168.0.1"),
				IPAddress.Parse("192.168.0.100"));
			TopDocs inclusiveHits = searcher.Search(inclusive, 10);
			Assert.AreEqual(3, inclusiveHits.TotalHits);
			CollectionAssert.AreEquivalent(
				IPAddressRangeInclusiveExpected,
				GetIPAddressValues(searcher, inclusiveHits, "ip").ToArray());

			Query exclusive = IPAddressField.NewRangeQuery(
				"ip",
				IPAddress.Parse("192.168.0.1"),
				IPAddress.Parse("192.168.0.100"),
				minInclusive: false,
				maxInclusive: false);
			TopDocs exclusiveHits = searcher.Search(exclusive, 10);
			Assert.AreEqual(1, exclusiveHits.TotalHits);
			Assert.AreEqual("192.168.0.10", searcher.Doc(exclusiveHits.ScoreDocs[0].Doc).GetIPAddressValue("ip")!.ToString());

			Query openMin = IPAddressField.NewRangeQuery("ip", null, IPAddress.Parse("10.0.0.1"));
			TopDocs openMinHits = searcher.Search(openMin, 10);
			Assert.AreEqual(1, openMinHits.TotalHits);
			Assert.AreEqual("10.0.0.1", searcher.Doc(openMinHits.ScoreDocs[0].Doc).GetIPAddressValue("ip")!.ToString());

			Query openMax = IPAddressField.NewRangeQuery("ip", IPAddress.Parse("192.168.1.1"), null);
			TopDocs openMaxHits = searcher.Search(openMax, 10);
			Assert.AreEqual(1, openMaxHits.TotalHits);
			Assert.AreEqual("192.168.1.1", searcher.Doc(openMaxHits.ScoreDocs[0].Doc).GetIPAddressValue("ip")!.ToString());

			Query exact = IPAddressField.NewExactQuery("ip", IPAddress.Parse("192.168.0.10"));
			TopDocs exactHits = searcher.Search(exact, 10);
			Assert.AreEqual(1, exactHits.TotalHits);
			Assert.AreEqual("192.168.0.10", searcher.Doc(exactHits.ScoreDocs[0].Doc).GetIPAddressValue("ip")!.ToString());

			Query exists = IPAddressField.NewExistsQuery("ip");
			TopDocs existsHits = searcher.Search(exists, 10);
			Assert.AreEqual(5, existsHits.TotalHits);
		}

		[TestMethod]
		public void IPAddressFieldIPv6RangeQueryTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			foreach (string ip in new[] { "2001:db8::1", "2001:db8::10", "2001:db8::100", "2001:db9::1" })
			{
				Document doc = [.. IPAddressField.CreateFields("ip", IPAddress.Parse(ip), Field.Store.YES)];
				writer.AddDocument(doc);
			}
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			Query inclusive = IPAddressField.NewRangeQuery(
				"ip",
				IPAddress.Parse("2001:db8::1"),
				IPAddress.Parse("2001:db8::100"));
			TopDocs inclusiveHits = searcher.Search(inclusive, 10);
			Assert.AreEqual(3, inclusiveHits.TotalHits);
			CollectionAssert.AreEquivalent(
				IPAddressIPv6RangeInclusiveExpected,
				GetIPAddressValues(searcher, inclusiveHits, "ip").ToArray());

			Query exclusive = IPAddressField.NewRangeQuery(
				"ip",
				IPAddress.Parse("2001:db8::1"),
				IPAddress.Parse("2001:db8::100"),
				minInclusive: false,
				maxInclusive: false);
			TopDocs exclusiveHits = searcher.Search(exclusive, 10);
			Assert.AreEqual(1, exclusiveHits.TotalHits);
			Assert.AreEqual(IPAddress.Parse("2001:db8::10"), searcher.Doc(exclusiveHits.ScoreDocs[0].Doc).GetIPAddressValue("ip"));
		}

		[TestMethod]
		public void IPAddressFieldFamilyDisambiguationTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document loopbackV6 = [.. IPAddressField.CreateFields("ip", IPAddress.Parse("::1"), Field.Store.YES)];
			Document ipv4 = [.. IPAddressField.CreateFields("ip", IPAddress.Parse("0.0.0.1"), Field.Store.YES)];
			Document mapped = [.. IPAddressField.CreateFields("ip", IPAddress.Parse("::ffff:192.168.0.10"), Field.Store.YES)];
			Document ipv4Equivalent = [.. IPAddressField.CreateFields("ip", IPAddress.Parse("192.168.0.10"), Field.Store.YES)];
			writer.AddDocument(loopbackV6);
			writer.AddDocument(ipv4);
			writer.AddDocument(mapped);
			writer.AddDocument(ipv4Equivalent);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			Assert.AreEqual(1, searcher.Search(IPAddressField.NewExactQuery("ip", IPAddress.Parse("::1")), 10).TotalHits);
			Assert.AreEqual(1, searcher.Search(IPAddressField.NewExactQuery("ip", IPAddress.Parse("0.0.0.1")), 10).TotalHits);
			Assert.AreEqual(2, searcher.Search(IPAddressField.NewExactQuery("ip", IPAddress.Parse("192.168.0.10")), 10).TotalHits);
			Assert.AreEqual(2, searcher.Search(IPAddressField.NewExactQuery("ip", IPAddress.Parse("::ffff:192.168.0.10")), 10).TotalHits);

			Document storedLoopback = searcher.Doc(searcher.Search(IPAddressField.NewExactQuery("ip", IPAddress.Parse("::1")), 1).ScoreDocs[0].Doc);
			Assert.AreEqual(IPAddress.Parse("::1"), storedLoopback.GetIPAddressValue("ip"));
		}

		[TestMethod]
		public void IPAddressFieldMultiValueNoCrossProductTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			// Cross-product trap under limb AND: (high=5-ish, low=small) + (high=3-ish, low=large)
			Document doc =
			[
				.. IPAddressField.CreateFields("ip", IPAddress.Parse("192.168.0.5"), Field.Store.YES),
				.. IPAddressField.CreateFields("ip", IPAddress.Parse("10.0.0.100"), Field.Store.YES),
			];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			// Would false-positive if high of 192.168.0.5 were ANDed with low of 10.0.0.100.
			Query shouldMiss = IPAddressField.NewExactQuery("ip", IPAddress.Parse("192.168.0.100"));
			Assert.AreEqual(0, searcher.Search(shouldMiss, 10).TotalHits);

			Query shouldHit = IPAddressField.NewExactQuery("ip", IPAddress.Parse("192.168.0.5"));
			Assert.AreEqual(1, searcher.Search(shouldHit, 10).TotalHits);

			Document stored = searcher.Doc(searcher.Search(new MatchAllDocsQuery(), 1).ScoreDocs[0].Doc);
			CollectionAssert.AreEquivalent(
				new[] { "192.168.0.5", "10.0.0.100" },
				stored.GetIPAddressValues("ip").Select(static ip => ip.ToString()).ToArray());
		}

		[TestMethod]
		public void DecimalFieldMultiValueNoCrossProductTest()
		{
			using RAMDirectory directory = new RAMDirectory();
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc =
			[
				.. DecimalField.CreateFields("amount", 5.0m, Field.Store.YES),
				.. DecimalField.CreateFields("amount", 100.0m, Field.Store.YES),
			];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			using SearcherLease lease = new SearcherLease(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			Assert.AreEqual(0, searcher.Search(DecimalField.NewExactQuery("amount", 50.0m), 10).TotalHits);
			Assert.AreEqual(1, searcher.Search(DecimalField.NewExactQuery("amount", 5.0m), 10).TotalHits);
			Assert.AreEqual(1, searcher.Search(DecimalField.NewExactQuery("amount", 5.00m), 10).TotalHits);

			Document stored = searcher.Doc(searcher.Search(new MatchAllDocsQuery(), 1).ScoreDocs[0].Doc);
			CollectionAssert.AreEquivalent(
				new[] { 5.0m, 100.0m },
				stored.GetDecimalValues("amount").ToArray());
		}

		private static HashSet<decimal> GetDecimalValues(IndexSearcher searcher, TopDocs hits, string fieldName)
		{
			HashSet<decimal> values = [];
			foreach (ScoreDoc scoreDoc in hits.ScoreDocs)
			{
				decimal? value = searcher.Doc(scoreDoc.Doc).GetDecimalValue(fieldName);
				Assert.IsNotNull(value);
				values.Add(value.Value);
			}
			return values;
		}

		private static HashSet<string> GetIPAddressValues(IndexSearcher searcher, TopDocs hits, string fieldName)
		{
			HashSet<string> values = [];
			foreach (ScoreDoc scoreDoc in hits.ScoreDocs)
			{
				IPAddress? value = searcher.Doc(scoreDoc.Doc).GetIPAddressValue(fieldName);
				Assert.IsNotNull(value);
				values.Add(value.ToString());
			}
			return values;
		}
	}
}
