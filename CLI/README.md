```powershell
op inject -i appsettings.json -o appsettings.Development.json
$env:DOTNET_ENVIRONMENT = "development"; dotnet run
```

```powershell
op inject -i appsettings.json -o appsettings.Production.json
docker -H "ssh://rpi5-8" build -t route53-ddns-dotnet .
```

Test. Apply to crontab.

```powershell
docker -H "ssh://rpi5-8" run --rm route53-ddns-dotnet
```
