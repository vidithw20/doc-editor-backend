# ---------- BASE ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# 🔥 REQUIRED (Syncfusion official)
RUN ln -s /lib/x86_64-linux-gnu/libdl.so.2 /lib/x86_64-linux-gnu/libdl.so

# 🔥 System.Drawing dependencies
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    && rm -rf /var/lib/apt/lists/*

RUN ln -s libgdiplus.so gdiplus.dll

WORKDIR /app
EXPOSE 10000

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false


# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["ASP.NET Core/src/EJ2APIServices_NET8.csproj", "ASP.NET Core/src/"]
RUN dotnet restore "ASP.NET Core/src/EJ2APIServices_NET8.csproj"

COPY . .

WORKDIR "/source/ASP.NET Core/src"
RUN dotnet build -c Release -o /app


# ---------- PUBLISH ----------
FROM build AS publish
RUN dotnet publish -c Release -o /app


# ---------- FINAL ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "EJ2APIServices_NET8.dll"]