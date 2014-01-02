namespace OPMStatusMonitor
{
    using Topshelf;

    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<OPMMonitor>(s =>
                {
                    s.ConstructUsing(name => new OPMMonitor());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsPrompt();

                x.SetDescription("Monitors the OPM site.");
                x.SetDisplayName("OPM Monitor");
                x.SetServiceName("OPMMonitor");
                x.StartAutomatically();
                
            }); 
        }
    }
}
