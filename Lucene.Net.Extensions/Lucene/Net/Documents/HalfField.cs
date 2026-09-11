namespace Lucene.Net.Documents
{
	/// <summary>
	/// Field that indexes a <see cref="Half"/> value for efficient filtering and sorting.
	/// Values are stored as <see cref="float"/>.
	/// </summary>
	public sealed class HalfField : Field
	{
		/// <summary>
		/// Type for a <see cref="HalfField"/> that is indexed and stored.
		/// </summary>
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

		/// <summary>
		/// Type for a <see cref="HalfField"/> that is indexed but not stored.
		/// </summary>
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

		/// <summary>
		/// Creates a new <see cref="HalfField"/>.
		/// </summary>
		/// <param name="name">Field name.</param>
		/// <param name="value">Field value.</param>
		/// <param name="stored">Whether to store the value.</param>
		public HalfField(string name, Half value, Store stored) : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
		{
			FieldsData = J2N.Numerics.Single.GetInstance((float)value);
		}
	}
}
