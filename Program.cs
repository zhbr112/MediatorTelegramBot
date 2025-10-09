using MediatorTelegramBot.Callback;
using MediatorTelegramBot.Callbackж;
using MediatorTelegramBot.Commands;
using MediatorTelegramBot.Configuration;
using MediatorTelegramBot.Data;
using MediatorTelegramBot.Models;
using MediatorTelegramBot.Services;
using MediatorTelegramBot.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = Host.CreateApplicationBuilder(args);

// Add user secrets
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);

//builder.Configuration.AddJsonFile("secrets.json");

builder.Services.AddTransient<S3Client>();

builder.Services.AddDbContext<MediatorDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Parse settings
builder.Services.Configure<TelegramSettings>(builder.Configuration);
builder.Services.Configure<BotConfiguration>(builder.Configuration);

builder.Services.AddSingleton<AdminsAddMediators>();
builder.Services.AddSingleton<AdminsEditMediators>();
builder.Services.AddSingleton<UsersAddingReview>();
builder.Services.AddSingleton<UsersInSearchProcess>();
builder.Services.AddSingleton<UsersAskingQuestion>();
builder.Services.AddSingleton<AdminsReplyingToQuestion>();

// Add Telegram update middleware pipeline
builder.AddMiddlewarePipeline();

// Add Telegram bot services to the DI container
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddSingleton<CommandParserService>();
builder.Services.AddScoped<EditMediatorFlowService>();
builder.Services.AddScoped<MediatorCardService>();

// Register commands
builder.Services.AddCommand<StartCommand>();
builder.Services.AddCommand<HelpCommand>();
builder.Services.AddCommand<FindMediatorCommand>();
builder.Services.AddCommand<AddMediatorCommand>();
builder.Services.AddCommand<DeleteMediatorsCommand>();
builder.Services.AddCommand<EditMediatorsCommand>();
builder.Services.AddCommand<EditMediatorProcessCommand>();
builder.Services.AddCommand<MyFavoritesCommand>();
builder.Services.AddCommand<AddReviewProcessCommand>();
builder.Services.AddCommand<SearchCommand>();
builder.Services.AddCommand<SearchProcessCommand>();
builder.Services.AddCommand<AskQuestionCommand>();
builder.Services.AddCommand<AskQuestionProcessCommand>();
builder.Services.AddCommand<AnswerQuestionProcessCommand>();

//builder.Services.AddCommand<TestCommand>();

builder.Services.AddCallback<GetMediatorsCallback>();
builder.Services.AddCallback<DeleteMediatorsCallback>();
builder.Services.AddCallback<EditMediatorCallback>();
builder.Services.AddCallback<EditSkipCallback>();
builder.Services.AddCallback<ToggleFavoriteCallback>();
builder.Services.AddCallback<ViewReviewsCallback>();
builder.Services.AddCallback<AddReviewCallback>();
builder.Services.AddCallback<SetReviewRatingCallback>();
builder.Services.AddCallback<FinishReviewCallback>();
builder.Services.AddCallback<GoBackToMediatorCallback>();
builder.Services.AddCallback<AnswerQuestionCallback>();

// Avoid circular dependency in /help
builder.Services.AddTransient<Func<IEnumerable<IChatCommand>>>(sp =>
    sp.GetServices<IChatCommand>);

// Build the application
var app = builder.Build();

// Add middlewares
app.UseErrorHandler(); // Send error messages to users
app.UseUpdateLogger(); // Log received Telegram bot updates
if (builder.Environment.IsDevelopment()) app.UseRequestTimer(); // Measure update processing time
app.UseTextCommands(); // Handle text commands
app.UseCallbackQuery();// Start the application
app.Run();