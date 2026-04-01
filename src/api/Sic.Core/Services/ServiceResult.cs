namespace Sic.Core.Services;

public class ServiceResult<T>
{
    public bool Success { get; }
    public T? Value { get; }
    public string? Error { get; }

    private ServiceResult(T value) { Success = true; Value = value; }
    private ServiceResult(string error) { Success = false; Error = error; }

    public static ServiceResult<T> Ok(T value) => new(value);
    public static ServiceResult<T> Fail(string error) => new(error);
}

public class ServiceResult
{
    public bool Success { get; }
    public string? Error { get; }

    private ServiceResult() { Success = true; }
    private ServiceResult(string error) { Success = false; Error = error; }

    public static ServiceResult Ok() => new();
    public static ServiceResult Fail(string error) => new(error);
}
