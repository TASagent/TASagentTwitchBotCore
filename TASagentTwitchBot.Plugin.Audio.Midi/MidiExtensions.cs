namespace TASagentTwitchBot.Plugin.Audio.Midi;

public static class MidiExtensions
{
    public static IMvcBuilder AddMidiAssembly(this IMvcBuilder builder)
    {
        return builder.AddApplicationPart(typeof(MidiExtensions).Assembly);
    }

    public static IServiceCollection RegisterMidiServices(this IServiceCollection services)
    {
        return services.AddSingleton<MidiKeyboardHandler>();
    }

    public static void ConstructRequiredMidiServices(this IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<MidiKeyboardHandler>();
    }
}
