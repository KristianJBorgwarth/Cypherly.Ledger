using System.Reflection;
using FluentValidation;
using Ledger.Application.Abstractions;
using Ledger.Application.Behavior;
using Ledger.Application.Interfaces;
using Ledger.Application.Services;
using Ledger.Domain.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.Application.Extensions;

public static class ApplicationExtensions
{
    public static void AddApplication(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly);

        services.AddSingleton<ISignatureHelper, SignatureHelper>();

        // Options are read by the source generator at compile time, so they must stay inline constants
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.Assemblies = [typeof(ICommand), typeof(IDomainEvent)];
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>), typeof(ExceptionBehavior<,>)];
        });
    }
}
