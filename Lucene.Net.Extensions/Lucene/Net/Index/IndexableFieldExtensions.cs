using System.Net;

namespace Lucene.Net.Index
{
	public static class IndexableFieldExtensions
	{
		public static sbyte? GetSByteValue(this IIndexableField field)
		{
			byte? v = field.GetByteValue();
			if (v is null)
				return null;
			return v.Value.ToSByte();
		}

		public static ushort? GetUInt16Value(this IIndexableField field)
		{
			short? v = field.GetInt16Value();
			if (!v.HasValue)
				return null;
			return v.Value.ToUInt16();
		}

		public static uint? GetUInt32Value(this IIndexableField field)
		{
			int? v = field.GetInt32Value();
			if (!v.HasValue)
				return null;
			return v.Value.ToUInt32();
		}

		public static ulong? GetUInt64Value(this IIndexableField field)
		{
			long? v = field.GetInt64Value();
			if (!v.HasValue)
				return null;
			return v.Value.ToUInt64();
		}

		public static Half? GetHalfValue(this IIndexableField field)
		{
			float? v = field.GetSingleValue();
			if (!v.HasValue)
				return null;
			return (Half)v.Value;
		}

		public static IPAddress? GetIPAddressValue(this IIndexableField field)
		{
			int? v = field.GetInt32Value();
			if (!v.HasValue)
				return null;
			return v.Value.ToIPAddress();
		}
	}
}
