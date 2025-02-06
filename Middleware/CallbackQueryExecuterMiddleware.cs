using MediatorTelegramBot.Callback;
using MediatorTelegramBot.Commands;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediatorTelegramBot.Middleware;

/// <summary>
///     Middleware that matches and executes text commands
/// </summary>
public class CallbackQueryExecuterMiddleware : IBotMiddleware
{
    /// <summary>
    ///     Find and execute all text commands
    /// </summary>
    public async Task InvokeAsync(UpdateContext context, BotMiddlewareDelegate next)
    {
        if (context.Update.CallbackQuery?.Data is null)
        {
            await next(context);
            return;
        }

        await using var scope = context.Services.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CallbackQueryExecuterMiddleware>>();

        // Get all registered commands
        var commands = scope.ServiceProvider.GetServices<ICallbackQuery>().ToList();

        logger.LogDebug("Found {count} CallbackQuery", commands.Count);


        // Find commands that match
        var CallbackQuerys = commands.Where(command => command.CanExecute(context.Update.CallbackQuery)).ToList();

        logger.LogDebug("{count} CallbackQuery match", CallbackQuerys.Count);

        // Execute all matching commands
        foreach (var command in CallbackQuerys)
        {
            logger.LogDebug("Executing command: {name}", command.GetType().Name);
            await command.ExecuteAsync(context.Client, context.Update.CallbackQuery, context.CancellationToken);
            logger.LogDebug("Done executing command {name}", command.GetType().Name);
        }

        // Proceed execution
        await next(context);
    }
}