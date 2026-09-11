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
	public class IPAddressFieldBoundaryTests
	{
		private static readonly IPAddress[] BoundaryAddresses =
		[
			IPAddress.Parse("0.0.0.0"),
			IPAddress.Parse("255.255.255.255"),
			IPAddress.Parse("::"),
			IPAddress.Parse("::1"),
			IPAddress.Parse("::ffff"),
			IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"),
		];

		[TestMethod]
		public void BoundaryValuesAndFullSpaceRange()
		{
			using RAMDirectory directory = new();
			using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE);
			using (IndexWriter writer = new(directory, config))
			{
				for (int i = 0; i < BoundaryAddresses.Length; i++)
				{
					Document doc =
					[
						.. IPAddressField.CreateFields("ip", BoundaryAddresses[i], Field.Store.YES),
						.. IPAddressField.CreateSortValueFields("ip", BoundaryAddresses[i]),
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

			Query full = IPAddressField.NewRangeQuery(
				"ip",
				IPAddress.Parse("0.0.0.0"),
				IPAddress.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff"));
			TopDocs fullHits = searcher.Search(full, BoundaryAddresses.Length + 1);
			Assert.AreEqual(BoundaryAddresses.Length, fullHits.TotalHits);

			foreach (IPAddress address in BoundaryAddresses)
				Assert.AreEqual(1, searcher.Search(IPAddressField.NewExactQuery("ip", address), 10).TotalHits);

			// Family byte orders all IPv4 before all IPv6.
			Sort sort = new([.. IPAddressField.CreateSortField("ip")]);
			TopFieldCollector collector = TopFieldCollector.Create(sort, BoundaryAddresses.Length, true, true, true, true);
			searcher.Search(new MatchAllDocsQuery(), collector);
			TopDocs ordered = collector.GetTopDocs();
			string[] orderedIds = ordered.ScoreDocs
				.Select(sd => searcher.Doc(sd.Doc).Get("id"))
				.ToArray();
			CollectionAssert.AreEqual(new[] { "0", "1", "2", "3", "4", "5" }, orderedIds);

			for (int i = 0; i < BoundaryAddresses.Length; i++)
			{
				TopDocs hits = searcher.Search(new TermQuery(new Term("id", i.ToString())), 1);
				IPAddress? stored = searcher.Doc(hits.ScoreDocs[0].Doc).GetIPAddressValue("ip");
				Assert.IsNotNull(stored);
				Assert.AreEqual(BoundaryAddresses[i], stored);
			}
		}

		[TestMethod]
		public void ScopeIdIsNotPreservedOnRoundTrip()
		{
			IPAddress scoped = IPAddress.Parse("fe80::1%12");
			Assert.AreNotEqual(0, scoped.ScopeId);

			using RAMDirectory directory = new();
			using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE);
			using (IndexWriter writer = new(directory, config))
			{
				writer.AddDocument([.. IPAddressField.CreateFields("ip", scoped, Field.Store.YES)]);
				writer.Commit();
				writer.ForceMerge(1);
			}

			using SearcherManager searcherManager = new(directory, null);
			using SearcherLease lease = new(searcherManager);
			IPAddress? stored = lease.Searcher.Doc(0).GetIPAddressValue("ip");
			Assert.IsNotNull(stored);
			Assert.AreEqual(IPAddress.Parse("fe80::1"), stored);
			Assert.AreEqual(0, stored!.ScopeId);
		}

		[TestMethod]
		public void StoredPayloadUsesCompactVariableWidth()
		{
			IPAddress v4 = IPAddress.Parse("192.168.0.1");
			IPAddress v6 = IPAddress.Parse("2001:db8::1");

			CollectionAssert.AreEqual(
				new byte[] { IPAddressExtensions.FamilyIPv4, 192, 168, 0, 1 },
				v4.ToStoredBytes());
			Assert.AreEqual(IPAddressExtensions.StoredLengthIPv6, v6.ToStoredBytes().Length);
			Assert.AreEqual(v6, IPAddressExtensions.ToIPAddress(v6.ToStoredBytes()));
			Assert.AreEqual(v4, IPAddressExtensions.ToIPAddress(v4.ToSortableBytes()));

			using RAMDirectory directory = new();
			using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
			IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
				.SetOpenMode(OpenMode.CREATE);
			using (IndexWriter writer = new(directory, config))
			{
				writer.AddDocument(
				[
					.. IPAddressField.CreateFields("ip", v4, Field.Store.YES),
					new StringField("id", "v4", Field.Store.YES),
				]);
				writer.AddDocument(
				[
					.. IPAddressField.CreateFields("ip", v6, Field.Store.YES),
					new StringField("id", "v6", Field.Store.YES),
				]);
				writer.Commit();
				writer.ForceMerge(1);
			}

			using SearcherManager searcherManager = new(directory, null);
			using SearcherLease lease = new(searcherManager);
			IndexSearcher searcher = lease.Searcher;

			Document doc4 = searcher.Doc(searcher.Search(new TermQuery(new Term("id", "v4")), 1).ScoreDocs[0].Doc);
			Document doc6 = searcher.Doc(searcher.Search(new TermQuery(new Term("id", "v6")), 1).ScoreDocs[0].Doc);
			BytesRef stored4 = doc4.GetBinaryValue("ip")!;
			BytesRef stored6 = doc6.GetBinaryValue("ip")!;
			Assert.AreEqual(IPAddressExtensions.StoredLengthIPv4, stored4.Length);
			Assert.AreEqual(IPAddressExtensions.StoredLengthIPv6, stored6.Length);
			Assert.AreEqual(v4, doc4.GetIPAddressValue("ip"));
			Assert.AreEqual(v6, doc6.GetIPAddressValue("ip"));

			Assert.AreEqual(1, searcher.Search(IPAddressField.NewExactQuery("ip", v4), 10).TotalHits);
			Assert.AreEqual(1, searcher.Search(IPAddressField.NewExactQuery("ip", v6), 10).TotalHits);
		}
	}
}
