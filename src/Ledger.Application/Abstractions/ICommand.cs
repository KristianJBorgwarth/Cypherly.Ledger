using Ledger.Domain.Common;
using Mediator;

namespace Ledger.Application.Abstractions;

public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
