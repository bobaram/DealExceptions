using DealExceptions.Domain;

namespace DealExceptions.Application.DTOs;

public record ExceptionSummaryDto(
    int Id,
    string DealRef,
    string ClientName,
    string ExceptionType,
    string Priority,
    string Status,
    string? AssignedOwner,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsOpen,
    bool IsCritical,
    bool IsPossibleDuplicate
);

public record ExceptionDetailDto(
    int Id,
    string DealRef,
    string ClientName,
    string ExceptionType,
    string Description,
    string Priority,
    string Status,
    string? AssignedOwner,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsOpen,
    bool IsCritical,
    bool IsPossibleDuplicate,
    IEnumerable<CommentDto> Comments,
    IEnumerable<StatusHistoryDto> StatusHistories
);

public record CreateExceptionRequest(
    string DealRef,
    string ClientName,
    string ExceptionType,
    string Description,
    string Priority,
    string? AssignedOwner,
    string CreatedBy
);

public record UpdateStatusRequest(
    string Status,
    string ChangedBy,
    string? Notes
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
