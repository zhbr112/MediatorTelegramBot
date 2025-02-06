﻿using System.Reflection;
using MediatorTelegramBot.Callback;
using MediatorTelegramBot.Commands;
using MediatorTelegramBot.Configuration;
using MediatorTelegramBot.Services;
using MediatorTelegramBot.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;

var builder = Host.CreateApplicationBuilder(args);

// Add user secrets
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);

builder.Configuration.AddJsonFile("secrets.json");

// Parse settings
builder.Services.Configure<TelegramSettings>(builder.Configuration);

// Add Telegram update middleware pipeline
builder.AddMiddlewarePipeline();

// Add Telegram bot services to the DI container
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddSingleton<CommandParserService>();

// Register commands
builder.Services.AddCommand<StartCommand>();
builder.Services.AddCommand<HelpCommand>();
builder.Services.AddCommand<FindMediatorCommand>();

builder.Services.AddCallback<MediatorsCallback>();

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