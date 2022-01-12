using System.Runtime.Serialization;

namespace BanjoBotAssets
{
    internal class AssetFormatException : ApplicationException
    {
        public AssetFormatException()
        {
        }

        public AssetFormatException(string? message) : base(message)
        {
        }

        public AssetFormatException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected AssetFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
