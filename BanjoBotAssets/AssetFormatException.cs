using System.Runtime.Serialization;

namespace BanjoBotAssets
{
    internal sealed class AssetFormatException : ApplicationException
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
    }
}
