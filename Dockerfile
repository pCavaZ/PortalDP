# Imagen base para runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Imagen para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar todos los archivos .csproj primero (para cache de dependencias)
COPY ["PortalDPAPI/PortalDPAPI.csproj", "PortalDPAPI/"]
COPY ["PortalDP.Application/PortalDP.Application.csproj", "PortalDP.Application/"]
COPY ["PortalDP.Domain/PortalDP.Domain.csproj", "PortalDP.Domain/"]
COPY ["PortalDP.Infrastructure/PortalDP.Infrastructure.csproj", "PortalDP.Infrastructure/"]

# Restaurar dependencias para toda la solución
RUN dotnet restore "PortalDPAPI/PortalDPAPI.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar el proyecto API
WORKDIR "/src/PortalDPAPI"
RUN dotnet publish "PortalDPAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Comando para ejecutar la aplicación
CMD ASPNETCORE_URLS=http://*:$PORT dotnet PortalDPAPI.dll