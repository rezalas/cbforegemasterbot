using CB.DiscordApps.ForgeMasterBot.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CB.DiscordApps.ForgeMasterBot
{
    public static class RCONConnector
    {
        private static ILogger _Logger = Log.Logger;

        public static async Task<bool> UpdateStatus(GameServer server)
        {
            bool result = false;

            try
            { 
                await Task.Run(() =>
                {
                    try
                    {
                        
                        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            socket.Connect(new IPEndPoint(IPAddress.Parse(server.Address), server.RConPort));
                            result = socket.Connected;

                            if (server.IsReachable == false)
                            {
                                server.IsReachable = true;
                                _Logger.Information($"Server {server.Name} is available once more.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.ToString());
                        _Logger.Information($"Server {server.Name} is unreachable. Aborting connection.");
                        _Logger.Error(ex,"UpdateStatus error {Error}");
                    }
                });
            }
            catch(Exception exception)
            {
                _Logger.Warning($"Update status check aborted for server {server.Name} due to exception: {exception.ToString()}");
            }

            return result;
        }

        public static async Task<string> SendCommand(GameServer server, string Command)
        {
            try
            {
                string result;
                if(server.RConConnector == null)
                {
                    server.RConConnector = new CoreRCON.RCON(IPAddress.Parse(server.Address), server.RConPort, server.Password);

                }
                result = await server.RConConnector.SendCommandAsync(Command);
                return result;
            }
            catch(Exception ex)
            {
                _Logger.Error(ex.ToString());
            }

            
            return string.Empty;
        }

        public static async void ExecuteTimedCommand(GameServer server, string command, int countdownSeconds, string friendlyMsg)
        {
            try
            {
                var rconExecutor = new CoreRCON.RCON(IPAddress.Parse(server.Address), server.RConPort, server.Password);
                while (countdownSeconds > 0)
                {
                    int timeInMinutes = countdownSeconds >= 60 ? (countdownSeconds / 60) : 0;                    
                    string secondsMsg = countdownSeconds > 60 ? $"{timeInMinutes} Minute(s)" : $"{countdownSeconds} seconds";
                    if (string.IsNullOrWhiteSpace(friendlyMsg))
                    {

                        await rconExecutor.SendCommandAsync($"broadcast Countdown {secondsMsg}");
                    }
                    else
                    {
                        await rconExecutor.SendCommandAsync($"broadcast Countdown {secondsMsg}: {friendlyMsg} ");
                    }


                    if (timeInMinutes >= 2)
                    {
                        if (timeInMinutes > 5)
                        {
                            countdownSeconds -= 300;
                            await Task.Delay(300000);
                        }
                        else
                        {
                            countdownSeconds -= 60;
                            await Task.Delay(60000);
                        }
                    }
                    else if (countdownSeconds > 60)
                    {
                        int burnSeconds = (countdownSeconds - 60);
                        countdownSeconds -= burnSeconds;
                        await Task.Delay(burnSeconds * 1000);
                    }
                    else if (countdownSeconds == 60)
                    {
                        int burnSeconds = 30;
                        countdownSeconds -= burnSeconds;
                        await Task.Delay(burnSeconds * 1000);
                    }
                    else if (countdownSeconds == 30)
                    {
                        int burnSeconds = 15;
                        countdownSeconds -= burnSeconds;
                        await Task.Delay(burnSeconds * 1000);
                    }
                    else
                    {
                        int burnSeconds = 1;
                        countdownSeconds -= burnSeconds;
                        await Task.Delay(burnSeconds * 1000);
                    }
                }

                // time is up! send the command. 
                await rconExecutor.SendCommandAsync($"broadcast TIME IS UP!");
                await Task.Delay(1000);
                await rconExecutor.SendCommandAsync($"{command}");
                rconExecutor.Dispose();
            }
            catch (Exception ex)
            {
                _Logger.Error($"SHIT, countdown error: {ex.ToString()}");
            }
        }
    }
}
