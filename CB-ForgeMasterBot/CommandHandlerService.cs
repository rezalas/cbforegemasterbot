using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CB.DiscordApps.ForgeMasterBot
{
    public class CommandHandlerService
    {
        private DiscordSocketClient _Client;
        private CommandService _CommandService;
        private IServiceProvider _Services;
        
        public CommandHandlerService(IServiceProvider services)
        {
            _Client = services.GetRequiredService<DiscordSocketClient>();
            _CommandService = services.GetRequiredService<CommandService>();
            _Services = services;

            RegisterEvents();
            _CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), _Services);
        }


        private void RegisterEvents()
        {
            _Client.MessageReceived += ProcessMessage;  
        }

        private async Task ProcessMessage(SocketMessage socketMessage)
        {
            
            if (!(socketMessage is SocketUserMessage userMsg))
            {
                return;
            }

            bool isDM = userMsg.Channel.GetType() == typeof(SocketDMChannel);
            if (userMsg.MentionedUsers == null && (userMsg.Author.IsBot || !isDM))    
            {
                return;
            }

            int ArgumentPos = 0; 

            if (isDM || userMsg.HasMentionPrefix(_Client.CurrentUser, ref ArgumentPos))
            {
                if (!isDM && !(userMsg.Author is SocketGuildUser _))
                { return; }

                var context = new SocketCommandContext(_Client, userMsg);
                
                
                var result = await _CommandService.ExecuteAsync(context, ArgumentPos, _Services);
                if(result.IsSuccess == false && result.Error != CommandError.UnknownCommand)
                {
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}
