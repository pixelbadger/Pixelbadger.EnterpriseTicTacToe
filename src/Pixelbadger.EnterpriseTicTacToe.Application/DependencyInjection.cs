using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Pixelbadger.EnterpriseTicTacToe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
