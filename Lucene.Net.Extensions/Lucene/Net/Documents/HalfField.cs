using System.Buffers.Binary;

namespace Lucene.Net.Documents
{
	public sealed class HalfField : Field
	{
		public static readonly FieldType TYPE_STORED = new FieldType
		{
			DocValueType = Net.Index.DocValuesType.NONE,
			IndexOptions = Net.Index.IndexOptions.DOCS_ONLY,
			IsIndexed = true,
			IsStored = true,
			IsTokenized = true,
			NumericPrecisionStep = 4,
			NumericType = Documents.NumericType.SINGLE,
			OmitNorms = true,
			StoreTermVectorOffsets = false,
			StoreTermVectorPayloads = false,
			StoreTermVectorPositions = false,
			StoreTermVectors = false
		}.Freeze();

		public static readonly FieldType TYPE_NOT_STORED = new FieldType
		{
			DocValueType = Net.Index.DocValuesType.NONE,
			IndexOptions = Net.Index.IndexOptions.DOCS_ONLY,
			IsIndexed = true,
			IsStored = false,
			IsTokenized = true,
			NumericPrecisionStep = 4,
			NumericType = Documents.NumericType.SINGLE,
			OmitNorms = true,
			StoreTermVectorOffsets = false,
			StoreTermVectorPayloads = false,
			StoreTermVectorPositions = false,
			StoreTermVectors = false
		}.Freeze();

		public HalfField(string name, Half value, Store stored) : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
		{
			FieldsData = J2N.Numerics.Single.GetInstance((float)value);
		}
	}
}
