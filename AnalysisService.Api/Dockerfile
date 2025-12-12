FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["AnalysisService.Api/AnalysisService.Api.csproj", "AnalysisService.Api/"]
RUN dotnet restore "AnalysisService.Api/AnalysisService.Api.csproj"

COPY . .

WORKDIR "/src/AnalysisService.Api"
RUN dotnet publish "AnalysisService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AnalysisService.Api.dll"]
