namespace Ledger.Domain.Common;

public enum ErrorType
{
    /// <summary>
    /// Indicates that an unexpected error occurred on the server.
    /// </summary>
    Failure = 0, // 500 Internal Server Error

    /// <summary>
    /// Indicates that the request was invalid due to validation errors.
    /// </summary>
    Validation = 1, // 400 Bad Request
    
    /// <summary>
    /// Indicates that the requested resource could not be found.
    /// </summary>
    NotFound = 2, // 404 Not Found
    
    /// <summary>
    /// Indicates that the request requires user authentication.
    /// </summary>
    Unauthorized = 3, // 401 Unauthorized
    
    /// <summary>
    /// Will be used for 400 Bad Request errors that are not related to validation.
    /// </summary>
    BadRequest = 5 // 400 Bad Request
}