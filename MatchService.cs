using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace TorneioBot
{
    public class MatchService
    {
        public string _overlayBasePath;
        private DiscordSocketClient _client;
        private SocketGuild _guild;
        private CancellationTokenSource _ct;                
        private ulong _player1RoleId;
        private ulong _player2RoleId;
        private ulong _player1ChannelId;
        private ulong _player2ChannelId;                
        private ulong _warningChannel;
        public List<string> MapList;
        public List<Match> _matches;
        private Queue<MatchOptions> _setOptions;        
        private ulong _discordPlayer1 = 0;
        private ulong _discordPlayer2 = 0;    
        public int _round = 1;
        private int _totalRounds = 1;
        private string _match = "";
        private string _bracket = "";
        private string _displayName1 = "";
        private string _displayName2 = "";
        public MatchService(DiscordSocketClient client, Config config)
        {
            _player1RoleId = config.Player1RoleId;
            _player2RoleId = config.Player2RoleId;
            _player1ChannelId = config.Player1ChannelId;
            _player2ChannelId = config.Player2ChannelId;
            _overlayBasePath = config.OverlayFullPath;
            _client = client;   
            _guild = _client.GetGuild(config.GuildId);       
            _guild.DownloadUsersAsync().Wait(); 
        }

        public void GenerateSet(int size)
        {
            _setOptions = new Queue<MatchOptions>();
            _matches = new List<Match>() { new Match() };            
            _setOptions.Enqueue(new MatchOptions("COIN_FLIP", round:1));
            _setOptions.Enqueue(new MatchOptions("BUILD", round:1));            
            for(var i = 1; i < size; i++)
            {                
                _setOptions.Enqueue(new MatchOptions("BLIND_PICK", i+1));
                _setOptions.Enqueue(new MatchOptions("BUILD", i+1));
                _matches.Add(new Match());
            }
                    
        }

        public async Task BanStage(string stageBanned, ulong? mapBannedChannelId)
        {
            _ct.Cancel();            
            _matches[_round - 1].BannedStage = stageBanned;
            MapList.Remove(stageBanned);            
            if(_player1ChannelId == mapBannedChannelId)
            {   
                await StartMapSelect(stageBanned, _displayName1, _player2ChannelId, _discordPlayer2);                
            }
            else if(_player2ChannelId == mapBannedChannelId)
            {                
                await StartMapSelect(stageBanned, _displayName2, _player1ChannelId, _discordPlayer1);
            }
        }

        private async Task StartMapSelect(string stageBanned, string displayName, ulong playerChannelId, ulong discordPlayerId)
        {
            await SendMapPickVoteMessageAsync(playerChannelId, discordPlayerId);
            StartTimer(2);
            await Task.Delay(1200);                
            File.Copy(Path.Combine(_overlayBasePath, @"results\loading.gif"), 
                Path.Combine(_overlayBasePath, "player_1_loading.gif"), true
            );                                
            UpdateOverlayTitle(string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("overlay_title_update_ban_choice_message"), displayName, stageBanned));

        }        

        private string GenerateDateTimeString(int minutes)
        {            
            DateTime currentTime = DateTime.Now;
            DateTime pickEndTime = currentTime.AddMinutes(minutes);            
            return $"<t:{(int)pickEndTime.Subtract(new DateTime(1970, 1, 1).AddHours(-3)).TotalSeconds}:R>";
        }
        public async Task SendMapPickVoteMessageAsync(ulong channelId, ulong discordPlayerId)
        {
            var pickEndTimeString = GenerateDateTimeString(2);

            var channel = _client.GetChannel(channelId) as ISocketMessageChannel;
            if (channel == null)
            {                
                return;
            }
            var embed = new EmbedBuilder
            {
                Title = Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_map_choice_title"),
                Description = string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_map_choice_description"), pickEndTimeString),
                Color = Color.Green,
            }.Build();

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("mapPick")
                .WithPlaceholder(Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_map_choice_place_holder"));
            MapList.ForEach(m => selectMenu.AddOption(m , m));                

            await channel.SendMessageAsync(MentionUser(discordPlayerId), embed:embed, components:new ComponentBuilder()
                .WithSelectMenu(selectMenu).Build());
        }
        
        public async Task SendMapBanVoteMessageAsync(ulong channelId, ulong playerDiscordId)
        {            
            var pickEndTimeString = GenerateDateTimeString(2);
            var channel = _client.GetChannel(channelId) as ISocketMessageChannel;
            if (channel == null)
            {
                Console.Error.WriteLine($"Channel not found {channelId}");
                return;
            }
            var embed = new EmbedBuilder
            {
                Title = Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_map_ban_title"),
                Description = 
                string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_map_ban_description"), pickEndTimeString),
                Color = Color.Red,
            }.Build();
                                        
            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("mapBan")
                .WithPlaceholder(Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_map_ban_place_holder"));
            
            MapList.ForEach(m => selectMenu.AddOption(m , m));                
            
            var properties = new ComponentBuilder()
                .WithSelectMenu(selectMenu);            

            await channel.SendMessageAsync(MentionUser(playerDiscordId), embed:embed, components:properties.Build());
        }

        private void SetPlayerResult(int round, string player1Result, string player2Result)
        {
            _matches[round -1].Player1Result = player1Result;
            _matches[round-1].Player2Result = player2Result;           
        }

        private void UpdateWinnerResultAndOverlay(string player1Result, string player2Result, string displayName)
        {
            SetPlayerResult(_round, player1Result, player2Result);                
            UpdateOverlayTitle(
                string.Format(
                    Program.LANGUAGE_MANAGER.GetLocalizedString("overlay_title_set_winner_message"),
                    displayName, _round
                ));
        }

        private void StopTimer()
        {
            if(_ct != null)
            {
                _ct.Cancel();
            }            
            File.WriteAllText(Path.Combine(_overlayBasePath, "timer.txt"), $"--:--");
        }

        private void SetGifFinalSetInOverlay(string player_winner_gif_destination, string player_loser_gif_destination)
        {
            File.Copy(
                Path.Combine(_overlayBasePath, @$"results\winner.gif"),
                Path.Combine(_overlayBasePath, player_winner_gif_destination)
                ,true);

            File.Copy(
                Path.Combine(_overlayBasePath, @$"results\ggs.gif"),
                Path.Combine(_overlayBasePath, player_loser_gif_destination)
                ,true
            );
        }

        private async Task UpdateFinalWinnerOverlay(ulong player_discord_id_winner)
        {
            string displayName = _displayName1;
            ulong player_discord_id_loser = _discordPlayer2;
            ulong player_winner_channel_id = _player1ChannelId;
            ulong player_loser_channel_id = _player2ChannelId;

            string player_winner_gif_destination = "player_1_result.gif";
            string player_loser_gif_destination = "player_2_result.gif";

            if(player_discord_id_winner == _discordPlayer2)
            {
                displayName = _displayName2;
                player_discord_id_loser = _discordPlayer1;
                player_winner_channel_id = _player2ChannelId;
                player_loser_channel_id = _player1ChannelId;

                player_winner_gif_destination = "player_2_result.gif";
                player_loser_gif_destination = "player_1_result.gif";
            }

            SetGifFinalSetInOverlay(player_winner_gif_destination, player_loser_gif_destination);
            
            await SendMessageToChannel(player_winner_channel_id, 
            string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString(
                "set_winner_message_for_winner"
            ), player_discord_id_winner, player_discord_id_loser));                
            
            await SendMessageToChannel(player_loser_channel_id, 
            string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString(
                "set_winner_message_for_loser"
            ), player_discord_id_loser, player_discord_id_winner));                

            UpdateOverlayTitle(
                string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("overlay_title_set_winner_at_end_of_set_message"), displayName)
            );
        }

        public async Task SetWinner(ulong discordIdWinner)
        {      
            try
            {
                StopTimer();                
                
                if(discordIdWinner == _discordPlayer1)
                {                
                    UpdateWinnerResultAndOverlay("vitoria", "derrota", _displayName1);                                
                } 
                else if(discordIdWinner == _discordPlayer2)
                {                
                    UpdateWinnerResultAndOverlay("derrota", "vitoria", _displayName2);                                                
                }                        

                var maxWins = Math.Ceiling((double)_totalRounds/2);                
                var vitoriasPlayer1 = _matches.Count(p => p.Player1Result == "vitoria");
                var vitoriasPlayer2 = _matches.Count(p => p.Player2Result == "vitoria");
                if(vitoriasPlayer1 == maxWins)
                {
                    await UpdateFinalWinnerOverlay(_discordPlayer1);                                                                            
                } 
                else if(vitoriasPlayer2 == maxWins)
                {
                    await UpdateFinalWinnerOverlay(_discordPlayer2);                    
                }

                TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;            
                var l = GenerateResultLine(GetStringInParentheses(_matches[_round - 1].Stage), 
                    textInfo.ToTitleCase(_matches[_round - 1].Player1Pick), textInfo.ToTitleCase(_matches[_round - 1].Player2Pick), discordIdWinner == _discordPlayer1
                );
                File.AppendAllText(Path.Combine(_overlayBasePath, "result_list.txt"), 
                    $"\n{l}"
                );                
                
                File.WriteAllText(Path.Combine(_overlayBasePath, "player_1_score.txt"), vitoriasPlayer1.ToString());
                File.WriteAllText(Path.Combine(_overlayBasePath, "player_2_score.txt"), vitoriasPlayer2.ToString());                            

                File.Delete(Path.Combine(_overlayBasePath, $"next_pick_player_1.png"));
                File.Delete(Path.Combine(_overlayBasePath, $"next_pick_player_2.png"));
                File.Delete(Path.Combine(_overlayBasePath, "player_1_loading.gif"));
                File.Delete(Path.Combine(_overlayBasePath, "player_2_loading.gif"));
                File.Delete(Path.Combine(_overlayBasePath, $"next_stage.png"));
                File.WriteAllText(Path.Combine(_overlayBasePath, $"next_stage.txt"), "");

                await SendMessageToChannel(_player1ChannelId, 
                string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("set_winner_result_message")
                    , _round, vitoriasPlayer1, _displayName1, vitoriasPlayer2, _displayName2  
                    ) 
                );

                await SendMessageToChannel(_player2ChannelId, 
                string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("set_winner_result_message")
                    , _round, vitoriasPlayer2, _displayName2, vitoriasPlayer1, _displayName1
                    ) 
                );            
            
            } catch(Exception ex ){
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }            
            
            _round++;            
            ResetMapList();
            File.Copy(
                    Path.Combine(_overlayBasePath, "seu_som.wav"),
                    Path.Combine(_overlayBasePath, @$"sounds_to_play\seu_som.wav"),
                    true
                );
        }

        private static string GetStringInParentheses(string input)
        {
            Regex regex = new Regex(@"\(([^)]*)\)");
            var match = regex.Match(input);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return "No string found within parentheses.";
            }
        }
 
        public async Task SetBlindPick(string pickPlayer, ulong? channelId)
        {
            if(channelId == _player1ChannelId)
            {
                _matches[_round - 1].Player1Pick = pickPlayer;                
            } else if(channelId == _player2ChannelId)
            {
                _matches[_round - 1].Player2Pick = pickPlayer;                
            }

            if (_matches[_round - 1].Player1Pick != null && 
                _matches[_round - 1].Player2Pick != null
            )
            {                
                await SendMessageToChannel(_player1ChannelId, Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_finished"));
                await SendMessageToChannel(_player2ChannelId, Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_finished"));
                SetNextPickInOverlay();  
                _ct.Cancel();
                await Task.Delay(500);                
                await ExecuteNextStep();
            }
        }        

        private void SetNextPickInOverlay()
        {
            File.Copy(
                Path.Combine(_overlayBasePath, @$"character_sources\{_matches[_round - 1].Player1Pick}.png"), 
                Path.Combine(_overlayBasePath, $"next_pick_player_1.png"), true
            );

            File.Copy(
                Path.Combine(_overlayBasePath, @$"character_sources\{_matches[_round - 1].Player2Pick}.png"), 
                Path.Combine(_overlayBasePath, $"next_pick_player_2.png"), true
            );
        }
        
        private void ResetMapList()
        {            
            MapList = JsonConvert.DeserializeObject<List<string>>(
                File.ReadAllText(".\\stages.json")
            );                    
        }
        public async Task PickStage(string stage)
        {                        
            _ct.Cancel();
            _matches[_round - 1].Stage = stage;
            
            await SendPicksRequest(
                _client.GetChannel(_player1ChannelId) as SocketTextChannel, _discordPlayer1, new List<string>() { stage }, _discordPlayer2, _round
            );
            await Task.Delay(100);
            await SendPicksRequest(
                _client.GetChannel(_player2ChannelId) as SocketTextChannel, _discordPlayer2, new List<string>() { stage }, _discordPlayer1, _round
            );                    
            
            StartTimer(2);

            File.Copy(Path.Combine(_overlayBasePath, @"results\loading.gif"), 
                Path.Combine(_overlayBasePath, "player_1_loading.gif"), true
            );
            File.Copy(Path.Combine(_overlayBasePath, @"results\loading.gif"), 
                Path.Combine(_overlayBasePath, "player_2_loading.gif"), true
            );

            await SetStageOverlay(stage);
            _matches[_round].Stage = stage;
            UpdateOverlayTitle(string.Format(
                Program.LANGUAGE_MANAGER.GetLocalizedString("overlay_stage_blind_pick_started_title"), _round
            ));
        }

        public async Task ExecuteNextStep()
        {
            var option = _setOptions.Dequeue();
                                        
            
            if(option.Type == "COIN_FLIP")
            {
                ResetMapList();                
                var random = new Random();
                int playerWinnerCoinFlip = random.Next(1, 3);
                
                if(playerWinnerCoinFlip == 1)
                {
                    await SendMessageToChannel(1173983788817207296, $"Coin flip winner <@{_discordPlayer1}>");
                    await StartBlindPick("player_1_loading.gif", "BAN", "PICK", _player1ChannelId, _player2ChannelId, _discordPlayer1, _displayName1, "coin_flip_winner_message", "coin_flip_loser_message", "overlay_title_map_ban_player_choosing_title");                    
                }
                else
                {   
                    await SendMessageToChannel(1173983788817207296, $"Coin flip winner <@{_discordPlayer2}>");
                    await StartBlindPick("player_2_loading.gif", "PICK", "BAN", _player2ChannelId, _player1ChannelId, _discordPlayer2, _displayName2, "coin_flip_winner_message", "coin_flip_loser_message", "overlay_title_map_ban_player_choosing_title");
                }
                return;
            }

            if(option.Type == "BLIND_PICK")
            {
                var player1WonLastMatch = _matches[option.Round - 2].Player1Result == "vitoria";
                if(player1WonLastMatch)
                {           
                    await StartBlindPick("player_1_loading.gif", "BAN", "PICK", _player1ChannelId, _player2ChannelId, _discordPlayer1, _displayName1);                    
                }
                else
                {                   
                    await StartBlindPick("player_2_loading.gif", "PICK", "BAN", _player2ChannelId, _player1ChannelId, _discordPlayer2, _displayName2);                    
                }
            }
            if(option.Type == "BUILD")
            {                
                StartTimer(3);                
                await Task.Delay(1000);
                SetBothPlayersLoadingInOverlay();                
                UpdateOverlayTitle($"{_bracket} - Match {_match} - Round {_round} - {Program.LANGUAGE_MANAGER.GetLocalizedString("overlay_title_build_customization")}");
                await SendBuildMessageToPlayers(_matches[_round - 1]);                
            }
        }

        private async Task StartBlindPick(string playerLoadGif, string player_a_action, string player_b_action, ulong player_a_channel_id, ulong player_b_channel_id, ulong player_a_id, string displayName,
            string pickMessage = "blind_pick_map_ban_player_message", 
            string waitMessage = "blind_pick_map_ban_player_wait_message", 
            string overlayTitleUpdate = "overlay_title_map_ban_player_choosing_title",        
            int timer = 2)
        {
            SetPlayerActions(player_a_action, player_b_action);
            await SendMessageToChannel(player_a_channel_id, string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString(
                pickMessage
            ), displayName));
            await SendMessageToChannel(player_b_channel_id, Program.LANGUAGE_MANAGER.GetLocalizedString(waitMessage));
            
            RemoveMapsFromListBasedOnWhoStartBan(displayName);

            await SendMapBanVoteMessageAsync(player_a_channel_id, player_a_id);
            StartTimer(timer);
            SetPlayerLoadingInOverlay(playerLoadGif);
            UpdateOverlayTitle(string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString(overlayTitleUpdate), displayName));
        }

        private void RemoveMapsFromListBasedOnWhoStartBan(string playerDisplayNameReference)
        {
            var winFilter = _displayName1 == playerDisplayNameReference ? "derrota" : "vitoria";            
            var winsMatches = _matches.Where(p => p.Player1Result == winFilter)
                .Select(p => p.Stage).ToArray();                
            MapList.RemoveAll(m => winsMatches.Contains(m));                
        }

        private void SetPlayerActions(string player1Action, string player2Action)
        {
            _matches[_round -1].Player1Action = player1Action;
            _matches[_round -1].Player2Action = player2Action;
        }        

        private async Task SendBuildMessageToPlayers(Match match)
        {            
            var channel1 = _guild.GetChannel(_player1ChannelId) as ISocketMessageChannel;
            var channel2 = _guild.GetChannel(_player2ChannelId) as ISocketMessageChannel;            

            //TODO: Remove blob storage dependency
            await SendDuelMatchEmbedAsync(channel1, match.Player1Pick, match.Player2Pick,
                $"https://ntarena.blob.core.windows.net/dissidia-012/{match.Player1Pick}.png",
                $"https://ntarena.blob.core.windows.net/dissidia-012/{match.Player2Pick}.png",
                match.Stage, _round, _discordPlayer1
            );

            await SendDuelMatchEmbedAsync(channel2, match.Player2Pick, match.Player1Pick,
                $"https://ntarena.blob.core.windows.net/dissidia-012/{match.Player2Pick}.png",
                $"https://ntarena.blob.core.windows.net/dissidia-012/{match.Player1Pick}.png",
                match.Stage, _round, _discordPlayer2
            );                                
        }

        private void SetPlayerLoadingInOverlay(string playerLoading)
        {
            File.Copy(Path.Combine(_overlayBasePath, @"results\loading.gif"), 
                Path.Combine(_overlayBasePath, playerLoading), true
            );
        }

        private void SetBothPlayersLoadingInOverlay()
        {
            File.Delete( Path.Combine(_overlayBasePath, "player_1_loading.gif"));
            File.Delete( Path.Combine(_overlayBasePath, "player_2_loading.gif"));

            File.Copy(Path.Combine(_overlayBasePath, @"results\loading.gif"), 
                Path.Combine(_overlayBasePath, "player_1_loading.gif"), true
            );
            File.Copy(Path.Combine(_overlayBasePath, @"results\loading.gif"), 
                Path.Combine(_overlayBasePath, "player_2_loading.gif"), true
            );
        }

        private async Task SetStageOverlay(string stage)
        {            
            try{
                File.Delete(Path.Combine(_overlayBasePath, $"next_stage.png"));
            } catch(Exception){}
            await Task.Delay(100);
            File.Copy(Path.Combine(_overlayBasePath, @$"stages_full_sources\{stage}.png"), 
                Path.Combine(_overlayBasePath, $"next_stage.png"), true
            );

            File.WriteAllText(Path.Combine(_overlayBasePath, "next_stage.txt"), stage);
        }

        public async Task StartNewMatch(ulong discordPlayer1, ulong discordPlayer2, int rounds, string match, string bracket, ulong host, ulong warningChannel)
        {            
            _warningChannel = warningChannel;            
            _totalRounds = rounds;
            _match = match;
            _bracket = bracket;
            _round = 1;
            _discordPlayer1 = discordPlayer1;
            _discordPlayer2 = discordPlayer2;
            
            await RemovePlayersFromChannel(_player1RoleId, _player1ChannelId);
            await RemovePlayersFromChannel(_player2RoleId, _player2ChannelId);

            await ApplyRoleInPlayer(discordPlayer1, _player1RoleId);
            await ApplyRoleInPlayer(discordPlayer2, _player2RoleId);


            GenerateSet(rounds);
            File.WriteAllText(Path.Combine(_overlayBasePath, "timer.txt"), $"--:--");
            var u1 = _guild.GetUser(discordPlayer1);
            var u2 = _guild.GetUser(discordPlayer2);
            _displayName1 = u1.DisplayName;
            _displayName2 = u2.DisplayName;
            File.WriteAllText(Path.Combine(_overlayBasePath, "result_list.txt"), string.Empty);
            File.WriteAllText(Path.Combine(_overlayBasePath, "player_1_name.txt"), u1.DisplayName);
            File.WriteAllText(Path.Combine(_overlayBasePath, "player_2_name.txt"), u2.DisplayName);
            UpdateOverlayTitle(Program.LANGUAGE_MANAGER.GetLocalizedString("overlay_title_start_match"));
            await StartMatchMessageAsync(host, _guild.GetChannel(_warningChannel) as ISocketMessageChannel);
            CleanOverlay();
        }

        public async Task StartMatchMessageAsync(ulong hostPlayerId, ISocketMessageChannel channel)
        {
            StopTimer();
            var builder = new EmbedBuilder
            {
                Title = string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("start_match_title_message"), _match) + " " + _bracket,
                Description = Program.LANGUAGE_MANAGER.GetLocalizedString("start_match_description_message"),
                Color = Color.Green,
            };

            builder.AddField("Host", MentionUser(hostPlayerId), inline: true);
            builder.AddField("Side A", MentionUser(_discordPlayer1), inline: true);
            builder.AddField("Side B", MentionUser(_discordPlayer2), inline: true);            
            
            var embed = builder.Build();
            await channel.SendMessageAsync($"{MentionUser(_discordPlayer1)},{MentionUser(_discordPlayer2)}", false, embed);
        }

        private string MentionUser(ulong userId)
        {
            return $"<@{userId}>";
        }
        
        private void UpdateOverlayTitle(string title)
        {
            File.WriteAllText(Path.Combine(_overlayBasePath, "titulo_partida.txt"), $"{title}");   

            File.Copy(
                    Path.Combine(_overlayBasePath, "seu_som.wav"),
                    Path.Combine(_overlayBasePath, @$"sounds_to_play\seu_som.wav"),
                    true
                );
        }

        public void CleanOverlay()
        {
            var images_files = new string[]
            {                                                             
                Path.Combine(_overlayBasePath, "next_stage.txt"),
                Path.Combine(_overlayBasePath, "next_stage.png"),
                Path.Combine(_overlayBasePath, "next_pick_player_1.png"),
                Path.Combine(_overlayBasePath, "next_pick_player_2.png"),
                Path.Combine(_overlayBasePath, "timer_loading.gif"),
                Path.Combine(_overlayBasePath, "player_2_round_3_vitoria.png"),
                Path.Combine(_overlayBasePath, "player_1_loading.gif"),
                Path.Combine(_overlayBasePath, "player_1_result.gif"),
                Path.Combine(_overlayBasePath, "player_2_result.gif"),
                Path.Combine(_overlayBasePath, "player_2_loading.gif"),                
            };

            foreach(var i_file in images_files)
            {
                File.Delete(i_file);                
            }            
            File.WriteAllText(Path.Combine(_overlayBasePath, "player_1_score.txt"), "0");
            File.WriteAllText(Path.Combine(_overlayBasePath, "player_2_score.txt"), "0");
        }
        
        private async Task ApplyRoleInPlayer(ulong playerDiscordId, ulong playerRole)
        {
            var player = _guild.GetUser(playerDiscordId);            
            await player.AddRoleAsync(playerRole);
        }

        private async Task RemovePlayersFromChannel(ulong roleId, ulong channelId)
        {            
            var role = _guild.GetRole(roleId);
            var channel = _guild.GetTextChannel(channelId);
            await RemoveRoleFromMembers(role, channel);
        }

        public async Task RemoveRoleFromMembers(IRole role, ITextChannel channel)
        {
            try{
                var members = await channel.GetUsersAsync().FlattenAsync();
                foreach (var member in members)
                {
                    var user = member as IGuildUser; // Cast to IGuildUser
                    if (user != null && user.RoleIds.Contains(role.Id))
                    {
                        await user.RemoveRoleAsync(role);
                    }
                }
            } catch(Exception)
            {

            }            
        }                
        
        public async Task StartTimer(int minutes)
        {
            if(_ct != null){                
                _ct.Cancel();                
                await Task.Delay(1000);
                File.WriteAllText(Path.Combine(_overlayBasePath, "timer.txt"), $"--:--");
            }
            _ct = new CancellationTokenSource();
            await Task.Factory.StartNew(async () => {
                try{
                    var timer = DateTime.Now.AddMinutes(minutes);                
                    File.Copy(Path.Combine(_overlayBasePath, @"results\loading.gif"), 
                        Path.Combine(_overlayBasePath, "timer_loading.gif"), true
                    );
                    while(DateTime.Now < timer)
                    {
                        _ct.Token.ThrowIfCancellationRequested();
                        await Task.Delay(500);
                        var reaminingTime = timer - DateTime.Now;

                        if(reaminingTime.TotalSeconds > 0)
                        {
                            if(reaminingTime.Seconds < 10)
                            {
                                File.WriteAllText(Path.Combine(_overlayBasePath, "timer.txt"), $"0{reaminingTime.Minutes}:0{reaminingTime.Seconds}");
                            }
                            else
                            {
                                File.WriteAllText(Path.Combine(_overlayBasePath, "timer.txt"), $"0{reaminingTime.Minutes}:{reaminingTime.Seconds}");
                            }
                            
                        }
                    }                    
                    try{
                        File.Delete(Path.Combine(_overlayBasePath, "player_1_loading.gif"));
                        File.Delete(Path.Combine(_overlayBasePath, "player_2_loading.gif"));
                        File.Delete(Path.Combine(_overlayBasePath, "timer_loading.gif"));
                    } catch(Exception){}
                    await SendMessageToChannel(_player1ChannelId, Program.LANGUAGE_MANAGER.GetLocalizedString("timer_is_over"));
                    await SendMessageToChannel(_player2ChannelId, Program.LANGUAGE_MANAGER.GetLocalizedString("timer_is_over"));
                }
                catch(Exception ex)
                {
                    File.WriteAllText(Path.Combine(_overlayBasePath, "timer.txt"), $"--:--");
                }
                finally
                {
                    try{
                        File.Delete(Path.Combine(_overlayBasePath, "player_1_loading.gif"));
                        File.Delete(Path.Combine(_overlayBasePath, "player_2_loading.gif"));
                        File.Delete(Path.Combine(_overlayBasePath, "timer_loading.gif"));
                    } catch(Exception){}
                    File.WriteAllText(Path.Combine(_overlayBasePath, "timer.txt"), $"--:--");
                }
                                               
                
            }, _ct.Token);            
        }

        private async Task SendMessageToChannel(ulong channelId, string message)
        {                        
            var channel = _guild.GetTextChannel(channelId);
            await channel.SendMessageAsync(message);
        }

        public async Task SendPicksRequest(SocketTextChannel channel, ulong playerId, List<string> maps, ulong opponentId, int round)
        {
            var pickEndTimeString = GenerateDateTimeString(2);
            
            var user = channel.GetUser(playerId);
            var embedBuilder = new EmbedBuilder
            {
                Title = string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_character_pick_request_title_message"), round),
                Description = string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_character_pick_request_description_message"), user.Mention, pickEndTimeString),
                Color = Color.Blue,
            };            
            
            string mapsList = string.Join("\n", maps);
            embedBuilder.AddField("Stage", mapsList);
                    
            var selectMenu = new SelectMenuBuilder()
                .WithCustomId("characterHeroesPick")
                .WithPlaceholder("🦸 Heroes")
                .WithOptions(
                    new List<SelectMenuOptionBuilder>()
                    {
                        new SelectMenuOptionBuilder("Warrior of Light", "wol"),
                        new SelectMenuOptionBuilder("Firion", "firion"),
                        new SelectMenuOptionBuilder("Onion Knight", "onion"),
                        new SelectMenuOptionBuilder("Cecil Harvey", "cecil"),
                        new SelectMenuOptionBuilder("Bartz Klauser", "bartz"),
                        new SelectMenuOptionBuilder("Terra Branford", "terra"),
                        new SelectMenuOptionBuilder("Cloud Strife", "cloud"),
                        new SelectMenuOptionBuilder("Squall Leonhart", "squall"),
                        new SelectMenuOptionBuilder("Zidane Tribal", "zidane"),
                        new SelectMenuOptionBuilder("Tidus", "tidus"),
                        new SelectMenuOptionBuilder("Shantotto", "shantotto"),
                        new SelectMenuOptionBuilder("Prishe", "prishe"),
                        new SelectMenuOptionBuilder("Lightining", "lightining"),
                        new SelectMenuOptionBuilder("Laguna", "laguna"),
                        new SelectMenuOptionBuilder("Vaan", "vaan"),
                        new SelectMenuOptionBuilder("Yuna", "yuna"),
                        new SelectMenuOptionBuilder("Kain", "kain"),
                        new SelectMenuOptionBuilder("Tifa", "tifa"),                        
                    }
                );
            
            
            var selectMenuVillains = new SelectMenuBuilder()
                .WithCustomId("characterVillainsPick")
                .WithPlaceholder("🦹 Villains")
                .WithOptions(
                    new List<SelectMenuOptionBuilder>()
                    {
                        new SelectMenuOptionBuilder("Garland", "garland"),
                        new SelectMenuOptionBuilder("The Emperor", "emperor"),
                        new SelectMenuOptionBuilder("Cloud of Darkness", "cod"),
                        new SelectMenuOptionBuilder("Golbez", "golbez"),
                        new SelectMenuOptionBuilder("Exdeath", "exdeath"),
                        new SelectMenuOptionBuilder("Kefka Palazzo", "kefka"),
                        new SelectMenuOptionBuilder("Sephiroth", "sephiroth"),
                        new SelectMenuOptionBuilder("Ultimecia", "ultimecia"),
                        new SelectMenuOptionBuilder("Kuja", "kuja"),
                        new SelectMenuOptionBuilder("Jecht", "jecht"),
                        new SelectMenuOptionBuilder("Gabranth", "gabranth"),
                        new SelectMenuOptionBuilder("Gilgamesh", "gilgamesh")

                    }
                );

            await channel.SendMessageAsync(user.Mention, false, embedBuilder.Build(),
                components: new ComponentBuilder()
                    .WithSelectMenu(selectMenu)                    
                    .WithSelectMenu(selectMenuVillains)
                    .Build()                    
                );

        }

        public async Task SendDuelMatchEmbedAsync(ISocketMessageChannel channel, string playerPick, string opponentPick, string playerImageUrl, string opponentImageUrl, string stage, int round, ulong playerId)
        {                        
            string pickEndTimeString = GenerateDateTimeString(3);

            var builder = new EmbedBuilder
            {
                Title = string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_send_customization_title_message"), round),
                Description = string.Format(Program.LANGUAGE_MANAGER.GetLocalizedString("blind_pick_send_customization_description_message"), pickEndTimeString),
                Color = new Color(0, 255, 0), // Cor verde
            };

            builder.AddField("Stage", $"{stage}", true);
            builder.AddField("Choice", playerPick, true);            
            builder.AddField("Opponent choice", opponentPick, false);
            
            var playerAvatarUrl = playerImageUrl; // URL da imagem do jogador
            var opponentAvatarUrl = opponentImageUrl; // URL da imagem do adversário
            
            builder.ThumbnailUrl = playerAvatarUrl; // Imagem do jogador
            builder.ImageUrl = opponentAvatarUrl; // Imagem do adversário

            var embed = builder.Build();
            await channel.SendMessageAsync(MentionUser(playerId), false, embed);
        }        
        private string GenerateResultLine(string map, string pick1, string pick2, bool winnerPlayer1)
        {        
            var builder = new StringBuilder();

            builder.Append(map);
            for(var i = map.Length; i < 6 ;i++)
            {
                builder.Append(' ');
            }
            var offset = 9;
            if(winnerPlayer1)
            {
                builder.Append("[V]");
                offset -= 3;
            }
            builder.Append(pick1);
            for(var i = pick1.Length; i < offset; i++)
            {
                builder.Append(' ');
            }
            
            builder.Append(" x ");

            if(!winnerPlayer1)
            {
                builder.Append("[V]");
            }
            builder.Append(pick2);
            for(var i = pick2.Length; i < offset; i++)
            {
                builder.Append(' ');
            }
            return builder.ToString();
        }        
       
    }

    
}