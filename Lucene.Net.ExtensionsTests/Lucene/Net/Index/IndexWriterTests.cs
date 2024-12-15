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
		[TestMethod]
		public void SByteFieldTest()
		{
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
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
			IndexSearcher searcher = searcherManager.Acquire();

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
		public void IPAddress4FieldTest()
		{
			DirectoryInfo dir = new DirectoryInfo("test");
			if (dir.Exists)
				dir.Delete(true);
			using FSDirectory directory = FSDirectory.Open(dir);
			Analyzer analyzer = new StandardAnalyzer(Util.LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(Util.LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE_OR_APPEND).SetRAMBufferSizeMB(1)
				.SetMergePolicy(new TieredMergePolicy());
			using IndexWriter writer = new IndexWriter(directory, config);

			writer.Commit();
			Document doc = [
					new IPAddress4Field("A", IPAddress.Parse("0.0.0.0"), Field.Store.YES), new IPAddress4Field("B", IPAddress.Parse("255.255.255.255"), Field.Store.YES),
				];
			writer.AddDocument(doc);
			writer.Commit();
			writer.ForceMerge(1);

			using SearcherManager searcherManager = new SearcherManager(directory, null);
			IndexSearcher searcher = searcherManager.Acquire();

			SortField field = new SortField("A", SortFieldType.INT32);
			Sort sort = new Sort(field);
			TopFieldCollector collector = TopFieldCollector.Create(sort, 10, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs topDocs = collector.GetTopDocs();
			IIndexableField aField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("A");
			IPAddress? aValue = aField.GetIPAddressValue();

			Assert.IsNotNull(aValue);
			Assert.AreEqual(IPAddress.Parse("0.0.0.0"), aValue);

			IIndexableField bField = searcher.Doc(topDocs.ScoreDocs[0].Doc).GetField("B");
			IPAddress? bValue = bField.GetIPAddressValue();

			Assert.IsNotNull(bValue);
			Assert.AreEqual(IPAddress.Parse("255.255.255.255"), bValue);
		}
	}
}
