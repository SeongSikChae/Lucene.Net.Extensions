using System.Net;
using System.Net.Sockets;

namespace Lucene.Net.Documents
{
	public class IPAddressField : Field
	{
		public const string LowPartSuffix = IPAddressExtensions.LowPartSuffix;

		public static readonly FieldType TYPE_STORED_V4 = new FieldType
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

		public static readonly FieldType TYPE_NOT_STORED_V4 = new FieldType
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

		public static readonly FieldType TYPE_STORED_V6 = new FieldType
		{
			DocValueType = Net.Index.DocValuesType.NONE,
			IndexOptions = Net.Index.IndexOptions.DOCS_ONLY,
			IsIndexed = true,
			IsStored = true,
			IsTokenized = true,
			NumericPrecisionStep = 4,
			NumericType = Documents.NumericType.INT64,
			OmitNorms = true,
			StoreTermVectorOffsets = false,
			StoreTermVectorPayloads = false,
			StoreTermVectorPositions = false,
			StoreTermVectors = false
		}.Freeze();

		public static readonly FieldType TYPE_NOT_STORED_V6 = new FieldType
		{
			DocValueType = Net.Index.DocValuesType.NONE,
			IndexOptions = Net.Index.IndexOptions.DOCS_ONLY,
			IsIndexed = true,
			IsStored = false,
			IsTokenized = true,
			NumericPrecisionStep = 4,
			NumericType = Documents.NumericType.INT64,
			OmitNorms = true,
			StoreTermVectorOffsets = false,
			StoreTermVectorPayloads = false,
			StoreTermVectorPositions = false,
			StoreTermVectors = false
		}.Freeze();

		public Field? LowPart { get; }

		public IPAddressField(string name, IPAddress value, Store stored) : base(name, GetPrimaryFieldType(value, stored))
		{
			IPAddress normalized = value.NormalizeForStorage();

			if (normalized.AddressFamily == AddressFamily.InterNetwork)
			{
				FieldsData = J2N.Numerics.Int32.GetInstance(normalized.ToInt32());
				LowPart = null;
				return;
			}

			if (normalized.AddressFamily != AddressFamily.InterNetworkV6)
				throw new ArgumentException("Only IPv4 and IPv6 addresses are supported.", nameof(value));

			normalized.ToInt64Pair(out long high, out long low);
			FieldsData = J2N.Numerics.Int64.GetInstance(high);
			LowPart = new IPAddressLowPartField(name + LowPartSuffix, low, stored);
		}

		public static IEnumerable<Field> CreateFields(string name, IPAddress value, Store stored)
		{
			IPAddressField field = new IPAddressField(name, value, stored);
			yield return field;
			if (field.LowPart is not null)
				yield return field.LowPart;
		}

		private static FieldType GetPrimaryFieldType(IPAddress value, Store stored)
		{
			IPAddress normalized = value.NormalizeForStorage();
			bool isStored = stored == Store.YES;

			return normalized.AddressFamily switch
			{
				AddressFamily.InterNetwork => isStored ? TYPE_STORED_V4 : TYPE_NOT_STORED_V4,
				AddressFamily.InterNetworkV6 => isStored ? TYPE_STORED_V6 : TYPE_NOT_STORED_V6,
				_ => throw new ArgumentException("Only IPv4 and IPv6 addresses are supported.", nameof(value))
			};
		}

		private sealed class IPAddressLowPartField : Field
		{
			public IPAddressLowPartField(string name, long low, Store stored) : base(name, stored == Store.YES ? TYPE_STORED_V6 : TYPE_NOT_STORED_V6)
			{
				FieldsData = J2N.Numerics.Int64.GetInstance(low);
			}
		}
	}
}
