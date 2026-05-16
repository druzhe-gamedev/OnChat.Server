using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OnChat.Protocol.Types;
using OnChat.Shared.Validation;

namespace OnChat.Common.Validation;

public static class ValidatorsExplorer
{
    public static void AddValidators(this IServiceCollection serviceCollection, params Assembly[] assemblies)
    {
        TypeInfo[] validators = assemblies
                                .SelectMany(a => a.DefinedTypes)
                                .Where(t => t is { IsConcrete: true, BaseType.IsGenericType: true } &&
                                            t.BaseType.GetGenericTypeDefinition() == typeof(ValidatorBase<,>)
                                ).ToArray();
        
        foreach (TypeInfo validatorType in validators)
        {
            Type genericType = validatorType.BaseType!.GenericTypeArguments[0];
            
            serviceCollection.AddScoped(typeof(IValidator<>).MakeGenericType(genericType), validatorType);
        }
    }
}