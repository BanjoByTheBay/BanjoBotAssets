using BanjoBotAssets.Artifacts.Models;

namespace BanjoBotAssets.Artifacts.Helpers
{
    /// <summary>
    /// Allows a subclass of <see cref="NamedItemData"/> to be deserialized polymorphically
    /// based on the value of the <see cref="NamedItemData.Type"/> field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal sealed class NamedItemDataAttribute : Attribute
    {
        public NamedItemDataAttribute(string typeFieldDiscriminator)
        {
            TypeFieldDiscriminator = typeFieldDiscriminator;
        }

        /// <summary>
        /// Gets a value that, if found in <see cref="NamedItemData.Type"/> when deserializing,
        /// will signal that the object should be deserialized as an instance of the class marked
        /// with this attribute.
        /// </summary>
        public string TypeFieldDiscriminator { get; }
    }
}
