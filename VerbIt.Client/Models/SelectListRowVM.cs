namespace VerbIt.Client.Models
{
    public class SelectListRowVM
    {
        public Guid ListId { get; set; }
        public Guid RowId { get; set; }
        public int RowNum { get; set; }
        public string[][] Words { get; set; }
        public bool IsSelected { get; set; }
    }
}
