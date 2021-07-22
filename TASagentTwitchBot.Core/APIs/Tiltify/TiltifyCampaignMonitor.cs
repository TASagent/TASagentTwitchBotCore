using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace TASagentTwitchBot.Core.API.Tiltify
{
    public class TiltifyCampaignMonitor : IDisposable
    {
        private readonly TiltifyHelper tiltifyHelper;
        private readonly TiltifyConfiguration tiltifyConfig;
        private readonly ErrorHandler errorHandler;
        private readonly ICommunication communication;
        private readonly Donations.IDonationHandler donationHandler;

        private readonly HandledDonationData handledDonationData;

        private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
        private readonly CountdownEvent readers = new CountdownEvent(1);

        private bool disposedValue;

        public TiltifyCampaignMonitor(
            TiltifyConfiguration tiltifyConfig,
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

            MonitorCampaign();
        }

        public async void MonitorCampaign()
        {
            if (tiltifyConfig.CampaignId == -1 || !tiltifyConfig.MonitorCampaign)
            {
                return;
            }

            try
            {
                readers.AddCount();

                while (true)
                {
                    CampaignDonationRequest donations = await tiltifyHelper.GetCampaignDonations(tiltifyConfig.CampaignId);

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
            catch (TaskCanceledException) { /* swallow */ }
            catch (ThreadAbortException) { /* swallow */ }
            catch (ObjectDisposedException) { /* swallow */ }
            catch (OperationCanceledException) { /* swallow */ }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Tiltify Campaign Manager Pinger Exception: {ex.GetType().Name}");
                errorHandler.LogMessageException(ex, "");
            }
            finally
            {
                readers.Signal();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    generalTokenSource.Cancel();

                    //Wait for readers
                    readers.Signal();
                    readers.Wait();
                    readers.Dispose();

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

            public List<CampaignDonation> campaignDonations = new List<CampaignDonation>();

            [JsonIgnore]
            public HashSet<int> donations = new HashSet<int>();

            public static HandledDonationData GetData()
            {
                HandledDonationData data;

                if (File.Exists(FilePath))
                {
                    data = JsonSerializer.Deserialize<HandledDonationData>(File.ReadAllText(FilePath));

                    foreach (CampaignDonation donation in data.campaignDonations)
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
                    campaignDonations.Add(donation);

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
}
