namespace Ledger.Application.Exceptions;

public sealed class ConcurrencyConflictException(string message, Exception inner) : Exception(message, inner);
