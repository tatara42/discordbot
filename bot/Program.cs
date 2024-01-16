using bot;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using static bot.GelbooruService;

public class Program
{
    private DiscordSocketClient? _client;
    private CommandService? _commands;
    private IServiceProvider? _services;
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly ObjectJson JsonData = getData("data.json");


    public static void Main(string[] args)
        => new Program().RunBotAsync().GetAwaiter().GetResult();

    public async Task RunBotAsync()
    {
        DotEnv.Load();
        // Load the Discord token from the environment variable
        string discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        if (string.IsNullOrEmpty(discordToken))
        {
            Console.WriteLine("Discord token is not set. Please set the DISCORD_TOKEN environment variable.");
            return;
        }
        var configuration = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.Guilds |
                     GatewayIntents.GuildMessages |
                     GatewayIntents.MessageContent |
                     GatewayIntents.GuildMembers |
                     GatewayIntents.DirectMessages
        };

        _client = new DiscordSocketClient(configuration);
        _commands = new CommandService();

        _client.Ready += Client_Ready;
        _client.UserJoined += UserJoined;
        _client.SlashCommandExecuted += SlashCommandHandler;

        await InstallCommandsAsync();

        _client.Log += Log;

        await _client.LoginAsync(TokenType.Bot, discordToken);

        await _client.StartAsync();

        await Task.Delay(-1);

    }


    private Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }
    public async Task Client_Ready()
    {
        // Global Command Builder
        List<ApplicationCommandProperties> applicationCommandProperties = new();
        try
        {
            // Simple help slash command.
            SlashCommandBuilder globalCommandHelp = new SlashCommandBuilder();
            globalCommandHelp.WithName("random");
            globalCommandHelp.WithNsfw(true);
            globalCommandHelp.WithDescription("Get random favourite art of tuturu42.");
            applicationCommandProperties.Add(globalCommandHelp.Build());

            // Slash command with name as its parameter.
            SlashCommandOptionBuilder gelbooruCommandTagOption = new();
            gelbooruCommandTagOption.WithName("tag");
            gelbooruCommandTagOption.WithType(ApplicationCommandOptionType.String);
            gelbooruCommandTagOption.WithDescription("Add a Tag");
            gelbooruCommandTagOption.WithRequired(true);

            SlashCommandBuilder globalGelbooruCommand = new SlashCommandBuilder();
            globalGelbooruCommand.WithName("gelbooru");
            globalGelbooruCommand.WithDescription("Get Image or Video from gelbooru website");
            globalGelbooruCommand.AddOptions(gelbooruCommandTagOption);
            applicationCommandProperties.Add(globalGelbooruCommand.Build());

            SlashCommandBuilder globalGelbooruNsfwCommand = new SlashCommandBuilder();
            globalGelbooruNsfwCommand.WithName("nsfw");
            globalGelbooruNsfwCommand.WithNsfw(true);
            globalGelbooruNsfwCommand.WithDescription("Get NSFW Image or Video from gelbooru website");
            globalGelbooruNsfwCommand.AddOptions(gelbooruCommandTagOption);
            applicationCommandProperties.Add(globalGelbooruNsfwCommand.Build());

            await _client.BulkOverwriteGlobalApplicationCommandsAsync(applicationCommandProperties.ToArray());
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }

    }
    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        try
        {
            switch (command.Data.Name)
            {
                case "gelbooru":
                    var options = command.Data.Options?.First()?.Value?.ToString();

                    if (IsValidTag(options))
                    {
                        using (var imageUrl = GelbooruService.getURL().withTag(options).withBlacklist(JsonData.Blacklist).Build())
                        {
                            await command.RespondAsync(await imageUrl);
                        }
                    }
                    break;

                case "random":
                    using (var imageUrl = GelbooruService.getURL().withBlacklist(JsonData.Blacklist).withNsfw(true).withArtist(JsonData.Artists).Build())
                    {
                        await command.RespondAsync(await imageUrl);
                    }
                    break;

                case "nsfw":
                    options = command.Data.Options?.First()?.Value?.ToString();

                    if (IsValidTag(options))
                    {
                        using (var imageUrl = GelbooruService.getURL().withTag(options).withBlacklist(JsonData.Blacklist).withNsfw(true).Build())
                        {
                            await command.RespondAsync(await imageUrl);
                        }
                    }
                    break;

                default:
                    await command.RespondAsync("EVERYTHING IS FINE!", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions appropriately, log them, and respond accordingly.
            Console.Error.WriteLine($"An error occurred: {ex.Message}");
            await command.RespondAsync("An error occurred while processing the command.", ephemeral: true);
        }
    }


    public async Task InstallCommandsAsync()
    {
        _client.MessageReceived += HandleCommandAsync;

        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }
    private async Task HandleCommandAsync(SocketMessage arg)
    {
        // Don't process the command if it was a system message
        var message = arg as SocketUserMessage;
        if (message == null) return;

        // Create a number to track where the prefix ends and the command begins
        int argPos = 0;

        // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        if (!(message.HasCharPrefix('!', ref argPos) ||
            message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
            message.Author.IsBot)
            return;

        // Create a WebSocket-based command context based on the message
        var context = new SocketCommandContext(_client, message);

        // Execute the command with the command context we just
        // created, along with the service provider for precondition checks.
        await _commands.ExecuteAsync(
            context: context,
            argPos: argPos,
            services: null);
    }
    private async Task UserJoined(SocketGuildUser user)
    {
        var channel = user.Guild.DefaultChannel; // You might want to customize this based on your server's setup

        if (channel != null)
        {
            await channel.SendMessageAsync($"Welcome to the server, {user.Mention}!");
        }
    }

    private static bool IsValidTag(string tag)
    {
        return !string.IsNullOrEmpty(tag) && (tag.All(x => char.IsLetterOrDigit(x) || char.IsWhiteSpace(x))
               || tag.Contains("(") || tag.Contains(")") || tag.Contains("_") || tag.Contains("*") || tag.Contains("~"));
    }

    private static ObjectJson getData(string filename)
    {
        string file = System.IO.Path.GetFullPath(filename);
        var jsonArray = File.ReadAllText(file);

        // Deserialize the JSON array into a C# string array
        return JsonConvert.DeserializeObject<ObjectJson>(jsonArray);
    }

}


