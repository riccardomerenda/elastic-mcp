FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ElasticMcp.slnx .
COPY src/ElasticMcp/ElasticMcp.csproj src/ElasticMcp/
COPY src/ElasticMcp.Http/ElasticMcp.Http.csproj src/ElasticMcp.Http/
RUN dotnet restore src/ElasticMcp.Http/ElasticMcp.Http.csproj

COPY src/ src/
RUN dotnet publish src/ElasticMcp.Http/ElasticMcp.Http.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ElasticMcp.Http.dll"]
