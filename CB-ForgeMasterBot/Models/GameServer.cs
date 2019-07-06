using CoreRCON;
using Newtonsoft.Json;

namespace CB.DiscordApps.ForgeMasterBot.Models
{
    public class GameServer
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Address { get; set; }
        public ushort QueryPort { get; set; }
        public ushort RConPort { get; set; }
        public string Password { get; set; }

        public bool IsReachable { get; set; }

        public string GameName { get; set; } = string.Empty;

        /// <summary>
        /// When provided overrides the listed connection address when providing steam links.
        /// Best used when your bot is behind the same firewall as your server.
        /// </summary>
        public string ServerExternalAddress { get; set; }

        [JsonIgnore]
        public RCON RConConnector { get; set; }

        public GameServer()
        {
            Name = string.Empty;
            Address = string.Empty;
            QueryPort = 27015;
            RConPort = 26015;
        }

        public GameServer(string Name, string Address)
        {
            this.Name = Name;
            this.Address = Address;
            QueryPort = 27015;
            RConPort = 26015;
        }

        public GameServer(string Name, string ShortName, string Address, ushort RConPort, string Password) : this(Name, Address)
        {
            this.RConPort = RConPort;
            this.Password = Password;
            this.ShortName = ShortName;
        }
        public GameServer(string Name, string ShortName, string Address, ushort RCONPort, ushort QueryPort, string Password) : this(Name, ShortName, Address, RCONPort, Password)
        {
            this.QueryPort = QueryPort;
        }

        public override string ToString() => Name;

        public override int GetHashCode() => Name.GetHashCode();
        

        public string GetPublicLink()
        {
            return $"steam://connect/{ServerExternalAddress ?? Address }:{QueryPort}";
        }
    }
}
