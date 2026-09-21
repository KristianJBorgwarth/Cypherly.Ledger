using FluentValidation;
using Ledger.Domain.Common;
using Mediator;
using Microsoft.Extensions.Logging;

// ReSharper disable InvertIf

namespace Ledger.Application.Behavior;

public sealed class ValidationBehavior<TMessage, TResponse>(
    ILogger<ValidationBehavior<TMessage, TResponse>> logger,
    IValidator<TMessage>? validator = null)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validator is null)
            return await next(message, cancellationToken);

        var validationResult = await validator.ValidateAsync(message, cancellationToken);

        if (validationResult.IsValid)
            return await next(message, cancellationToken);

        var errorMessage = string.Join("; ", validationResult.Errors.Select(e => $"{e.ErrorMessage} ({e.PropertyName})"));

        logger.LogWarning("Validation failed for {RequestType}: {ErrorMessage}", typeof(TMessage).Name, errorMessage);

        var error = Error.Validation("Validation failed: " + errorMessage);
        return ResultFactory.Fail<TResponse>(error);
    }
}
