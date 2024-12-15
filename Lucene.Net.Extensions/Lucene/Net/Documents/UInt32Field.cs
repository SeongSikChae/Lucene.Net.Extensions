using System.Buffers.Binary;

namespace Lucene.Net.Documents
{
	public sealed class UInt32Field : Field
	{
		public static readonly FieldType TYPE_STORED = new FieldType
		{
			DocValueType = Net.Index.DocValuesType.NONE,
			IndexOptions = Net.Index.IndexOptions.DOCS_ONLY,
			IsIndexed = true,
			IsStored = true,
			IsTokenized = true,
			NumericPrecisionStep = 4,
			NumericType = Documents.NumericType.INT32,
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
			NumericType = Documents.NumericType.INT32,
			OmitNorms = true,
			StoreTermVectorOffsets = false,
			StoreTermVectorPayloads = false,
			StoreTermVectorPositions = false,
			StoreTermVectors = false
		}.Freeze();

		public UInt32Field(string name, uint value, Store stored) : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
		{
			FieldsData = J2N.Numerics.Int32.GetInstance(value.ToInt32());
		}
	}
}
