using TASagentTwitchBot.Core.Web;

namespace TASagentTwitchBot.Plugin.Audio.Midi;

public static class MidiExtensions
{
    public static IMvcBuilder AddMidiAssembly(this IMvcBuilder builder) =>
        builder.AddApplicationPart(typeof(MidiExtensions).Assembly);

    public static IServiceCollection RegisterMidiServices(this IServiceCollection services) =>
        services.AddTASSingleton<MidiKeyboardHandler>()
            .AddTASSingleton<NAudioMidiDeviceManager>();
}
