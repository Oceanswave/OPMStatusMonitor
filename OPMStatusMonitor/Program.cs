namespace OPMStatusMonitor
{
    using Microsoft.Threading;
    using Q42.HueApi;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    class Program
    {
        public static string AppName = "OPMStatusMonitor";
        public static string AppKey = "RotinomSutatsMPO";
        public static string[] LightsToNotify = { "2" };
        static void Main(string[] args)
        {
            var locator = new HttpBridgeLocator();
            var hueIps = AsyncPump.Run(async () => { return await locator.LocateBridgesAsync(TimeSpan.FromSeconds(15)); }).ToList();

            var hueIp = hueIps.First();

            HueClient client = new HueClient(hueIp);
            Console.WriteLine("Hue found at:{0}", hueIp);
            AssociateWithHue(client, hueIp);

            OPMStatus? lastStatus = null;

            while (1 == 1)
            {
                Console.WriteLine("Checking OPM status...");
                var status = AsyncPump.Run<OPMStatus>(() => { return OPMMonitor.GetOPMCurrentStatus(); });

                if (status == OPMStatus.Error || (lastStatus.HasValue && lastStatus != status))
                {
                    var command = CreateLightCommandForStatus(status);

                    AsyncPump.Run(async () => { await client.SendCommandAsync(command, LightsToNotify); });
                }

                Console.WriteLine("Status {0}.", status);
                lastStatus = status;
                Thread.Sleep(5000);
            }
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
    }
}
