using System.Net;

namespace Lucene.Net.Index
{
	using Documents;

	/// <summary>
	/// Extension methods for reading typed numeric values from an <see cref="IIndexableField"/>.
	/// </summary>
	public static class IndexableFieldExtensions
	{
		/// <summary>
		/// Reads an <see cref="sbyte"/> stored via order-preserving conversion from <see cref="byte"/>.
		/// </summary>
		public static sbyte? GetSByteValue(this IIndexableField field)
		{
			byte? v = field.GetByteValue();
			if (v is null)
				return null;
			return v.Value.ToSByte();
		}

		/// <summary>
		/// Reads a <see cref="ushort"/> stored via order-preserving conversion from <see cref="short"/>.
		/// </summary>
		public static ushort? GetUInt16Value(this IIndexableField field)
		{
			short? v = field.GetInt16Value();
			if (!v.HasValue)
				return null;
			return v.Value.ToUInt16();
		}

		/// <summary>
		/// Reads a <see cref="uint"/> stored via order-preserving conversion from <see cref="int"/>.
		/// </summary>
		public static uint? GetUInt32Value(this IIndexableField field)
		{
			int? v = field.GetInt32Value();
			if (!v.HasValue)
				return null;
			return v.Value.ToUInt32();
		}

		/// <summary>
		/// Reads a <see cref="ulong"/> stored via order-preserving conversion from <see cref="long"/>.
		/// </summary>
		public static ulong? GetUInt64Value(this IIndexableField field)
		{
			long? v = field.GetInt64Value();
			if (!v.HasValue)
				return null;
			return v.Value.ToUInt64();
		}

		/// <summary>
		/// Reads a <see cref="Half"/> stored as <see cref="float"/>.
		/// </summary>
		public static Half? GetHalfValue(this IIndexableField field)
		{
			float? v = field.GetSingleValue();
			if (!v.HasValue)
				return null;
			return (Half)v.Value;
		}

		/// <summary>
		/// Reads an <see cref="IPAddress"/> from the high limb field plus the matching low limb on <paramref name="document"/>.
		/// </summary>
		public static IPAddress? GetIPAddressValue(this IIndexableField field, Document document)
		{
            long? high = field.GetInt64Value();
			if (!high.HasValue)
				return null;

            IIndexableField? lowField = document.GetField(field.Name + IPAddressField.LowPartSuffix);
			if (lowField is null)
				return null;
            long? low = lowField.GetInt64Value();
			if (!low.HasValue)
				return null;

            return IPAddressExtensions.ToIPAddress(high.Value, low.Value);
        }

		/// <summary>
		/// Reads a <see cref="decimal"/> from the flags field plus the matching GetBits parts on <paramref name="document"/>.
		/// </summary>
		public static decimal? GetDecimalValue(this IIndexableField field, Document document)
		{
			IIndexableField? lowField = document.GetField(field.Name + DecimalField.LowPartSuffix);
			if (lowField is null)
				return null;
            int? low = lowField.GetInt32Value();
			if (!low.HasValue)
				return null;

            IIndexableField? midField = document.GetField(field.Name + DecimalField.MidPartSuffix);
            if (midField is null)
                return null;
            int? mid = midField.GetInt32Value();
            if (!mid.HasValue)
                return null;

            IIndexableField? highField = document.GetField(field.Name + DecimalField.HighPartSuffix);
            if (highField is null)
                return null;
            int? high = highField.GetInt32Value();
            if (!high.HasValue)
                return null;

            int? flags = field.GetInt32Value();
			if (!flags.HasValue)
				return null;

			return new decimal([low.Value, mid.Value, high.Value, flags.Value]);
        }
	}
}
