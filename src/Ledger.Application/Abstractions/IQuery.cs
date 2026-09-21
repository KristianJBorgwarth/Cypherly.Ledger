using Ledger.Domain.Common;
using Mediator;

namespace Ledger.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
