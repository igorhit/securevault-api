FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SecureVault.slnx", "./"]
COPY ["src/SecureVault.API/SecureVault.API.csproj", "src/SecureVault.API/"]
COPY ["src/SecureVault.Application/SecureVault.Application.csproj", "src/SecureVault.Application/"]
COPY ["src/SecureVault.Domain/SecureVault.Domain.csproj", "src/SecureVault.Domain/"]
COPY ["src/SecureVault.Infrastructure/SecureVault.Infrastructure.csproj", "src/SecureVault.Infrastructure/"]

RUN dotnet restore "src/SecureVault.API/SecureVault.API.csproj"

COPY . .
RUN dotnet publish "src/SecureVault.API/SecureVault.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && adduser --disabled-password --gecos "" appuser \
    && mkdir -p /app/runtime \
    && chown -R appuser:appuser /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER appuser

ENTRYPOINT ["dotnet", "SecureVault.API.dll"]
