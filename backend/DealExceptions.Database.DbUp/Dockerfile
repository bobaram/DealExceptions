FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DealExceptions.Database.DbUp/DealExceptions.Database.DbUp.csproj DealExceptions.Database.DbUp/
RUN dotnet restore DealExceptions.Database.DbUp/DealExceptions.Database.DbUp.csproj
COPY DealExceptions.Database.DbUp/ DealExceptions.Database.DbUp/
RUN dotnet publish DealExceptions.Database.DbUp/DealExceptions.Database.DbUp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DealExceptions.Database.DbUp.dll"]
