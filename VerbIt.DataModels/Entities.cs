using System.ComponentModel.DataAnnotations;

namespace VerbIt.DataModels;

public record MasterListRow([Required] string Name, [Required] int Number, [Required] [MinLength(1)] string[][] Words);
