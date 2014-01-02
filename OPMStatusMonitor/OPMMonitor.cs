namespace OPMStatusMonitor
{
    using HtmlAgilityPack;
    using ScrapySharp.Extensions;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class OPMMonitor
    {
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
