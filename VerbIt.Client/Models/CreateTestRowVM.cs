namespace VerbIt.Client.Models;

public class CreateTestRowVM
{
    public Guid RowId { get; set; }
    public int RowNum { get; set; }
    public string[][] Words { get; set; } = null!;
    public List<int> HiddenColumnIndices { get; set; } = null!;
    public string? Hint { get; set; } = null;

    public CreateTestRowVM(SelectListRowVM selectedRow)
    {
        RowId = selectedRow.RowId;
        RowNum = selectedRow.RowNum;
        Words = selectedRow.Words;
        HiddenColumnIndices = new List<int>(Words.Length);
        Hint = null;
    }
}
