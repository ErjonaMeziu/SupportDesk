using System.ComponentModel.DataAnnotations;

namespace SupportDesk.Api.Dtos.Comments;

public record CommentDto(int Id, string AuthorName, string Body, DateTime CreatedDate);

public record CreateCommentRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string AuthorName { get; init; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Body { get; init; } = string.Empty;
}
