FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /App
COPY --from=build /App/out .
COPY secrets.json .

ENTRYPOINT ["dotnet", "MediatorTelegramBot.dll"]

