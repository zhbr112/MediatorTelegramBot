using MediatorTelegramBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MediatorTelegramBot.Callback;

/// <summary>
///     Chatbot command
/// </summary>
public interface ICallbackQuery
{
    /// <summary>
    ///     Check if this command shall be executed for given update
    /// </summary>
    /// <param name="context">The context of the command</param>
    /// <returns>true if the command shall be executed, otherwise false</returns>
    bool CanExecute(CallbackQuery callbackQuery);

    /// <summary>
    ///     Execute the command
    /// </summary>
    /// <param name="botClient">Telegram bot client</param>
    /// <param name="context">Command context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}