using System.Net;

namespace Lucene.Net.Documents
{
    using Index;

    /// <summary>
    /// Extension methods for reading typed values from a <see cref="Document"/>.
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Reads an <see cref="IPAddress"/> previously indexed with <see cref="IPAddressField"/>.
        /// </summary>
        /// <param name="document">Document to read from.</param>
        /// <param name="name">Base field name.</param>
        /// <returns>The address, or <c>null</c> if the field is missing.</returns>
        public static IPAddress? GetIPAddressValue(this Document document, string name)
        {
            IIndexableField? field = document.GetField(name);
            if (field is null)
                return null;

            return field.GetIPAddressValue(document);
        }

        /// <summary>
        /// Reads a <see cref="decimal"/> previously indexed with <see cref="DecimalField"/>.
        /// </summary>
        /// <param name="document">Document to read from.</param>
        /// <param name="name">Base field name.</param>
        /// <returns>The value, or <c>null</c> if the field is missing.</returns>
        public static decimal? GetDecimalValue(this Document document, string name)
        {
            IIndexableField? field = document.GetField(name);
            if (field is null)
                return null;
            return field.GetDecimalValue(document);
        }
    }
}
