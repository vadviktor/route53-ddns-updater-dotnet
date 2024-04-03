using System.CommandLine;

var rootCommand = new RootCommand("Update your Route53 DNS record with your current public IP address");

var whatsMyIpCommand = new Command("whats-my-ip", "Check your public IP address");
rootCommand.AddCommand(whatsMyIpCommand);

var registeredIpCommand = new Command("registered-ip", "Check your Route53 registered IP address");
rootCommand.AddCommand(registeredIpCommand);

whatsMyIpCommand.SetHandler(async () =>
    {
      try
      {
        using var client = new HttpClient();
        string ip = await client.GetStringAsync("http://checkip.amazonaws.com/");
        Console.WriteLine($"Your public IP address is: {ip.Trim()}");
      }
      catch (HttpRequestException e)
      {
        Console.WriteLine($"Error: {e.Message}");
      }
    }
);

registeredIpCommand.SetHandler(() =>
    {
      Console.WriteLine("TBD");
    }
);

return rootCommand.InvokeAsync(args).Result;
