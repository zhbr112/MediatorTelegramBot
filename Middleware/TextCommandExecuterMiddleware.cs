﻿using MediatorTelegramBot.Commands;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediatorTelegramBot.Middleware;

/// <summary>
///     Middleware that matches and executes text commands
/// </summary>
public class TextCommandExecuterMiddleware : IBotMiddleware
{
    /// <summary>
    ///     Find and execute all text commands
    /// </summary>
    public async Task InvokeAsync(UpdateContext context, BotMiddlewareDelegate next)
    {
        if (context.Update.Message?.Text is null)
        {
            await next(context);
            return;
        }

        await using var scope = context.Services.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TextCommandExecuterMiddleware>>();

        // Get all registered commands
        var commands = scope.ServiceProvider.GetServices<IChatCommand>().ToList();

        logger.LogDebug("Found {count} commands.", commands.Count);

        // Build command context
        var cmdParser = scope.ServiceProvider.GetRequiredService<CommandParserService>();
        var ctx = cmdParser.BuildContext(context.Update.Message!);

        // Find commands that match
        var matchingCommands = commands.Where(command => command.CanExecute(ctx)).ToList();

        logger.LogDebug("{count} commands match", matchingCommands.Count);

        // Execute all matching commands
        foreach (var command in matchingCommands)
        {
            logger.LogDebug("Executing command: {name}", command.GetType().Name);
            await command.ExecuteAsync(context.Client, ctx, context.CancellationToken);
            logger.LogDebug("Done executing command {name}", command.GetType().Name);
        }

        // Proceed execution
        await next(context);
    }
}