using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace TorneioBot
{
    public class SlashCommand
    {
        private string _discordBaseUrl;
        private static readonly HttpClient httpClient = new HttpClient();        
        private string _botToken;
        private ulong _guildId;
        private ulong _clientId;

        public SlashCommand(Config config)
        {
            _discordBaseUrl = "https://discord.com/api/v10/applications";
            _botToken = config.Token;            
            _guildId = config.GuildId;
            _clientId = config.ClientId;

            DeleteCommandsAsync().Wait();
            BuildCommands().ForEach(async cmd => await RegisterSlashCommandAsync(cmd));            
            Console.WriteLine($"Slash commands updated!");
        }

        private async Task DeleteCommandsAsync()
        {            
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {_botToken}");
            var responseGet = await httpClient.GetAsync($"{_discordBaseUrl}/{_clientId}/guilds/{_guildId}/commands");
            GetIdsFromJson(await responseGet.Content.ReadAsStringAsync())
                .ForEach(async challengeCommandId => {
                    await httpClient.DeleteAsync($"{_discordBaseUrl}/{_clientId}/guilds/{_guildId}/commands/{challengeCommandId}");
                });            
        }

        public async Task RegisterSlashCommandAsync(CommandPayload commandPayload)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bot {_botToken}");        
            var jsonString = JsonSerializer.Serialize(commandPayload);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{_discordBaseUrl}/{_clientId}/guilds/{_guildId}/commands", content);
            await response.Content.ReadAsStringAsync();
        }

        private List<CommandPayload> BuildCommands()
        {
            var result = new List<CommandPayload>()
            {
                new CommandPayload 
                { 
                    name = "tournament_012_new_match", description = "Start a new tournament 012 match", 
                    options = new[] 
                        { 
                            new Option{name="player-1", description="Inform the player1.", type=6, required=true },
                            new Option{name="player-2", description="Inform the player2.", type=6, required=true },
                            new Option{name="rounds", description="Inform the total number of rounds.", type=4, required=true },
                            new Option{name="match", description="Match description will show in overlay title.", type=3, required=true },
                            new Option{name="bracket", description="Inform the bracket title will show in overlay title.", type=3, required=true },
                            new Option{name="host", description="Inform the player that needs to host the match.", type=6, required=true },
                            new Option{name="announcement-channel", description="Inform the channel to call players to the group battle", type=7, required=true },
                        }
                },                
                new CommandPayload  
                {
                    name = "tournament_012_set_winner", description = "Start a new tournament 012 match", 
                        options = new[] 
                            {                                   
                                new Option{name="winner", description="Inform winner", type=6, required=true },                            
                            }    
                },
                new CommandPayload
                {
                    name = "tournament_012_start_round", description="Start ban phase and inititate the next round",
                    options = new []
                    {
                        new Option{name="next_player_pick", description="Inform none for send pick to all", type=6, required=false}
                    }
                }
            };

            return result;
        }

        private List<string> GetIdsFromJson(string jsonString)
        {
            List<string> ids = new List<string>();

            JArray jsonArray = JArray.Parse(jsonString);

            foreach (JObject jsonObject in jsonArray)
            {
                foreach (KeyValuePair<string, JToken> kvp in jsonObject)
                {
                    if (kvp.Key == "id")
                    {
                        ids.Add(kvp.Value.ToString());
                    }
                }
            }

            return ids;
        }        
    }
}