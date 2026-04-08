# ---------- BASE RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6 \
    libfontconfig1 \
    libfreetype6 \
    libjpeg62-turbo \
    libpng16-16 \
    libx11-6 \
    libxrender1 \
    libxcb1 \
    libxext6 \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 10000


# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj first (for caching)
COPY ["ASP.NET Core/src/EJ2APIServices_NET8.csproj", "ASP.NET Core/src/"]
RUN dotnet restore "ASP.NET Core/src/EJ2APIServices_NET8.csproj"

# copy everything else
COPY . .

WORKDIR "/src/ASP.NET Core/src"
RUN dotnet build "EJ2APIServices_NET8.csproj" -c Release -o /app/build


# ---------- PUBLISH ----------
FROM build AS publish
RUN dotnet publish "EJ2APIServices_NET8.csproj" -c Release -o /app/publish


# ---------- FINAL ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "EJ2APIServices_NET8.dll"]