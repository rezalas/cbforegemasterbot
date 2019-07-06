using CB.DiscordApps.ForgeMasterBot;
using CB.DiscordApps.ForgeMasterBot.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CB.DiscordApps.ForgeMasterBot
{
    class Program
    {
        private DiscordSocketClient _Client;
        private bool IsActive = false;
        private static ILogger _Logger;

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error)
                .WriteTo.Console()
                .CreateLogger();

            _Logger = Log.Logger;

            _Logger.Information("Firing up the forge...");
            
            try
            {
                Settings.LoadServerConfig();
                new Program().StartAsync().GetAwaiter().GetResult();
                _Logger.Information("Exiting...");
            }
            catch(Exception ex)
            {
                _Logger.Error($"Error leading to crash: {ex.ToString()}");
            }
        }

        public async Task<int> StartAsync()
        {
            using (var services = ConfigureServices())
            {
                _Client = services.GetRequiredService<DiscordSocketClient>();

                _Client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;
                RegisterEvents();

                _Logger.Information("Adding coals...");
                await _Client.LoginAsync(Discord.TokenType.Bot, Settings.Token);

                await _Client.StartAsync();

                _Logger.Information("Initiating server status update timer");
                var statusTimer = new System.Timers.Timer();
                statusTimer.Interval = 60000;
                statusTimer.Elapsed += TimedEvents;
                statusTimer.AutoReset = true;
                statusTimer.Start();

                _Logger.Information("Stoaking the fire...");
                services.GetRequiredService<CommandHandlerService>();

                UpdateGameServerStatus(); // kick off initial server status update

                _Logger.Information("Awaiting requests from the masses...");

                while (IsActive)
                {
                    await Task.Delay(1000);
                }
            }


            _Logger.Information("Well fuck. Good night!");

            return 0;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<CommandHandlerService>()
                .BuildServiceProvider();
        }

        private Task LogAsync(LogMessage log)
        {
            switch(log.Severity)
            {
                case LogSeverity.Debug:
                    _Logger.Debug(log.ToString());
                    break;
                case LogSeverity.Verbose:
                    _Logger.Verbose(log.ToString());
                    break;
                case LogSeverity.Info:
                    _Logger.Information(log.ToString());
                    break;
                case LogSeverity.Warning:
                    _Logger.Warning(log.ToString());
                    break;
                case LogSeverity.Error:
                    _Logger.Error(log.ToString());
                    break;
                case LogSeverity.Critical:
                default:
                    _Logger.Fatal(log.ToString());
                    break;
            }

            return Task.CompletedTask;
        }

        private async void TimedEvents(Object source, System.Timers.ElapsedEventArgs e)
        {
            await UpdateGameServerStatus();
        }

        private async Task UpdateGameServerStatus()
        {
            List<Task> statuses = new List<Task>();

            foreach (GameServer server in Settings.ServerList)
            {

                statuses.Add(new Task( async ()  => 
                {
                    try
                    {
                        server.IsReachable = await RCONConnector.UpdateStatus(server);
                    }
                    catch (Exception ex)
                    {
                        _Logger.Error(ex, "Program.cs Error: {Error}");
                    }
                }));
            }

            try
            {
                Parallel.ForEach(statuses, task => task.Start());
                await Task.WhenAll(statuses);
            }
            catch (Exception ex)
            {
                _Logger.Error("Program.cs Error while WaitAll tasks: {Exception}", ex);
            }
        }

        private void RegisterEvents()
        {
            _Client.LoggedIn += LoggedIn;
            _Client.LoggedOut += LoggedOut;
        }

        private async Task LoggedIn()
        {
            await Task.Run(() =>
            {
                IsActive = true;
            });
        }

        private async Task LoggedOut()
        {
            await Task.Run(() => 
            {
                IsActive = false;
            });
        }
    }
}
