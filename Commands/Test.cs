using MediatorTelegramBot.Attributes;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Amazon.Runtime;
using Amazon.S3;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Amazon.S3.Model;

namespace MediatorTelegramBot.Commands;

[BotCommand("/test", "Sends a greeting message")]
public class TestCommand(S3Client s3Client, ILogger<TestCommand> logger) : IChatCommand
{
    public bool CanExecute(CommandContext context)
    {
        return context.Command?.Equals("/test", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CommandContext context,
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(context.Message.Chat.Id, context.Message.Chat.Id.ToString(), cancellationToken: cancellationToken);


        var listResponse = await s3Client.S3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "public"
        });

        foreach (var obj in listResponse.S3Objects)
        {
            await botClient.SendMessage(context.Message.Chat.Id, obj.Key, cancellationToken: cancellationToken);
        }
    }
}