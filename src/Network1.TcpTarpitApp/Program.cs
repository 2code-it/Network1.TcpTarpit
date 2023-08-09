

using Network1.TcpTarpitApp;
using System.Text;

AppService appService = new AppService();

if (args.Length == 0 || args.Length == 1 && args[0] == "-h")
{
	Console.WriteLine(appService.GetHelpText());
}
else
{
	appService.Start(args);
	if (appService.Error is not null)
	{
		Console.WriteLine(appService.Error);
	}
	else
	{
		Console.WriteLine($"{nameof(Network1.TcpTarpitApp)} running with:");
		Console.Write(GetRunInfo());
		Console.WriteLine();
		Console.WriteLine("Press any key to quit");
	}
	Console.ReadKey();
	appService.Stop();
}

string GetRunInfo()
{
	StringBuilder sb = new StringBuilder();
	sb.AppendLine($"- Listen address: {appService.AppOptions.ListenAddress}");
	sb.AppendLine($"- Ports count: {appService.AppOptions.Ports.Length}");
	sb.AppendLine($"- Iteration: delay={appService.AppOptions.IterationDelayInMs}, bytes={appService.AppOptions.BytesPerIteration}");
	sb.AppendLine($"- Connection timeout: {appService.AppOptions.MaxConnectionTimeInSeconds}");
	return sb.ToString();
}