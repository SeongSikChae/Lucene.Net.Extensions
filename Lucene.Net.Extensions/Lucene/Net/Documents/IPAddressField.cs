using Lucene.Net.Search;
using System.Net;

namespace Lucene.Net.Documents
{
	public static class IPAddressField
	{
		public const string LowPartSuffix = IPAddressExtensions.LowPartSuffix;


		public static IEnumerable<Field> CreateFields(string name, IPAddress value, Field.Store stored)
		{
			value.NormalizeForStorage().ToInt64Pair(out long high, out long low);
			yield return new Int64Field(name, high, stored);
            yield return new Int64Field(name + LowPartSuffix, low, stored);
		}

		public static IEnumerable<SortField> CreateSortField(string name, bool reverse = false)
		{
			yield return new SortField(name, SortFieldType.INT64, reverse);
			yield return new SortField(name + LowPartSuffix, SortFieldType.INT64, reverse);
		}
	}
}
