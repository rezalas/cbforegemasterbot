# CB Forgemaster Bot
A discord bot with integration support for multiple RCON supprted games.

### Author Paul McDowell


### Thanks

This project exists in great part due to the efforts of hard working men and women around the world creating and sharing their
efforts with all of us. I'd like to thank the team working on Discord.Net, CoreRCON, Serilog, and certainly not least the .Net Core
team and .Net Foundation. Their work powers this bot to a great degree and as such should be recognized.

### Servers

Servers are stored in appsettings.json which is created on first launch if it doesn't exist already. A default settings file is provided with the proper formatting and available fields. Default options are provided if you don't want to provide individual settings for each server when you create them. These defaults are loaded on save when a server is added to the list. *_HOWEVER_: Please note that passwords are NOT stored securely, and while a default password field is provided you should only be using it if your RCON is not externally accessible and the bot is hosted locally to protect both from external reads. If you need to host this externally, you need to change how this data is all stored and secure it properly.* The following server options are provided:

```json
{
  "DefaultRCONPwd": "",
  "DiscordToken": "",
  "AdminDiscordId": "",
  "ServerExternalAddressOverride": "",
  "Servers": [
    {
      "Name": "Demo Server",
      "ShortName": "DMO",
      "Address": "127.0.0.1",
      "QueryPort": 27015,
      "RConPort": 27016,
      "Password": "",
      "IsReachable": true,
      "GameName": "DemoGame",
      "ServerExternalAddress": ""
    }
  ]
}
```
