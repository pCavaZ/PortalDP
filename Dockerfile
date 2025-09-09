# Imagen base para runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Imagen para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar todos los archivos .csproj primero (para cache de dependencias)
COPY ["PortalDP.API/PortalDP.PI.csproj", "PortalDP.API/"]
COPY ["PortalDP.Application/PortalDP.Application.csproj", "PortalDP.Application/"]
COPY ["PortalDP.Domain/PortalDP.Domain.csproj", "PortalDP.Domain/"]
COPY ["PortalDP.Infrastructure/PortalDP.Infrastructure.csproj", "PortalDP.Infrastructure/"]

# Restaurar dependencias para toda la solución
RUN dotnet restore "PortalDP.API/PortalDP.API.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar el proyecto API
WORKDIR "/src/PortalDP.API"
RUN dotnet publish "PortalDP.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Comando para ejecutar la aplicación
CMD ASPNETCORE_URLS=http://*:$PORT dotnet PortalDP.API.dll