FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.18-arm64v8 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
# RUN dotnet publish --os linux --arch amd64 -c Release -o out
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine3.18-arm64v8
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "CLI.dll"]
