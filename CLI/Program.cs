using System.CommandLine;
using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IHostEnvironment env = builder.Environment;
builder.Configuration
  .AddJsonFile("appsettings.json", optional: true)
  .AddJsonFile($"appsettings.{env.EnvironmentName}.json");
using IHost host = builder.Build();

SentrySdk.Init(options =>
{
  options.Dsn = builder.Configuration.GetValue<string>("SentryDsn");
});

AwsSettings awsSettings = new();
builder.Configuration.GetSection("AWS").Bind(awsSettings);

var rootCommand = new RootCommand("Update your Route53 DNS record with your current public IP address");

var whatsMyIpCommand = new Command("whats-my-ip", "Check your public IP address");
whatsMyIpCommand.SetHandler(whatsMyIpAsync);
rootCommand.AddCommand(whatsMyIpCommand);

var registeredIpCommand = new Command("registered-ip", "Check your Route53 registered IP address");
registeredIpCommand.SetHandler(async context => await registeredIpAsync(awsSettings));
rootCommand.AddCommand(registeredIpCommand);


static async Task whatsMyIpAsync()
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

static async Task registeredIpAsync(AwsSettings awsSettings)
{
  var credentials = new BasicAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey);
  var route53Client = new AmazonRoute53Client(credentials, RegionEndpoint.GetBySystemName(awsSettings.Region));
  var response = await route53Client.ListResourceRecordSetsAsync(new ListResourceRecordSetsRequest
  {
    HostedZoneId = awsSettings.HostedZoneId,
    StartRecordName = awsSettings.RecordName,
    MaxItems = "1"
  });
  var registeredIp = response.ResourceRecordSets[0].ResourceRecords[0].Value;

  Console.WriteLine($"Your Route53 registered IP address for {awsSettings.RecordName} is: {registeredIp}");
}

await rootCommand.InvokeAsync(args);
