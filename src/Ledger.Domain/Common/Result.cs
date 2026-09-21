// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Ledger.Domain.Common;

public class Result
{
    public bool Success { get; private set; }
    public Error? Error { get; private set; }

    protected Result(bool success, Error? error)
    {
        Success = success;
        Error = error;
    }

    public static Result Fail(Error? error)
    {
        return new Result(false, error);
    }

    public static Result<T> Fail<T>(Error? error)
    {
        return new Result<T>(default, false, error);
    }

    public static Result Ok()
    {
        return new Result(true, null);
    }

    public static Result<T> Ok<T>(T? value = default)
    {
        return new Result<T>(value, true, null);
    }
}


public class Result<T> : Result
{
    private readonly T? _value;
    
    /// <summary>
    /// Gets the value of the Result if it is successful; otherwise, throws an InvalidOperationException.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T? Value => !Success ? throw new InvalidOperationException("Cannot fetch Value on a failed Result") : _value;
    
    /// <summary>
    /// Gets the value of the Result, throwing an InvalidOperationException if the value is null.
    /// </summary>
    /// <exception cref="InvalidOperationException">In case of accessing failed result</exception>
    /// <exception cref="NullReferenceException">In case of null _value</exception>
    public T RequiredValue
    {
        get
        {
            if(!Success) throw new InvalidOperationException("Cannot fetch RequiredValue on a failed Result");
            return _value ?? throw new NullReferenceException($"RequiredValue is null of type {typeof(T).FullName}");
        }
    }
    
    protected internal Result(T? value, bool success, Error? error)
        : base(success, error)
    {
        _value = value;
    }

    public static implicit operator Result<T>(T from)
    {
        return Ok(from);
    }

    public static implicit operator T(Result<T> from)
    {
        return from.Value!;
    }
}
