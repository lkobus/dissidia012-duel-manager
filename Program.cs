using System.Reactive;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using TorneioBot;

class Program
{
    public static Config CONFIG;    
    public static LanguageManager LANGUAGE_MANAGER;
    private DiscordSocketClient _client;
    private static MatchService _matchService;
    static async Task Main(string[] args)
    {
        var program = new Program();
        await program.Run(args);
    }

    public async Task Run(string[] args)
    {                
        CONFIG = JsonConvert.DeserializeObject<Config>(
            File.ReadAllText(".\\app-config.json")
        );
        LANGUAGE_MANAGER = new LanguageManager(CONFIG.Language);
        new SlashCommand(CONFIG);

        _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Info, 
            UseInteractionSnowflakeDate = false
        });        
        
        _client.Log += LogAsync;
        _client.InteractionCreated += InteractionCreatedAsync; 
        _client.Ready += ReadyAsync;
        
        await _client.LoginAsync(TokenType.Bot, CONFIG.Token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private static async Task InteractionCreatedAsync(SocketInteraction interaction)
    {            
        if (interaction is SocketSlashCommand slashCommand)
        {                        
            if  (slashCommand.Data.Name == "tournament_012_new_match")
            {
                var player1DiscordId = (slashCommand.Data.Options.FirstOrDefault(x => x.Name == "player-1")?.Value as SocketGuildUser).Id;
                var player2DiscordId = (slashCommand.Data.Options.FirstOrDefault(x => x.Name == "player-2")?.Value as SocketGuildUser).Id;                    
                var totalRounds = slashCommand.Data.Options.FirstOrDefault(x => x.Name == "rounds")?.Value.ToString();
                var match = slashCommand.Data.Options.FirstOrDefault(x => x.Name == "match")?.Value.ToString();
                var bracket = slashCommand.Data.Options.FirstOrDefault(x => x.Name == "bracket")?.Value.ToString();
                var host = (slashCommand.Data.Options.FirstOrDefault(x => x.Name == "host")?.Value as SocketGuildUser).Id;
                var announcementChannel = (slashCommand.Data.Options.FirstOrDefault(x => x.Name == "announcement-channel")?.Value as SocketTextChannel).Id;

                await Task.Factory.StartNew(async () => await _matchService.StartNewMatch(player1DiscordId, player2DiscordId, Convert.ToInt32(totalRounds), match, bracket, host, announcementChannel));                    
                
                await slashCommand.RespondAsync(LANGUAGE_MANAGER.GetLocalizedString("new_match_confirmation"));                    
            }
            else if(slashCommand.Data.Name == "tournament_012_start_round")
            {
                Task.Factory.StartNew(async () => {
                    var player1DiscordId = (slashCommand.Data.Options.FirstOrDefault(x => x.Name == "next_player_pick")?.Value as SocketGuildUser)?.Id;
                    await _matchService.ExecuteNextStep();                        
                });                
                await slashCommand.RespondAsync(LANGUAGE_MANAGER.GetLocalizedString("blind_pick_start_message"));
            } else if(slashCommand.Data.Name == "tournament_012_set_winner")
            {                
                var winner = (slashCommand.Data.Options.FirstOrDefault(x => x.Name == "winner")?.Value as SocketGuildUser).Id;                
                Task.Factory.StartNew(async () => await _matchService.SetWinner(winner));
                await slashCommand.RespondAsync(LANGUAGE_MANAGER.GetLocalizedString("winners_setted"));
            }
        }
    }

    public async Task MyMenuHandler(SocketMessageComponent arg)
    {   
        if(arg.Data.CustomId == "mapBan")
        {
            await Task.Factory.StartNew(async () => {
                await _matchService.BanStage(arg.Data.Values.FirstOrDefault(), arg.ChannelId);
                await arg.Message.DeleteAsync();
            });
            
            await arg.RespondAsync(string.Format(LANGUAGE_MANAGER.GetLocalizedString("blind_pick_map_ban_confirmation_message"), arg.Data.Values.FirstOrDefault()));            
        } else if(arg.Data.CustomId == "mapPick")
        {
            await Task.Factory.StartNew(async () => {
                await Task.Delay(1000);
                await _matchService.PickStage(arg.Data.Values.FirstOrDefault());
                await arg.Message.DeleteAsync();    
            });
            
            await arg.RespondAsync(string.Format(LANGUAGE_MANAGER.GetLocalizedString("blind_pick_map_choice_confirmation_message"), arg.Data.Values.FirstOrDefault()));
        } else if(arg.Data.CustomId == "characterHeroesPick" || arg.Data.CustomId == "characterVillainsPick")
        {
            await Task.Factory.StartNew(async () => {
                await _matchService.SetBlindPick(
                    arg.Data.Values.FirstOrDefault(), arg.ChannelId
                );
                await arg.Message.DeleteAsync();
            });

            
            await arg.RespondAsync(string.Format(LANGUAGE_MANAGER.GetLocalizedString("blind_pick_character_choice_confirmation_message"), arg.Data.Values.FirstOrDefault()));
        }
    }

    public async Task ReadyAsync()
    {
        _matchService = new MatchService(_client, CONFIG);
        _client.Log += LogAsync;
        _client.Ready -= ReadyAsync;
        _client.SelectMenuExecuted += MyMenuHandler;    
    }

    public Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log);
        return Task.CompletedTask;
    }
}
