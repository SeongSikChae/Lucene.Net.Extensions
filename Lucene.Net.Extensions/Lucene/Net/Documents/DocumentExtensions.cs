using System.Net;

namespace Lucene.Net.Documents
{
    using Index;

    public static class DocumentExtensions
    {
        public static IPAddress? GetIPAddressValue(this Document document, string name)
        {
            IIndexableField? field = document.GetField(name);
            if (field is null)
                return null;

            return field.GetIPAddressValue(document);
        }
    }
}
