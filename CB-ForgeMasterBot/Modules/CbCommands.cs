using CB.DiscordApps.ForgeMasterBot.Models;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CB.DiscordApps.ForgeMasterBot.Modules
{
    public class CbCommands : ModuleBase<SocketCommandContext>
    {
        Dictionary<string, string> CommandDescriptions = new Dictionary<string, string>
        {
            {"help", $"Help <optional: command> - I tell you this list, or just about the command you ask about." },
            {"list-servers", "list-servers <optional:game name> - I tell you all server names (optionally filtered by game) and their steam link."},
            { "server-players", "server-players <servername> - I peek at the player tab for that server and tell you what I see."},
            {"status", "Status <servername> - I share individual server status."},
            {"status-all", "status-all <optional: game name> - Because I'm nice I provide the server status for all servers, optionally filtered by game."},
            {"list-games", "list-games - Provide a list of all games we currently host." }
        };

        Dictionary<string, string> AdminCommandDesc = new Dictionary<string, string>
        {
            {"adminhelp", $"adminHelp <optional: command> - I tell you this list, or just about the command you ask about." },
            {"snuffitout", "snuffitout - I take my balls and go home." },
            {"add-server", "add-server <serverName> <initials> <game name> <queryPort> <rconPort> - Adds the server to the server list and updates the config." },
            {"reload-servers", "reload-servers - I'll go through the list of servers from my book of servers." },
            {"status-update", "status-update <optional: name> - Updates the status of a server (or all servers)." },
            {"server-tell", "server-tell <servername> <playername> \"<msg>\" - I whisper sweet somethings in the target's ears. Somethings need not be sweet." },
            {"find-player", "find-player <name> - I stalk my target and return all information available on them." },
            {"all-players", "all-players - I tell you about all the players on every server." },
            { "server-countdown", "server-countdown <servername> <seconds> \"<command>\" <optional: \"friendly warning\"> - I Send the command after X seconds. If you" +
                " provide a friendly message, I pass it along at each iteration of my countdown." },
            { "shutdown-game", "shutdown-game <gameName> - I reach out to the servers for that game and attempt to cleanly shut down all of them."}
        };

        [Command("SnuffItOut")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task SnuffItOut()
        {
            await Context.Message.Channel.SendMessageAsync("The fires are low, I'm not long for this night!");

            //var dmChan = await Context.User.GetOrCreateDMChannelAsync();
            //await dmChan.SendMessageAsync("Test! AHAH!");

            await Task.Run(() => 
            {
                Context.Client.LogoutAsync();
            });
        }

        [Command("reload-servers")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task ReloadServers()
        {
            await Context.Channel.SendMessageAsync("Let me look through my books...");

            await Task.Run(() =>
            {
                Settings.LoadServerConfig();
            });

            await Context.Channel.SendMessageAsync("I've read about the following places:");
            await ListServers();
        }

        [Command("add-server")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task AddServer(string name, string shortName, string gameName, ushort queryPort, ushort rconPort)
        {
            name = name.Trim();
            shortName = shortName.Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName) || queryPort < 0 || rconPort < 0)
            {
                await Context.Channel.SendMessageAsync("That doesn't sound right. I can't write about that place.");
                return;
            }

            if (Settings.ServerList.Any(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()))
            {
                await Context.Channel.SendMessageAsync("There's already a place named that!");
                return;
            }

            if (Settings.ServerList.Any(x => x.ShortName.ToUpper() == shortName.ToUpper()))
            {
                await Context.Channel.SendMessageAsync("There's already a place with those initials!");
                return;
            }

            if(string.IsNullOrWhiteSpace(gameName))
            {
                await Context.Channel.SendMessageAsync("You must include a valid game name: e.g. \"Ark\", \"Atlas\"");
                return;
            }

            if (Settings.ServerList.Any(x => x.QueryPort == queryPort))
            {
                await Context.Channel.SendMessageAsync("There's already a place with that address!");
                return;
            }

            if (Settings.ServerList.Any(x => x.RConPort == rconPort))
            {
                await Context.Channel.SendMessageAsync("That's the same phone number as another place!");
                return;
            }

            GameServer newServer = new GameServer
            {
                Name = name,
                ShortName = shortName.ToUpper(),
                Address = Settings.ServerList.First().Address,
                QueryPort = queryPort,
                RConPort = rconPort
            };

            await Context.Channel.SendMessageAsync("I'll see if I can visit there..");
            if (await Settings.AddServer(newServer))
            {
                await Context.Channel.SendMessageAsync("Sure I'll write that down.");
                Settings.SaveServerConfig();
            }
            else
            {
                await Context.Channel.SendMessageAsync("Are you sure I can reach it? I can't write about places I can't reach.");
            }
        }

        [Command("list-games")]
        public async Task ListGames()
        {
            var games = Settings.ServerList.Select(x => x.GameName.ToLowerInvariant()).Distinct();
            StringBuilder builder = new StringBuilder();
            builder.Append("```");
            builder.AppendLine("Game Name");
            builder.AppendLine("----------");
            foreach(var game in games)
            {
                builder.AppendLine($"{game}");
            }
            builder.Append("```");

            await Context.Channel.SendMessageAsync(builder.ToString());
        }

        [Command("Status")]
        public async Task Status(string ServerName)
        {
            GameServer server = EvaluateServerByString(ServerName);
            if (server == null)
            {
                await Context.Channel.SendMessageAsync($"{ServerName} isn't a server I know. Ask me to list-servers if you need help.");
                return;
            }

            string msg = $"{server.Name.PadRight(14)}\t - {(server.IsReachable ? "Alive" : "Unreachable")}.";

            await Context.Channel.SendMessageAsync(msg);
        }

        [Command("status-all")]
        public async Task StatusAll(string gameName = "")
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("```");

                var servers = string.IsNullOrWhiteSpace(gameName) ? Settings.ServerList : Settings.ServerList.Where(x => string.Equals(x.GameName.Trim(), gameName.Trim(), StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrWhiteSpace(gameName) == false && servers.Count() == 0)
                {
                    await Context.Channel.SendMessageAsync("Sorry, I couldn't find a game by that name. Try list-games first?");
                    return;
                }

                foreach (var server in servers)
                {
                    builder.AppendLine($"{server.Name} is currently {(server.IsReachable ? "Alive" : "Unreachable")}.");
                }
                builder.Append("```");

                await Context.Channel.SendMessageAsync(builder.ToString());
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("I'm a forge master not a mind reader! Ask me to List-Servers if you don't know the name!");
            }
        }

        [Command("Help")]
        public async Task Help(string Command = "")
        {
            StringBuilder builder = new StringBuilder();
            
            if(string.IsNullOrWhiteSpace(Command))
            {
                foreach(string item in  CommandDescriptions.Values)
                {
                    builder.AppendLine(item);
                }

            }
            else if(CommandDescriptions.ContainsKey(Command.ToLower()))
            {
                builder.AppendLine(CommandDescriptions[Command.ToLower()]);
            }
            else
            {
                builder.Append($"{Context.User.Username} slow down, I can't understand ya! If you say: ");
                foreach (string item in CommandDescriptions.Values)
                {
                    builder.AppendLine(item);
                }
            }
            await Context.Channel.SendMessageAsync(builder.ToString());
        }

        [Command("adminhelp")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task AdminHelp(string Command = "")
        {
            StringBuilder builder = new StringBuilder();

            if (string.IsNullOrWhiteSpace(Command))
            {
                foreach (string item in AdminCommandDesc.Values)
                {
                    builder.AppendLine(item);
                }

            }
            else if (AdminCommandDesc.ContainsKey(Command.ToLower()))
            {
                builder.AppendLine(AdminCommandDesc[Command.ToLower()]);
            }
            else
            {
                builder.Append($"{Context.User.Username} slow down, I can't understand ya! If you say: ");
                foreach (string item in AdminCommandDesc.Values)
                {
                    builder.AppendLine(item);
                }
            }
            await Context.Channel.SendMessageAsync(builder.ToString());
        }

        [Command("List-Servers")]
        public async Task ListServers(string gameName = "")
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("```"); // code block formatter for discord
            int count = 1;

            var servers = string.IsNullOrWhiteSpace(gameName) ? Settings.ServerList : Settings.ServerList.Where(x => string.Equals(x.GameName.Trim(), gameName.Trim(), StringComparison.InvariantCultureIgnoreCase));

            if(string.IsNullOrWhiteSpace(gameName) == false && servers.Count() == 0)
            {
                await Context.Channel.SendMessageAsync("Sorry, I couldn't find a game by that name. Try list-games first?");
                return;
            }
            foreach (var server in servers)
            {
                builder.AppendLine($"{count}\t| {server.ShortName} |\t{server.Name} - {server.GetPublicLink()}");
                count++;
            }
            builder.Append("```"); // code block terminator for discord

            await Context.Channel.SendMessageAsync(builder.ToString());
        }

        [Command("fuck")]
        public async Task HandleJerks(string target)
        {
            string msg = "";

            string targ = target.ToLower();
            if(targ.StartsWith("you") || targ.Equals("u"))
            {
                msg = $"{Context.User.Username} I don't take shit from compy kibble!";
                await Context.Channel.SendMessageAsync(msg);
                // in the future, kick them.
            }
            else if(targ.StartsWith("me") || targ.Equals("me"))
            {
                msg = "I'm sure you're nice but I prefer digital.";
                await Context.Channel.SendMessageAsync(msg);
            }
            else
            {
                msg = "Swear all you want but I don't age and you're a meat sack.";
                await Context.Channel.SendMessageAsync(msg);
            }
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
