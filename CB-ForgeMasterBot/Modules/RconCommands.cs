using CB.DiscordApps.ForgeMasterBot.Models;
using Discord.Commands;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CB.DiscordApps.ForgeMasterBot.Modules
{
    public class RconCommands : ModuleBase<SocketCommandContext>
    {
        private ILogger _Logger;

        public RconCommands()
        {
            _Logger = Log.Logger;
        }

        [Command("Status-Update")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task StatusUpdate(string serverName = "")
        {
            if(string.IsNullOrWhiteSpace(serverName))
            {
                foreach(GameServer server in Settings.ServerList)
                {
                    try
                    {
                        server.IsReachable = await RCONConnector.UpdateStatus(server);
                        await Context.Channel.SendMessageAsync($"server {server.Name} status: " + (server.IsReachable ? "Alive" : "Unreachable"));
                    }
                    catch(Exception ex)
                    {
                        _Logger.Error("RconCommands.cs status-update error: {Exception}", ex);
                        await Context.Channel.SendMessageAsync($"I'm sorry, the servers are being bitchy and won't talk to me right now.");
                    }
                }
            }
            else
            {
                try
                {
                    GameServer server = EvaluateServerByString(serverName);
                    server.IsReachable = await RCONConnector.UpdateStatus(server);
                }
                catch(Exception ex)
                {
                    _Logger.Error("RconCommands.cs status-update error: {Exception}", ex);
                    await Context.Channel.SendMessageAsync("I'm a forge master not a mind reader! Ask me to List-Servers if you don't know the name!");
                }
            }
        }

        [Command("Server-Tell")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ServerTell(string serverName, string playerName, string message)
        {
            string msg = string.Empty;

            try
            {
                GameServer server = EvaluateServerByString(serverName);

                msg = await RCONConnector.SendCommand(server, $"ServerChatToPlayer \"{playerName}\" \"{message}\"");
            }
            catch (Exception ex)
            {
                msg = "I'm not talking to them at the moment.";
                _Logger.Error("RconCommands.cs server-tell error: {Exception}", ex);
            }
            //await Context.Channel.SendMessageAsync(msg);
        }

        [Command("Game-Announce")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task GameTell(string game, string message)
        {
            ConcurrentBag<string> messages = new ConcurrentBag<string>();
            string msg = "Result:\r\n";

            var gameservers = FindByGameName(game).ToList();

            List<Task> tasks = new List<Task>();
            gameservers.ForEach(server => tasks.Add(new Task(async () =>
            {
                try
                {
                    //await RCONConnector.SendCommand(server, $"broadcast \"{message}\"");
                    await RCONConnector.SendCommand(server, $"tribemessage 1366943132 \"{message}\"");
                    messages.Add($"{server.Name} received the message\r\n");
                }
                catch(Exception ex)
                {
                    messages.Add($"{server.Name} didn't like that.\r\n");
                    _Logger.Error($"[Error]->Game-Announce: {ex.ToString()}");
                }
            })));

            Parallel.ForEach(tasks, task => task.Start());
            await Task.WhenAll(tasks.ToArray());

            msg += string.Join("", messages.ToArray());

            await Context.Channel.SendMessageAsync(msg);
        }


        [Command("Server-Countdown")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ServerCountdown(string serverName, string TimeInSeconds, string command, string friendlyMsg = "")
        {
            GameServer server = EvaluateServerByString(serverName);
            if (server == null)
            {
                await Context.Channel.SendMessageAsync($"{serverName} isn't a server I know. Ask me to list-servers if you need help.");
                return;
            }
            
            int countdownSeconds = 0;
            if(int.TryParse(TimeInSeconds, out countdownSeconds))
            {
                try
                {
                    RCONConnector.ExecuteTimedCommand(server, command, countdownSeconds, friendlyMsg);
                }
                catch(Exception ex)
                {
                    _Logger.Error("RconCommand.cs error in ServerCountdown: {0}", ex);
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync($"I didn't understand '{TimeInSeconds}'. Remember: time in seconds (whole numbers please).");
            }
        }

        [Command("shutdown-game")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ShutdownGameServers(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName))
            {
                await Context.Channel.SendMessageAsync("You must tell me what game servers to close.");
                return;
            }

            if (Settings.ServerList.Any(x => x.GameName.Equals(gameName, StringComparison.InvariantCultureIgnoreCase))  == false)
            {
                await Context.Channel.SendMessageAsync($"I don't know what game '{gameName}' is.");
                return;
            }

            var servers = Settings.ServerList.Where(x => x.IsReachable && x.GameName.Equals(gameName, StringComparison.InvariantCultureIgnoreCase));

            if (servers.Count() == 0)
            {
                await Context.Channel.SendMessageAsync($"Found {servers.Count()} servers for '{gameName}', are you sure they're up?");
                return;
            }
            else
            {
                await Context.Channel.SendMessageAsync($"Found {servers.Count()} servers for '{gameName}', beginning shutdown procedures.");
            }

            foreach (var server in servers)
            {
                try
                {
                    RCONConnector.ExecuteTimedCommand(server, "saveworld", 0, "");
                }
                catch (Exception ex)
                {
                    _Logger.Error("RconCommand.cs error in ServerCountdown: {0}", ex);
                }
            }
            foreach (var server in servers)
            {
                try
                {
                    RCONConnector.ExecuteTimedCommand(server, "doexit", 0, "");
                }
                catch (Exception ex)
                {
                    _Logger.Error("RconCommand.cs error in ServerCountdown: {0}", ex);
                }
            }
        }

        [Command("Server-Players")]
        public async Task Players(string serverName, string playerName = "")
        {
            string msg = string.Empty;
            try
            {
                GameServer server = EvaluateServerByString(serverName);

                msg = await RCONConnector.SendCommand(server, $"listplayers");

                StringBuilder builder = new StringBuilder(); 
                var split = msg.Split("\r\n");
                foreach(string item in split)
                {
                    if(string.IsNullOrWhiteSpace(item)) { continue; }
                    builder.AppendLine(item.Split(',')[0]);
                }

                await Context.Channel.SendMessageAsync(builder.ToString());
            }
            catch (Exception ex)
            {
                _Logger.Error("RconCommands.cs ServerPlayers error: {Exception}", ex);

                msg = "I'm not talking to them at the moment.";
                await Context.Channel.SendMessageAsync(msg);
            }
        }

        [Command("Find-Player")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task FindPlayer(string playerName = "")
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                await Context.Channel.SendMessageAsync("No, if you want me to find someone you need to know their name.");
                return;
            }

            foreach(var server in Settings.ServerList)
            {
                try
                {
                    string result = await RCONConnector.SendCommand(server, $"listplayers");
                    await Context.Channel.SendMessageAsync($"server: {server.Name}\r\n{result}");
                }
                catch(Exception ex)
                {
                    _Logger.Error("RconCommands.cs FindPlayer error: {Exception}", ex);
                }
            }
        }

        [Command("All-Players")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task AllPlayers(string gameName = "")
        {
            List<Task<string>> tasks = new List<Task<string>>();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("```ml");
            foreach (var server in FindByGameName(gameName))
            {
                if(server.IsReachable == false)
                {
                    builder.AppendLine($"{server.Name} is offline, skipping...");
                    continue;
                }

                tasks.Add(GetServerPlayers(server));
            }

            //await Context.Channel.SendMessageAsync($"server: {server.Name}\r\n{result}");

            bool success = false; // canary boolean
            try
            {
                //Parallel.ForEach(tasks, task => task.Start());
                await Task.WhenAll(tasks);
                tasks.ForEach(x => builder.AppendLine(x.Result));
                success = true;
            }
            catch(Exception ex)
            {
                _Logger.Error("RconCommand.cs error in AllPlayers: ", ex);
            }
            finally
            {
                if(success == false)
                {
                    builder.AppendLine($"I spilled my beer during processing... @{Settings.AdminDiscordId} help!");
                }

                builder.AppendLine("```");
                await Context.Channel.SendMessageAsync(builder.ToString());
            }
        }  

        private IEnumerable<GameServer> FindByGameName(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName))
            {
                return Settings.ServerList;
            }

            return Settings.ServerList.FindAll(x => x.GameName.ToLowerInvariant().Contains(gameName.ToLowerInvariant()));
        }

        private async Task<string> GetServerPlayers(GameServer server)
        {
            string result = string.Empty;
            try
            {
                result = await RCONConnector.SendCommand(server, $"listplayers");
            }
            catch (Exception) { };

            if(string.IsNullOrWhiteSpace(result))
            {
                result = "Server did not respond or returned nothing.";
            }

            result = string.Join("\r\n\t", result.Trim().Split("\r\n").Where(x => string.IsNullOrWhiteSpace(x) == false).ToArray());
            return $"{server.Name}\r\n\t{result}";           
        }

        private GameServer EvaluateServerByString(string serverName)
        {
            int sentNumericId = 0;

            if (int.TryParse(serverName, out sentNumericId) && sentNumericId > 0 && sentNumericId <= Settings.ServerList.Count)
            {
                sentNumericId--;
                return Settings.ServerList[sentNumericId];
            }
            else if (serverName.Length == 3)
            {
                return Settings.ServerList.Find(x => x.ShortName.ToLower().Equals(serverName.ToLower()));
            }
            else
            {
                return Settings.ServerList.Find(x => x.Name.ToLower().Equals(serverName.ToLower()));
            }
        }
    }
}
