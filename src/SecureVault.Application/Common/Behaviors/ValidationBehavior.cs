using FluentResults;
using FluentValidation;
using MediatR;

namespace SecureVault.Application.Common.Behaviors;

// Pipeline do MediatR: toda command/query passa por aqui antes de chegar ao handler.
// Se houver erros de validação, retorna Fail imediatamente sem executar o handler.
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : ResultBase, new()
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures.Select(f => new Error(f.ErrorMessage)
            .WithMetadata("field", f.PropertyName)
            .WithMetadata("code", f.ErrorCode));

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Fail(errors);

        var valueType = typeof(TResponse).GenericTypeArguments.Single();
        var failMethod = typeof(Result)
            .GetMethods()
            .Single(m =>
                m.Name == nameof(Result.Fail) &&
                m.IsGenericMethodDefinition &&
                m.GetParameters() is [{ ParameterType: var parameterType }] &&
                parameterType == typeof(IEnumerable<IError>));

        return (TResponse)failMethod
            .MakeGenericMethod(valueType)
            .Invoke(null, new object[] { errors })!;
    }
}
