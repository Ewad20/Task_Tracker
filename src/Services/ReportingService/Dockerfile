FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PostSharpLicense
ENV PostSharpLicense=$PostSharpLicense
WORKDIR /src
COPY src/BuildingBlocks/BuildingBlocks.csproj src/BuildingBlocks/
COPY src/Services/ReportingService/ReportingService.csproj src/Services/ReportingService/
RUN dotnet restore src/Services/ReportingService/ReportingService.csproj
COPY . .
WORKDIR /src/src/Services/ReportingService
RUN dotnet publish ReportingService.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReportingService.dll"]
