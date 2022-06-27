using System.Text.Json.Serialization;
using System.Text.Json;

namespace TASagentTwitchBot.Core.API.Tiltify;

public class TiltifyCampaignMonitor : IStartupListener, IDisposable
{
    private readonly TiltifyHelper tiltifyHelper;
    private readonly TiltifyConfiguration tiltifyConfig;
    private readonly ErrorHandler errorHandler;
    private readonly ICommunication communication;
    private readonly Donations.IDonationHandler donationHandler;

    private readonly HandledDonationData handledDonationData;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    private readonly Task monitorTask;

    private bool disposedValue;

    public TiltifyCampaignMonitor(
        TiltifyConfiguration tiltifyConfig,
        Config.BotConfiguration botConfig,
        TiltifyHelper tiltifyHelper,
        ErrorHandler errorHandler,
        ICommunication communication,
        Donations.IDonationHandler donationHandler)
    {
        this.tiltifyConfig = tiltifyConfig;
        this.tiltifyHelper = tiltifyHelper;
        this.errorHandler = errorHandler;
        this.communication = communication;
        this.donationHandler = donationHandler;

        handledDonationData = HandledDonationData.GetData();

        if (tiltifyConfig.CampaignId == -1 || !tiltifyConfig.MonitorCampaign)
        {
            //No campaign running
            monitorTask = Task.CompletedTask;
        }
        else
        {
            //Campaign running
            if (botConfig.UseThreadedMonitors)
            {
                monitorTask = Task.Run(MonitorCampaign);
            }
            else
            {
                monitorTask = MonitorCampaign();
            }
        }

    }

    private async Task MonitorCampaign()
    {
        try
        {
            while (true)
            {
                CampaignDonationRequest donations = await tiltifyHelper.GetCampaignDonations(tiltifyConfig.CampaignId) ??
                    throw new Exception("Unable to Get Tiltify Campaign Donations.");

                foreach (CampaignDonation donation in donations.CampaignDonations)
                {
                    if (handledDonationData.Add(donation))
                    {
                        //New Donation
                        donationHandler.HandleDonation(
                            name: donation.Name,
                            amount: donation.Amount,
                            message: donation.Comment,
                            approved: true);
                    }
                    else
                    {
                        //Hit end of new donations
                        break;
                    }
                }

                //Wait 60 seconds
                await Task.Delay(60 * 1000, generalTokenSource.Token);

                //Bail if we're trying to quit
                if (generalTokenSource.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) { /* swallow */ }
        catch (ThreadAbortException) { /* swallow */ }
        catch (ObjectDisposedException) { /* swallow */ }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Tiltify Campaign Manager Pinger Exception: {ex.GetType().Name}");
            errorHandler.LogMessageException(ex, "");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                generalTokenSource.Cancel();

                //Wait for monitor
                monitorTask.Wait(2_000);

                generalTokenSource.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private class HandledDonationData
    {
        [JsonIgnore]
        private static string FilePath => BGC.IO.DataManagement.PathForDataFile("Config", "Tiltify", "HandledTransactions.json");

        [JsonIgnore]
        private static readonly object _lock = new object();

        public List<CampaignDonation> CampaignDonations { get; set; } = new List<CampaignDonation>();

        [JsonIgnore]
        public HashSet<int> donations = new HashSet<int>();

        public static HandledDonationData GetData()
        {
            HandledDonationData data;

            if (File.Exists(FilePath))
            {
                data = JsonSerializer.Deserialize<HandledDonationData>(File.ReadAllText(FilePath))!;

                foreach (CampaignDonation donation in data.CampaignDonations)
                {
                    data.donations.Add(donation.Id);
                }
            }
            else
            {
                data = new HandledDonationData();
                File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
            }

            return data;
        }

        public bool Add(CampaignDonation donation)
        {
            if (donations.Add(donation.Id))
            {
                //Successfully added new donation
                CampaignDonations.Add(donation);

                lock (_lock)
                {
                    File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
                }

                return true;
            }

            return false;
        }
    }
}
