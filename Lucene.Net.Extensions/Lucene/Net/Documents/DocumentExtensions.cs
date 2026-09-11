using System.Net;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
{
	using Index;

	/// <summary>
	/// Extension methods for reading typed values from a <see cref="Document"/>.
	/// </summary>
	public static class DocumentExtensions
	{
		/// <summary>
		/// Reads the first <see cref="IPAddress"/> previously indexed with <see cref="IPAddressField"/>.
		/// </summary>
		public static IPAddress? GetIPAddressValue(this Document document, string name)
		{
			ArgumentNullException.ThrowIfNull(document);
			foreach (IPAddress value in document.GetIPAddressValues(name))
				return value;
			return null;
		}

		/// <summary>
		/// Reads all <see cref="IPAddress"/> values previously indexed with <see cref="IPAddressField"/>.
		/// </summary>
		public static IEnumerable<IPAddress> GetIPAddressValues(this Document document, string name)
		{
			ArgumentNullException.ThrowIfNull(document);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			foreach (BytesRef binary in document.GetBinaryValues(name))
			{
				if (binary is null)
					continue;
				if (!IPAddressExtensions.TryToIPAddress(binary.Bytes.AsSpan(binary.Offset, binary.Length), out IPAddress? address))
					continue;

				yield return address!;
			}
		}

		/// <summary>
		/// Reads the first <see cref="decimal"/> previously indexed with <see cref="DecimalField"/>.
		/// </summary>
		public static decimal? GetDecimalValue(this Document document, string name)
		{
			ArgumentNullException.ThrowIfNull(document);
			foreach (decimal value in document.GetDecimalValues(name))
				return value;
			return null;
		}

		/// <summary>
		/// Reads all <see cref="decimal"/> values previously indexed with <see cref="DecimalField"/>.
		/// </summary>
		public static IEnumerable<decimal> GetDecimalValues(this Document document, string name)
		{
			ArgumentNullException.ThrowIfNull(document);
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			foreach (BytesRef binary in document.GetBinaryValues(name))
			{
				if (binary is null || binary.Length != DecimalField.StoredBitsLength)
					continue;

				yield return DecimalField.FromStoredBits(binary.Bytes.AsSpan(binary.Offset, binary.Length));
			}
		}
	}
}
