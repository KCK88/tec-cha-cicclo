FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Cicclo.Api/Cicclo.Api.csproj src/Cicclo.Api/
RUN dotnet restore src/Cicclo.Api/Cicclo.Api.csproj
COPY src/Cicclo.Api/ src/Cicclo.Api/
RUN dotnet publish src/Cicclo.Api/Cicclo.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet Cicclo.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
