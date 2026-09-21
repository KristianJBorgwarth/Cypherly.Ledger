using Ledger.Domain.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Behavior;

public sealed class ExceptionBehavior<TMessage, TResponse>(
    ILogger<ExceptionBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing request of type {RequestType}", typeof(TMessage).Name);

            var error = Error.Failure("An unexpected error occurred: " + ex.Message);
            return ResultFactory.Fail<TResponse>(error);
        }
    }
}
