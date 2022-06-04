using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Runtime.CompilerServices;
using VerbIt.Client.Models;

namespace VerbIt.Client.Services
{
    internal class CsvImporterService : ICsvImporterService
    {
        private readonly ILogger<CsvImporterService> _logger;

        public CsvImporterService(ILogger<CsvImporterService> logger)
        {
            _logger = logger;
        }

        public async IAsyncEnumerable<CreateListRowVM> ImportMasterList(
            Stream csvFileStream,
            [EnumeratorCancellation] CancellationToken token
        )
        {
            using var streamReader = new StreamReader(csvFileStream);
            using var csv = new CsvReader(
                streamReader,
                new CsvConfiguration(CultureInfo.CurrentUICulture) { HasHeaderRecord = false, DetectDelimiter = true, }
            );

            csv.Context.RegisterClassMap<CsvRowMap>();

            await foreach (var row in csv.GetRecordsAsync<CsvRow>(token))
            {
                yield return new CreateListRowVM
                {
                    Words = new List<List<string>>
                    {
                        new List<string> { row.Col1 },
                        new List<string> { row.Col2 },
                        new List<string> { row.Col3 },
                        new List<string> { row.Col4 },
                    }
                };
            }
        }
    }

    public class CsvRow
    {
        public string Col1 { get; set; } = null!;
        public string Col2 { get; set; } = null!;
        public string Col3 { get; set; } = null!;
        public string Col4 { get; set; } = null!;

        public CsvRow() { }
    }

    public sealed class CsvRowMap : ClassMap<CsvRow>
    {
        public CsvRowMap()
        {
            Map(m => m.Col1).Index(0);
            Map(m => m.Col2).Index(1);
            Map(m => m.Col3).Index(2);
            Map(m => m.Col4).Index(3);
        }
    }

    internal interface ICsvImporterService
    {
        /// <summary>
        /// Reads the given CSV file stream and returns back a list of rows. Makes the assumption
        /// that each CSV row is made up of exactly four (4) string columns.
        /// </summary>
        /// <exception cref="CsvHelperException"></exception>
        IAsyncEnumerable<CreateListRowVM> ImportMasterList(Stream csvFileStream, CancellationToken token);
    }
}
