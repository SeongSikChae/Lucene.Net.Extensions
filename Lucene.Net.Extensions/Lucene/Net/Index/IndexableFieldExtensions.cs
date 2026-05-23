using System.Net;

namespace Lucene.Net.Index
{
	using Documents;

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

		public static IPAddress? GetIPAddressValue(this IIndexableField field, Document document)
		{
			IIndexableField? lowField = document.GetField(field.Name + IPAddressField.LowPartSuffix);
			if (lowField is not null)
			{
				long? high = field.GetInt64Value();
				long? low = lowField.GetInt64Value();
				if (!high.HasValue || !low.HasValue)
					return null;

				return IPAddressExtensions.ToIPv6Address(high.Value, low.Value);
			}

			int? v4 = field.GetInt32Value();
			if (v4.HasValue)
				return v4.Value.ToIPAddress();

			return null;
		}
	}
}
