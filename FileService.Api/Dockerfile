FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]
COPY ["FileService.Api/FileService.Api.csproj", "FileService.Api/"]
RUN dotnet restore "FileService.Api/FileService.Api.csproj"

COPY . .

WORKDIR "/src/FileService.Api"
RUN dotnet publish "FileService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileService.Api.dll"]
