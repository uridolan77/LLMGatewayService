using MediatR;

namespace LLMGateway.Core.CQRS;

/// <summary>
/// Base interface for queries
/// </summary>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Base interface for commands
/// </summary>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Base interface for command handlers
/// </summary>
/// <typeparam name="TCommand">Type of the command</typeparam>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Base interface for query handlers
/// </summary>
/// <typeparam name="TQuery">Type of the query</typeparam>
/// <typeparam name="TResponse">Type of the response</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
