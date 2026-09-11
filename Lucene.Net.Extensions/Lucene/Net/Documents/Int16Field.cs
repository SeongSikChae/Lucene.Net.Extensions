namespace Lucene.Net.Documents
{
	/// <summary>
	/// Field that indexes an <see cref="short"/> value for efficient filtering and sorting.
	/// </summary>
	public sealed class Int16Field : Field
	{
		/// <summary>
		/// Type for an <see cref="Int16Field"/> that is indexed and stored.
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
		/// Type for an <see cref="Int16Field"/> that is indexed but not stored.
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
		/// Creates a new <see cref="Int16Field"/>.
		/// </summary>
		/// <param name="name">Field name.</param>
		/// <param name="value">Field value.</param>
		/// <param name="stored">Whether to store the value.</param>
		public Int16Field(string name, short value, Store stored) : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
		{
			FieldsData = J2N.Numerics.Int16.GetInstance(value);
		}
	}
}
