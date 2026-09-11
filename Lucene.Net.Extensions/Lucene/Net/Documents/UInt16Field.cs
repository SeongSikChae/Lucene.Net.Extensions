namespace Lucene.Net.Documents
{
	/// <summary>
	/// Field that indexes a <see cref="ushort"/> value for efficient filtering and sorting.
	/// Values are stored via order-preserving conversion to <see cref="short"/>.
	/// </summary>
	public sealed class UInt16Field : Field
	{
		/// <summary>
		/// Type for a <see cref="UInt16Field"/> that is indexed and stored.
		/// </summary>
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

		/// <summary>
		/// Type for a <see cref="UInt16Field"/> that is indexed but not stored.
		/// </summary>
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

		/// <summary>
		/// Creates a new <see cref="UInt16Field"/>.
		/// </summary>
		/// <param name="name">Field name.</param>
		/// <param name="value">Field value.</param>
		/// <param name="stored">Whether to store the value.</param>
		public UInt16Field(string name, ushort value, Store stored) : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
		{
			FieldsData = J2N.Numerics.Int16.GetInstance(value.ToInt16());
		}
	}
}
