using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using MediatorTelegramBot.Resources;
using Telegram.Bot.Types.ReplyMarkups;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Commands;
using Microsoft.Extensions.Logging;
using MediatorTelegramBot.Models;
using Amazon.S3.Model;

namespace MediatorTelegramBot.Callback;

public class GetMediatorsCallback(MediatorDbContext db, S3Client s3Client) : ICallbackQuery
{
    public bool CanExecute(CallbackQuery callbackQuery)
    {
        return callbackQuery.Data?.Split(' ')[0].Equals("Mediators", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ExecuteAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var tag = callbackQuery.Data![(callbackQuery.Data!.IndexOf(' ') + 1)..];


        var mediators = db.Mediators.Where(x => x.Tags.Contains(tag)).ToArray();

        await botClient.AnswerCallbackQuery(callbackQuery.Id);

        foreach (var mediator in mediators)
        {
            var message = $"{mediator.Name}\n{mediator.Description}\n{string.Join("\n", mediator.Tags.Select(x => $"- {x}"))}\nтел.{mediator.Phone}";

            var listResponse = await s3Client.S3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = "mediators",
                Prefix = $"{mediator.Id}.jpg"
            });

            if (listResponse.S3Objects is null)
            {
                await botClient.SendMessage(callbackQuery.Message.Chat.Id, message,
                cancellationToken: cancellationToken);
                continue;
            }

            var presignRequest = new GetPreSignedUrlRequest()
            {
                BucketName = "mediators",
                Key = $"{mediator.Id}.jpg",
                Expires = DateTime.UtcNow.AddSeconds(10),
            };

            var presignedUrlResponse = s3Client.S3.GetPreSignedURL(presignRequest);
          
            await botClient.SendPhoto(callbackQuery.Message.Chat.Id, presignedUrlResponse, caption: message,
                cancellationToken: cancellationToken);
        }
        
    }
}
