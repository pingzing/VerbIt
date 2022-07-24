using System.ComponentModel;
using VerbIt.Client.Converters;

namespace VerbIt.Client.Models;

public class EditListRowVM
{
    public bool Editable { get; set; } = false;

    [TypeConverter(typeof(StringListToStringConverter))]
    public List<List<string>> Words { get; set; } = null!;
    public Guid RowId { get; set; }
    public int RowNum { get; set; }
}
