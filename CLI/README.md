Dev test run:

```powershell
cd CLI
op inject -i appsettings.json -o appsettings.Development.json
$env:DOTNET_ENVIRONMENT = "development"; dotnet run
```

Deploy:

```powershell
cd CLI
op inject -i appsettings.json -o appsettings.Production.json
docker -H "ssh://rpi5-8" build -t rpi5-8:5000/route53-ddns-dotnet .
docker -H "ssh://rpi5-8" push rpi5-8:5000/route53-ddns-dotnet
```

Test. Apply to crontab.

```powershell
docker -H "ssh://rpi5-8" run --rm rpi5-8:5000/route53-ddns-dotnet
```
