# ---------- BASE ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# 🔥 Syncfusion required fix (VERY IMPORTANT)
RUN ln -s /lib/x86_64-linux-gnu/libdl.so.2 /lib/x86_64-linux-gnu/libdl.so

# 🔥 System.Drawing + rendering dependencies
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    && rm -rf /var/lib/apt/lists/*

# 🔥 Fix for System.Drawing on Linux
RUN ln -s libgdiplus.so gdiplus.dll

WORKDIR /app
EXPOSE 10000

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false


# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj first
COPY ["ASP.NET Core/src/EJ2APIServices_NET8.csproj", "ASP.NET Core/src/"]
RUN dotnet restore "ASP.NET Core/src/EJ2APIServices_NET8.csproj"

# copy everything
COPY . .

WORKDIR "/source/ASP.NET Core/src"

# 🔥 IMPORTANT: specify project file
RUN dotnet build "EJ2APIServices_NET8.csproj" -c Release -o /app


# ---------- PUBLISH ----------
FROM build AS publish
RUN dotnet publish "EJ2APIServices_NET8.csproj" -c Release -o /app


# ---------- FINAL ----------
FROM base AS final
WORKDIR /app

COPY --from=publish /app .

ENTRYPOINT ["dotnet", "EJ2APIServices_NET8.dll"]