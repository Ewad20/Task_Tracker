FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PostSharpLicense
ENV PostSharpLicense=$PostSharpLicense
WORKDIR /src
COPY src/BuildingBlocks/BuildingBlocks.csproj src/BuildingBlocks/
COPY src/Services/UserService/UserService.csproj src/Services/UserService/
RUN dotnet restore src/Services/UserService/UserService.csproj
COPY . .
WORKDIR /src/src/Services/UserService
RUN dotnet publish UserService.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.dll"]
