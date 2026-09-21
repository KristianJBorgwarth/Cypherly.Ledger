using Ledger.Domain.Common;

namespace Ledger.API.Common;

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result)
    {
        if (result.Success)
        {
            throw new InvalidOperationException("Cannot convert a successful result to a problem details");
        }

        return Results.Problem(
            statusCode: result.Error?.Type.ToHttpStatusCode(),
            type: result.Error?.Type.ToProblemDetailsTypeUri(),
            title: result.Error?.Type.ToProblemDetailsTitle(),
            extensions: new Dictionary<string, object?>
            {
            {"errors", new[] {result.Error}}
            }
        );
    }
}
