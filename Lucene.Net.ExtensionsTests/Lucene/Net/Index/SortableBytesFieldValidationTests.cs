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
	public class SortableBytesFieldValidationTests
	{
		[TestMethod]
		public void CreateFieldsRejectsEmptyStoredBytes()
		{
			byte[] indexed = new byte[17];
			Assert.ThrowsException<ArgumentException>(() =>
			{
				_ = SortableBytesField.CreateFields("ip", indexed, storedBytes: [], Field.Store.YES).ToArray();
			});
		}

		[TestMethod]
		public void ReservedSuffixesMatchDocumentedNames()
		{
			Assert.AreEqual("_$Sort", SortableBytesField.SortFieldSuffix);
			Assert.AreEqual("_$Exists", SortableBytesField.ExistsFieldSuffix);
			Assert.AreEqual(SortableBytesField.SortFieldSuffix, IPAddressField.SortFieldSuffix);
			Assert.AreEqual(SortableBytesField.ExistsFieldSuffix, IPAddressField.ExistsFieldSuffix);
			Assert.AreEqual(SortableBytesField.SortFieldSuffix, DecimalField.SortFieldSuffix);
			Assert.AreEqual(SortableBytesField.ExistsFieldSuffix, DecimalField.ExistsFieldSuffix);
		}

		[TestMethod]
		public void NewRangeQueryRejectsEmptyBoundArrays()
		{
			Assert.ThrowsException<ArgumentException>(() =>
			{
				_ = SortableBytesField.NewRangeQuery("x", [], null);
			});
			Assert.ThrowsException<ArgumentException>(() =>
			{
				_ = SortableBytesField.NewRangeQuery("x", null, []);
			});
		}

		[TestMethod]
		public void ExistsQueryUsesMarkerField()
		{
			using RAMDirectory directory = new();
			using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE);
			using (IndexWriter writer = new(directory, config))
			{
				Document doc = [.. IPAddressField.CreateFields("ip", IPAddress.Loopback, Field.Store.YES)];
				writer.AddDocument(doc);
				writer.AddDocument([]);
				writer.Commit();
				writer.ForceMerge(1);
			}

			using SearcherManager searcherManager = new(directory, null);
			using SearcherLease lease = new(searcherManager);
			TopDocs hits = lease.Searcher.Search(IPAddressField.NewExistsQuery("ip"), 10);
			Assert.AreEqual(1, hits.TotalHits);
		}
	}
}
