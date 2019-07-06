using System.Collections.Generic;

namespace CB.DiscordApps.ForgeMasterBot.Models
{
    public class Configuration
    {
        public string DefaultRCONPwd { get; set; }
        public List<GameServer> Servers { get; set; }
        public string DiscordToken { get; set; }

    }
}
