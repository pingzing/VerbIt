namespace VerbIt.Client.Models;

public struct Result<T, E>
{
    private T? _ok;
    private E? _error;

    bool _isOk = false;
    bool _isError = false;

    private Result(T ok)
    {
        _ok = ok;
        _error = default;

        _isOk = true;
        _isError = false;

        if (_isOk == _isError)
        {
            throw new ArgumentException();
        }
    }

    private Result(E error)
    {
        _error = error;
        _ok = default;

        _isOk = false;
        _isError = true;

        if (_isOk == _isError)
        {
            throw new ArgumentException();
        }
    }

    public bool IsOk => _isOk;

    public bool IsError => _isError;

    public T Unwrap()
    {
        if (_isError || _ok == null)
        {
            throw new ArgumentNullException(nameof(_ok));
        }

        return _ok;
    }

    public E UnwrapError()
    {
        if (_isOk || _error == null)
        {
            throw new ArgumentNullException(nameof(_error));
        }

        return _error;
    }

    public static Result<T, E> Ok(T value)
    {
        return new Result<T, E>(value);
    }

    public static Result<T, E> Error(E value)
    {
        return new Result<T, E>(value);
    }
}
