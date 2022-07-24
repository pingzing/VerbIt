using VerbIt.DataModels;

namespace VerbIt.Client.Models
{
    public class TestOverviewEntryVM
    {
        public Guid TestId { get; init; }
        public string TestName { get; init; }
        public int TotalRows { get; init; }
        public DateTimeOffset TestCreationTimestamp { get; init; }
        public Guid SourceList { get; init; }
        public string SourceListName { get; set; }

        public bool IsAvailable { get; set; }
        public bool IsRetakeable { get; set; }

        public TestOverviewEntryVM(TestOverviewEntry backingModel)
        {
            TestId = backingModel.TestId;
            TestName = backingModel.TestName;
            TotalRows = backingModel.TotalRows;
            TestCreationTimestamp = backingModel.TestCreationTimestamp;
            SourceList = backingModel.SourceList;
            SourceListName = backingModel.SourceListName;

            IsAvailable = backingModel.IsAvailable;
            IsRetakeable = backingModel.IsRetakeable;
        }
    }
}
