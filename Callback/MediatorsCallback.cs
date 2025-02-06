using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using MediatorTelegramBot.Resources;
using Telegram.Bot.Types.ReplyMarkups;

namespace MediatorTelegramBot.Callback;

public class MediatorsCallback:ICallbackQuery
{
    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.Split(' ')[0].Equals("Mediators", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await botClient.AnswerCallbackQuery(callbackQuery.Id);

        await botClient.SendMessage(callbackQuery.Message.Chat.Id, CommandStrings.Start_Welcome, 
            cancellationToken: cancellationToken);
    }
}
