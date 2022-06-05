using System.ComponentModel;
using VerbIt.Client.Converters;

namespace VerbIt.Client.Models
{
    internal class CreateListRowVM
    {
        public bool Editable { get; set; } = false;

        [TypeConverter(typeof(StringCollectionToStringConverter))]
        public List<List<string>> Words { get; set; } = null!;

        internal static CreateListRowVM GetDefaultRow() =>
            new()
            {
                Words = new List<List<string>>
                {
                    new List<string> { "Valid answer 1", "Valid answer 2" },
                    new List<string> { "Verb Form 2" },
                    new List<string> { "Verb Form 3" },
                    new List<string> { "Verb Form 4" }
                }
            };
    }
}
