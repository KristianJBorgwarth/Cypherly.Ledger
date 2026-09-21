using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.Application.Extensions;

public static class ApplicationExtensions
{
    /// <summary>
    /// Registers application services. Mediator itself is wired in the API project,
    /// since its source generator emits <c>AddMediator</c> in the outermost project.
    /// </summary>
    public static void AddApplication(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly);
    }
}
