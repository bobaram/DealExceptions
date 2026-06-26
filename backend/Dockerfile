FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for layer-cached restore
COPY DealExceptions.Domain/DealExceptions.Domain.csproj             DealExceptions.Domain/
COPY DealExceptions.Application/DealExceptions.Application.csproj   DealExceptions.Application/
COPY DealExceptions.Infrastructure/DealExceptions.Infrastructure.csproj DealExceptions.Infrastructure/
COPY DealExceptions.Api/DealExceptions.Api.csproj                   DealExceptions.Api/
RUN dotnet restore DealExceptions.Api/DealExceptions.Api.csproj

# Copy source and publish
COPY . .
RUN dotnet publish DealExceptions.Api/DealExceptions.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DealExceptions.Api.dll"]
