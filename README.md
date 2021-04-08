# TASagent Twitch Bot

My extensible, modular C# twitch bot development framework.

## How do I use this?

At the moment, if you aren't comfortable getting your hands dirty with the source or at least json config files, this may not be the project for you.

But once configured, the project should be very simple to use.  It has a number of modular features, and it should be fast and easy to develop new ones.

## Getting Started

To start with, get the appropriate [NetCore 5 SDK (With AspNet)](https://dotnet.microsoft.com/download/dotnet/5.0)

If you're using this as a twitch bot, you're going to need to make a new Twitch account for that bot.  You also need to go to [The Twitch Dev Console](https://dev.twitch.tv/console/apps) and register an application to receive a ClientID.  Enter any name, use `http://localhost:9000/` as the OAuth Redirect URL, and choose "Chat Bot" as the category.

You may need to forward ports `9000` and `9005` to the computer you intend to use to get everything to work.  Port `9000` is used for OAuth callbacks, and port `9005` is used for follower notifications.  To enable listening on these ports, you'll likely need to run the following commands in powershell in administrator mode:

```powershell
netsh http add urlacl url="http://+:9000/" user=everyone
netsh http add urlacl url="http://+:9005/" user=everyone
```

Compile and run the `BotConfigurator` program to begin setup.  You'll be prompted for several values, and it will prepare some configuration files in your `Documents/TASagentBotDemo` directory.  

### TTS

If you wish to use TTS, you'll have to create a GoogleCloud account with TTS enabled and put the credentials in `TASagentBotDemo/Config/googleCloudCredentials.json` and an AWSPolly account with its credentials stored `TASagentBotDemo/Config/awsPollyCredentials.json`.  Otherwise, you'll have to use a configuration of the bot that doesn't support TTS.  Read and understand the pricing schemes of each service.  It's unlikely one streamer would be able to use enough TTS in a single month to result leave the free tier of either service, but it's your responsibility to understand how the pricing works.

Example `googleCloudCredentials.json` file:
```json
{
  "type": "service_account",
  "project_id": "TheProjectsName",
  "private_key_id": "LONGPRIVATEID",
  "private_key": "-----BEGIN PRIVATE KEY-----\nLOTS OF PRIVATE STUFF HERE\n-----END PRIVATE KEY-----\n",
  "client_email": "examplettssystem@theprojectsname.iam.gserviceaccount.com",
  "client_id": "CLIENTID",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/examplettssystem%theprojectsname.iam.gserviceaccount.com"
}
```

Example `awsPollyCredentials.json` file:
```json
{
    "AccessKey": "AN ACCESS KEY HERE",
    "SecretKey": "A SECRET KEY HERE"
}
```

### Database

Navigate to the directory of the Bot project (where the `.proj` file lives) and create the initial database with `dotnet ef database update`.

## Running and controlling the bot

If everything has been set up properly, you should be able to launch the bot and have it connect to Chat.

Open a web browser to http://localhost:5000/API/ControlPage.html to see the control page, and enter the admin password.

Voila!

## Customizing the bot

There are several major subsystems to help streamline customization, and it's simple to create new ones.

### Commands

New commands just need to extend the `ICommandSystem` interface, and get registered to that interface in the `Startup` class.

### Channel Point Redemptions

### Notifications