using Ledger.Domain.Abstractions;
using Mediator;

namespace Ledger.Application.Abstractions;

public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent> where TDomainEvent : IDomainEvent { }
