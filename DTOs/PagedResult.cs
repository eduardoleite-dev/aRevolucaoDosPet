namespace aRevolucaoDosPet.DTOs;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    long TotalCount,
    int PageNumber,
    int PageSize
);