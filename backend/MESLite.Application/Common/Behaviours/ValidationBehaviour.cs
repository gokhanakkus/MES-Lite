using FluentValidation;
using MediatR;

namespace MESLite.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all FluentValidation validators registered for a request
/// before its handler executes. Throws <see cref="ValidationException"/> on the first failed batch.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
