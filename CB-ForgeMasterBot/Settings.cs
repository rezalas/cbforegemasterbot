using CB.DiscordApps.ForgeMasterBot.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CB.DiscordApps.ForgeMasterBot
{
    public static class Settings
    {
        private const string _ConfigFileLoc = "./appsettings.json";

        private static Configuration _Configuration;
        public static string Token => _Configuration.DiscordToken;
        public static string AdminDiscordId => _Configuration.AdminDiscordId;

        public static List<GameServer> ServerList { get => _Configuration.Servers; }

        public static void LoadServerConfig()
        {
            try
            {
                using (StreamReader fileStream = File.OpenText(_ConfigFileLoc))
                {
                    _Configuration = JsonConvert.DeserializeObject<Configuration>(fileStream.ReadToEnd());
                }

            }
            catch (Exception ex)
            {
                var log = Log.Logger;
                log.Error($"Error loading configuration file: {ex.ToString()}");
  
            }
        }

        public static async Task<bool> AddServer(GameServer server)
        {
            if(string.IsNullOrWhiteSpace(server.Password))
            {
                server.Password = _Configuration.DefaultRCONPwd;
            }
            
            if(string.IsNullOrWhiteSpace(_Configuration.ServerExternalAddressOverride) == false)
            {
                server.ServerExternalAddress = _Configuration.ServerExternalAddressOverride;
            }

            if(await RCONConnector.UpdateStatus(server))
            {
                ServerList.Add(server);
                return SaveServerConfig();
            }

            return false;
        }

        public static bool SaveServerConfig()
        {
            try
            {
                string convertedServers = JsonConvert.SerializeObject(_Configuration);
                File.WriteAllText(_ConfigFileLoc, convertedServers);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
