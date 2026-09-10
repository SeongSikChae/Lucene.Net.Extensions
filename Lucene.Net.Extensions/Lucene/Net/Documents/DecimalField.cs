using Lucene.Net.Search;

namespace Lucene.Net.Documents
{
    public static class DecimalField
    {
        public const string LowPartSuffix = "_DL";
        public const string MidPartSuffix = "_DM";
        public const string HighPartSuffix = "_DH";

        public static IEnumerable<Field> CreateFields(string name, decimal value, Field.Store stored)
        {
            int[] parts = decimal.GetBits(value);
            yield return new Int32Field(name + LowPartSuffix, parts[0], stored);
            yield return new Int32Field(name + MidPartSuffix, parts[1], stored);
            yield return new Int32Field(name + HighPartSuffix, parts[2], stored);
            yield return new Int32Field(name, parts[3], stored);
        }

        public static IEnumerable<SortField> CreateSortField(string name, bool reverse = false)
        {
            yield return new SortField(name + LowPartSuffix, SortFieldType.INT32, reverse);
            yield return new SortField(name + MidPartSuffix, SortFieldType.INT32, reverse);
            yield return new SortField(name + HighPartSuffix, SortFieldType.INT32, reverse);
            yield return new SortField(name, SortFieldType.INT32, reverse);
        }
    }
}
