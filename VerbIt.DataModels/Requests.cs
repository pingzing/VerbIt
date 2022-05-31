using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VerbIt.DataModels;

public record LoginRequest([Required] string Username, [Required] string Password);

public record CreateAdminUserRequest([Required] string Username, [Required] string Password);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MasterListRowUpdateAction
{
    Delete,
    Edit
};

public record MasterListRowUpdateRequest([Range(0, int.MaxValue)] int Number, MasterListRowUpdateAction UpdateAction);

public record MasterListRowDeleteRequest([Range(0, int.MaxValue)] int Number)
    : MasterListRowUpdateRequest(Number, MasterListRowUpdateAction.Delete);

public record MasterListRowEditRequest([Range(0, int.MaxValue)] int Number, [Required] [MinLength(1)] string[][] Words)
    : MasterListRowUpdateRequest(Number, MasterListRowUpdateAction.Edit);

public record EditMasterListRequest(
    [Required] string ListName,
    [Required] [MinLength(1)] MasterListRowUpdateRequest[] ChangedRows
);
