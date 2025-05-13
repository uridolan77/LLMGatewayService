FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/LLMGateway.API/LLMGateway.API.csproj", "src/LLMGateway.API/"]
COPY ["src/LLMGateway.Core/LLMGateway.Core.csproj", "src/LLMGateway.Core/"]
COPY ["src/LLMGateway.Infrastructure/LLMGateway.Infrastructure.csproj", "src/LLMGateway.Infrastructure/"]
COPY ["src/LLMGateway.Providers/LLMGateway.Providers.csproj", "src/LLMGateway.Providers/"]
RUN dotnet restore "src/LLMGateway.API/LLMGateway.API.csproj"
COPY . .
WORKDIR "/src/src/LLMGateway.API"
RUN dotnet build "LLMGateway.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LLMGateway.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LLMGateway.API.dll"]
