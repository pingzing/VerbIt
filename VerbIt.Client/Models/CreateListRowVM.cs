namespace VerbIt.Client.Models
{
    internal class CreateListRowVM
    {
        public List<List<string>> Words { get; set; } = null!;

        internal static CreateListRowVM GetDefaultRow() =>
            new()
            {
                Words = new List<List<string>>
                {
                    new List<string> { "Verb Form 1" },
                    new List<string> { "Verb Form 2" },
                    new List<string> { "Verb Form 3" },
                    new List<string> { "Verb Form 4" }
                }
            };
    }
}
