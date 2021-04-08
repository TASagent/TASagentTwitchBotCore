namespace TASagentTwitchBot.Core.Config
{
    public class MicConfiguration
    {
        public NoiseGateConfiguration NoiseGateConfiguration { get; set; } = new NoiseGateConfiguration();
        public ExpanderConfiguration ExpanderConfiguration { get; set; } = new ExpanderConfiguration();
        public CompressorConfiguration CompressorConfiguration { get; set; } = new CompressorConfiguration();
    }

    public class NoiseGateConfiguration
    {
        public bool Enabled { get; set; } = false;

        public double OpenThreshold { get; set; } = -26;
        public double CloseThreshold { get; set; } = -32;
        public double AttackDuration { get; set; } = 0.025;
        public double HoldDuration { get; set; } = 0.3;
        public double ReleaseDuration { get; set; } = 0.3;
    }

    public class ExpanderConfiguration
    {
        public bool Enabled { get; set; } = true;

        public double Ratio { get; set; } = 2.0;
        public double Threshold { get; set; } = -40.0;
        public double AttackDuration { get; set; } = 0.005;
        public double ReleaseDuration { get; set; } = 0.200;
        public double OutputGain { get; set; } = 0.0;
    }

    public class CompressorConfiguration
    {
        public bool Enabled { get; set; } = true;

        public double Ratio { get; set; } = 3.0;
        public double Threshold { get; set; } = -20.0;
        public double AttackDuration { get; set; } = 0.001;
        public double ReleaseDuration { get; set; } = 0.200;
        public double OutputGain { get; set; } = 10.0;
    }
}
