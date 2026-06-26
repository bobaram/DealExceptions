namespace DealExceptions.Application.DTOs;

public record CommentDto(
    int Id,
    string AuthorName,
    string Text,
    DateTime CreatedAt
);

public record StatusHistoryDto(
    int Id,
    string FromStatus,
    string ToStatus,
    string ChangedBy,
    DateTime ChangedAt,
    string? Notes
);

public record AddCommentRequest(
    string AuthorName,
    string Text
);
