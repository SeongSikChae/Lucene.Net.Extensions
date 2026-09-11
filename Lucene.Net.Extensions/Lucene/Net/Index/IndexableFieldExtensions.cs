using System.Net;
using Lucene.Net.Util;

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
		/// Reads an <see cref="IPAddress"/> from a stored encoded payload produced by <see cref="IPAddressField"/>.
		/// </summary>
		public static IPAddress? GetIPAddressValue(this IIndexableField field)
		{
			ArgumentNullException.ThrowIfNull(field);
			BytesRef? binary = field.GetBinaryValue();
			if (binary is null)
				return null;
			return IPAddressExtensions.TryToIPAddress(binary.Bytes.AsSpan(binary.Offset, binary.Length), out IPAddress? address)
				? address
				: null;
		}

		/// <summary>
		/// Reads an <see cref="IPAddress"/> from the stored field on <paramref name="document"/> named like <paramref name="field"/>.
		/// </summary>
		public static IPAddress? GetIPAddressValue(this IIndexableField field, Document document)
		{
			ArgumentNullException.ThrowIfNull(field);
			ArgumentNullException.ThrowIfNull(document);
			return document.GetIPAddressValue(field.Name);
		}

		/// <summary>
		/// Reads a <see cref="decimal"/> from a stored GetBits payload produced by <see cref="DecimalField"/>.
		/// </summary>
		public static decimal? GetDecimalValue(this IIndexableField field)
		{
			ArgumentNullException.ThrowIfNull(field);
			BytesRef? binary = field.GetBinaryValue();
			if (binary is null || binary.Length != DecimalField.StoredBitsLength)
				return null;

			return DecimalField.FromStoredBits(binary.Bytes.AsSpan(binary.Offset, binary.Length));
		}

		/// <summary>
		/// Reads a <see cref="decimal"/> from the stored field on <paramref name="document"/> named like <paramref name="field"/>.
		/// </summary>
		public static decimal? GetDecimalValue(this IIndexableField field, Document document)
		{
			ArgumentNullException.ThrowIfNull(field);
			ArgumentNullException.ThrowIfNull(document);
			return document.GetDecimalValue(field.Name);
		}
	}
}
