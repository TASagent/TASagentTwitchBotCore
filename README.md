# TASagent Stream Bot

My extensible, modular C# stream bot development framework (for integration with Twitch) .

## How do I use this?

At the moment, if you aren't comfortable getting your hands dirty with the source or at least json config files, this may not be the project for you.

But once configured, the project should be very simple to use. It has a number of modular features, and it should be fast and easy to develop new ones.

![Control Page](assets/ControlPage.png)

I've also developed [a StreamDeck plugin](https://github.com/TASagent/TASagentTwitchBot.StreamDeckPlugin) for interacting with the REST endpoints hosted by the bot.

## Getting Started

To start with, get the appropriate [NetCore 6 SDK (With AspNet)](https://dotnet.microsoft.com/download/dotnet/6.0). It will also be useful to install the .NET Entity Framework CLI tools by running:

```cmd
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

Clone the [TASagentTwitchBotDemos project](https://github.com/TASagent/TASagentTwitchBotDemos) and initialize and update the git submodule (this project):

```cmd
git clone https://github.com/TASagent/TASagentTwitchBotDemos
cd TASagentTwitchBotDemos
git submodule init
git submodule update
```

If you're using this as a twitch bot, you're going to need to make a new Twitch account for that bot. You also need to go to [The Twitch Dev Console](https://dev.twitch.tv/console/apps) and register an application to receive a ClientID. Enter any name, use `http://localhost:5000/TASagentBotAPI/OAuth/BotCode` and `http://localhost:5000/TASagentBotAPI/OAuth/BroadcasterCode` as the OAuth Redirect URLs, and choose "Chat Bot" as the category.

If the project you're building uses the database, navigate to the project directory via command line. If the migration files already exist for it (and are up-to-date), then just run `dotnet ef database update`. If they do not exist yet, you'll have to create them first with `dotnet ef migrations add InitialDBCreation`, followed by `dotnet ef database update`.  

The first time you run a demo program, you'll be prompted for several values, and it will prepare some configuration files in your `Documents/TASagentBotDemo` directory.  

### TTS

There are now two methods of using TTS.

#### The Easy Way - My Webserver

The first is to head to [My Webserver](https://server.tas.wtf/) and register a new account using the Twitch OAuth option (make sure to log in as the Broadcaster). Then contact me at [TASagent.Streams@gmail.com](mailto:TASagent.Streams@gmail.com) and give me your Twitch username and ask me to authorize you to connect and increase your TTS Character Limit. Be sure to include whether you'd like to use the Neural voices, but understand that the Neural TTS Voices consume 4x the character limit (because of the increased cost). You'll be given some values to insert into `TASagentBotDemo/Config/ServerConfig.json` in order to be able to connect.

#### The Hard Way - Self-hosing

I have made sure to keep _The Hard Way_ available because I don't want to keep a stranglehold on how the bot is used. If you wish to use TTS the hard way, you'll have to create a GoogleCloud account with TTS enabled and put the credentials in `TASagentBotDemo/Config/googleCloudCredentials.json`, an AWSPolly account with its credentials stored `TASagentBotDemo/Config/awsPollyCredentials.json`, and/or a Microsoft Azure account with its credentials stored in `TASagentBotDemo/Config/azureSpeechSynthesisCredentials.json`. Otherwise, you'll have to use a configuration of the bot that doesn't support TTS. Read and understand the pricing schemes of each service. It's unlikely one streamer would be able to use enough TTS in a single month to result leave the free tier of any service (while it lasts), but it's your responsibility to understand how the pricing works.

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

Example `azureSpeechSynthesisCredentials.json` file:
```json
{
    "AccessKey": "AN ACCESS KEY HERE",
    "Region": "westus2"
}
```

Depending on which Demo you use, you may need to change the `ITTSRenderer` provider to the Local one in the `Startup` class like so:
```csharp
services.UnregisterImplementation<Core.TTS.TTSWebRenderer>()
    .AddSingleton<Core.TTS.ITTSRenderer, Core.TTS.TTSRenderer>();
```

### Database

Navigate to the directory of the Bot project (where the `.proj` file lives) and create the initial database with `dotnet ef database update`.

To apply model changes as a new database migration, navigate to the directory of the project and use `dotnet ef migrations add <MigrationName>` where `<MigrationName>` is what you'd like to name this model update, for example, `dotnet ef migrations add AddingNewUserField`. Then use `dotnet ef database update` to apply the model updates to the database.

## Running and controlling the bot

If everything has been set up properly, you should be able to launch the bot and have it connect to Chat.

Open a web browser to http://localhost:5000/API/ControlPage.html to see the control page, and enter the admin password.

Voila!

## Customizing the bot

There are several major subsystems to help streamline customization, and it's simple to create new ones. A lot of the action takes place in the in the [Dependency Injection system](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection) that's part of ASP.NET. Classes are registered in `Program.cs`, and when an instance of a class is required, it is instantiated (along with all of its constructor pre-requisites). As a result, no _cycles_ are allowed (where Class A and Class B both require one-another).

When multiple classes are registered for an interface or class, only the last one to be registered will be instantiated.

If you wish to register a single class to handle multiple interfaces, be careful how you register it.

This block would instantiate only one instance of `Notifications.FullActivityProvider` and provide it for every listed interface:

```csharp
builder.Services
    .AddSingleton<Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.ISubscriptionHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.ICheerHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.IRaidHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.IGiftSubHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.IFollowerHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>())
    .AddSingleton<Notifications.ITTSHandler>(x => x.GetRequiredService<Notifications.FullActivityProvider>());
```

Whereas this block would instantiate an independent copy of the class as the handler for each interface:

```csharp
builder.Services
    .AddSingleton<Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.ISubscriptionHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.ICheerHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.IRaidHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.IGiftSubHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.IFollowerHandler, Notifications.FullActivityProvider>()
    .AddSingleton<Notifications.ITTSHandler, Notifications.FullActivityProvider>();
```

However, when a class accepts an `IEnumerable` as a constructor argument, it receives an instance of every class that's been registered as a handler. For example, the CommandSystem has the following constructor:

```csharp
public CommandSystem(
    ICommunication communication,
    ErrorHandler errorHandler,
    IEnumerable<ICommandContainer> commandContainers)
```

So it recieves and instance of every class registered as an `ICommandContainer`:

```csharp
builder.Services
    .AddSingleton<Commands.ICommandContainer, Commands.CustomSimpleCommands>()
    .AddSingleton<Commands.ICommandContainer, Commands.SystemCommandSystem>()
    .AddSingleton<Commands.ICommandContainer, Commands.PermissionSystem>()
    .AddSingleton<Commands.ICommandContainer, Commands.ShoutOutSystem>()
    .AddSingleton<Commands.ICommandContainer, Commands.NotificationSystem>()
    .AddSingleton<Commands.ICommandContainer, Quotes.QuoteSystem>()
    .AddSingleton<Commands.ICommandContainer, TTS.TTSSystem>()
    .AddSingleton<Commands.ICommandContainer, Commands.TestCommandSystem>();
```

However, classes are (deliberately) not automatically registered as the handler for all of their interfaces.  If you created a class called `ExampleClass` that implements `Commands.ICommandContainer`, and in the `Startup` class you called `services.AddSingleton<ExampleClass>()`, then `ExampleClass` would _not_ be passed to the `CommandSystem`.

### Commands

New commands just need to extend the `ICommandSystem` interface, and get registered to that interface in the `Startup` class.

### Scripting

A Scripting subsystem is utilized by several modules, like the `ScriptedActivityProvider` and `ScriptedCommands`. The scripts can be edited on the ControlPage.

Each script has a predefined entry-point. You can best conceptualize each script as living inside of an implicit anonymous class, with syntax similar to C#. Other methods and script-instanced variables can be defined.

A variable or container defined outside of a function is persistent across all invocations of the script, but is scoped to only the script.  If the variable is declared with the `global` keyword, then it can be accessed in _all_ scripts that declare it with the `global` keyword, and only the assignment in the first declaration to run is executed.  So a dozen scripts that all say `global int exampleInt = -1;` will all access the same `exampleInt`, but it will only be set to `-1` the first time one is run.

#### Supported Containers

For the time being, these are the supported containers: `List<T>`, `Queue<T>`, `Stack<T>`, `RingBuffer<T>`, `DepletableList<T>`, `DepletableBag<T>`, `Dictionary<TKey,TValue>`, `HashSet<T>`.

New classes can be registered for access in the scripting language with the `ClassRegistrar`, see the `ScriptedActivityProvider` for examples.

### Channel Point Redemptions

### Notifications

In OBS, add the following BrowserSources

`http://localhost:5000/BrowserSource/overlay.html` - Image notifications (example size: 800px wide x 600px tall)  
`http://localhost:5000/BrowserSource/controllerSpy.html` - SNES NintendoSpy Overlay (example size: 400px wide x 100px tall)  
`http://localhost:5000/BrowserSource/timer.html` - Timer overlay (example size: 450px wide x 150px tall)  
`http://localhost:5000/BrowserSource/ttsmarquee.html` - Scrolling TTS Marquee (example size: 1920px wide x 60px tall)  
`http://localhost:5000/BrowserSource/emoteRain.html` - Raining Emote effect (example size: 1920px wide x 1080px tall)
