namespace OPMStatusMonitor
{
    using HtmlAgilityPack;
    using Microsoft.Threading;
    using Q42.HueApi;
    using ScrapySharp.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class OPMMonitor
    {
        public static string AppName = "OPMStatusMonitor";
        public static string AppKey = "RotinomSutatsMPO";
        public static string[] LightsToNotify = { "2" };

        private HueClient m_client;
        private bool m_isRunning = false;

        public OPMMonitor()
        {
        }

        public void Start()
        {
            var locator = new HttpBridgeLocator();
            var hueIps = AsyncPump.Run(async () => { return await locator.LocateBridgesAsync(TimeSpan.FromSeconds(15)); }).ToList();

            var hueIp = hueIps.First();

            m_client = new HueClient(hueIp);
            Console.WriteLine("Hue found at:{0}", hueIp);
            AssociateWithHue(m_client, hueIp);

            OPMStatus? lastStatus = null;
            m_isRunning = true;

            while (m_isRunning)
            {
                Console.WriteLine("Checking OPM status...");
                var status = AsyncPump.Run<OPMStatus>(() => { return OPMMonitor.GetOPMCurrentStatus(); });

                if (status == OPMStatus.Error || (lastStatus.HasValue && lastStatus != status))
                {
                    var command = CreateLightCommandForStatus(status);

                    AsyncPump.Run(async () => { await m_client.SendCommandAsync(command, LightsToNotify); });
                }

                Console.WriteLine("Status {0}.", status);
                lastStatus = status;
                Thread.Sleep(5000);
            }
        }

        public void Stop()
        {
            m_client = null;
            m_isRunning = false;
        }

        private static LightCommand CreateLightCommandForStatus(OPMStatus status)
        {
            var command = new LightCommand();
            command.On = true;

            switch (status)
            {
                case OPMStatus.Alert:
                    command.TurnOn().SetColor("FF8000");
                    break;
                case OPMStatus.Closed:
                    command.TurnOn().SetColor("FF0000");
                    break;
                case OPMStatus.Open:
                    command.TurnOn().SetColor("00FF00");
                    break;
                case OPMStatus.Error:
                    command.TurnOn().SetColor("0000FF");
                    break;
            }

            command.Brightness = 255;
            command.Alert = Alert.Once;

            return command;
        }

        private static IEnumerable<Light> AssociateWithHue(HueClient client, string ip)
        {
            client.Initialize(AppKey);

            if (client.IsInitialized == false)
            {
                throw new InvalidOperationException("Unable to connect with your Hue -- ensure this machine is able to locate the hue on your network.");
            }

            IEnumerable<Light> lights;

            //Try catch a get operation to assert that we're registered with the hue.
            try
            {
                lights = AsyncPump.Run(async () => { return await client.GetLightsAsync(); });
            }
            catch
            {
                Console.WriteLine("Registering OPMStatusMonitor with your Hue.");
                Console.WriteLine("Please press the link button on your hue and then press enter.");
                Console.ReadLine();
                var result = AsyncPump.Run(async () => { return await client.RegisterAsync(AppName, AppKey); });

                if (result == true)
                {
                    client = new HueClient(ip);
                    return AssociateWithHue(client, ip);
                }
                else
                {
                    throw new InvalidOperationException("Unable to register the OPMStatusMonitor with your Hue -- try again potentally reducing the time between pressing the link button and enter.");
                }
            }

            return lights;
        }

        public static async Task<OPMStatus> GetOPMCurrentStatus()
        {
            var client = new HttpClient();
            var result = await client.GetAsync("http://www.opm.gov/policy-data-oversight/snow-dismissal-procedures/current-status/");
            if (result.IsSuccessStatusCode)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(await result.Content.ReadAsStringAsync());

                var statusContainer = doc.DocumentNode.CssSelect(".StatusContainer").FirstOrDefault();
                if (statusContainer != null)
                {
                    var statusNode = statusContainer.CssSelect(".Status").FirstOrDefault();
                    if (statusNode != null)
                    {
                        var statusClass = statusNode.Attributes["class"].Value;
                        if (statusClass.IndexOf("Open", StringComparison.CurrentCultureIgnoreCase) != -1)
                            return OPMStatus.Open;
                        else if (statusClass.IndexOf("Alert", StringComparison.CurrentCultureIgnoreCase) != -1)
                            return OPMStatus.Alert;
                        else if (statusClass.IndexOf("Closed", StringComparison.CurrentCultureIgnoreCase) != -1)
                            return OPMStatus.Closed;
                    }
                }
            }

            return OPMStatus.Error;
        }
    }
}
