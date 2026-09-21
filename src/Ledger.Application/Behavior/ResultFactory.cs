using Ledger.Domain.Common;

namespace Ledger.Application.Behavior;

internal static class ResultFactory
{
    public static TResponse Fail<TResponse>(Error error)
        where TResponse : Result
    {
        if (!typeof(TResponse).IsGenericType || typeof(TResponse).GetGenericTypeDefinition() != typeof(Result<>))
            return (TResponse)Result.Fail(error);

        var resultType = typeof(TResponse).GetGenericArguments()[0];

        var method = typeof(Result).GetMethods()
            .First(m => m is { Name: "Fail", IsGenericMethod: true });

        var genericFailMethod = method.MakeGenericMethod(resultType);

        return (TResponse)genericFailMethod.Invoke(null, [error])!;
    }
}
