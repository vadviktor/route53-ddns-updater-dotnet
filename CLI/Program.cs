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
rootCommand.SetHandler(async context =>
{
  var publicIp = await whatsMyIpAsync();
  var registeredIp = await registeredIpAsync(awsSettings);
  if (publicIp == registeredIp)
  {
    Console.WriteLine($"Your public IP address {publicIp} is already registered in Route53");
    return;
  }

  Console.WriteLine($"Updating Route53 record {awsSettings.RecordName} from {registeredIp} to {publicIp}");
  await updateIp(awsSettings, publicIp);
  Console.WriteLine($"Route53 record {awsSettings.RecordName} updated to {publicIp}");
});


var whatsMyIpCommand = new Command("whats-my-ip", "Check your public IP address");
whatsMyIpCommand.SetHandler(async () =>
{
  try
  {
    var ip = await whatsMyIpAsync();
    Console.WriteLine($"Your public IP address is: {ip}");
  }
  catch (HttpRequestException e)
  {
    Console.WriteLine($"Error: {e.Message}");
  }
});
rootCommand.AddCommand(whatsMyIpCommand);

var registeredIpCommand = new Command("registered-ip", "Check your Route53 registered IP address");
registeredIpCommand.SetHandler(async context =>
{
  var ip = await registeredIpAsync(awsSettings);
  Console.WriteLine($"Your Route53 registered IP address for {awsSettings.RecordName} is: {ip}");
});
rootCommand.AddCommand(registeredIpCommand);


static async Task updateIp(AwsSettings awsSettings, string publicIp)
{
  var credentials = new BasicAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey);
  var route53Client = new AmazonRoute53Client(credentials, RegionEndpoint.GetBySystemName(awsSettings.Region));
  var changeBatch = new ChangeBatch
  {
    Changes = new List<Change>
      {
        new Change
        {
          Action = ChangeAction.UPSERT,
          ResourceRecordSet = new ResourceRecordSet
          {
            Name = awsSettings.RecordName,
            Type = RRType.A,
            TTL = 3600,
            ResourceRecords = new List<ResourceRecord>
            {
              new ResourceRecord
              {
                Value = publicIp
              }
            }
          }
        }
      }
  };
  await route53Client.ChangeResourceRecordSetsAsync(new ChangeResourceRecordSetsRequest
  {
    HostedZoneId = awsSettings.HostedZoneId,
    ChangeBatch = changeBatch
  });
}

static async Task<string> whatsMyIpAsync()
{
  using var client = new HttpClient();
  var ip = await client.GetStringAsync("http://checkip.amazonaws.com/");
  return ip.Trim();
}

static async Task<string> registeredIpAsync(AwsSettings awsSettings)
{
  var credentials = new BasicAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey);
  var route53Client = new AmazonRoute53Client(credentials, RegionEndpoint.GetBySystemName(awsSettings.Region));
  var response = await route53Client.ListResourceRecordSetsAsync(new ListResourceRecordSetsRequest
  {
    HostedZoneId = awsSettings.HostedZoneId,
    StartRecordName = awsSettings.RecordName,
    MaxItems = "1"
  });
  return response.ResourceRecordSets[0].ResourceRecords[0].Value;
}

await rootCommand.InvokeAsync(args);
