FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY APIScriptoriumArchivum.csproj ./
RUN dotnet restore APIScriptoriumArchivum.csproj

COPY . ./
RUN dotnet publish APIScriptoriumArchivum.csproj --configuration Release --no-restore --output /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish ./
RUN install -d -m 700 -o "$APP_UID" /home/app/.aspnet/DataProtection-Keys
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "APIScriptoriumArchivum.dll"]
