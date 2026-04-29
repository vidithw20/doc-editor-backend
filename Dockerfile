FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN ln -s /lib/x86_64-linux-gnu/libdl.so.2 /lib/x86_64-linux-gnu/libdl.so

RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    libfontconfig1 \
    libfreetype6 \
    libpng16-16 \
    libjpeg62-turbo \
    libharfbuzz0b \
    fonts-dejavu-core \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

RUN ln -s libgdiplus.so gdiplus.dll

WORKDIR /app
EXPOSE 10000

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["ASP.NET Core/src/EJ2APIServices_NET8.csproj", "ASP.NET Core/src/"]
RUN dotnet restore "ASP.NET Core/src/EJ2APIServices_NET8.csproj"

COPY . .

WORKDIR "/source/ASP.NET Core/src"

RUN dotnet build "EJ2APIServices_NET8.csproj" -c Release -o /app


FROM build AS publish
RUN dotnet publish "EJ2APIServices_NET8.csproj" -c Release -o /app


FROM base AS final
WORKDIR /app

COPY --from=publish /app .

ENTRYPOINT ["dotnet", "EJ2APIServices_NET8.dll"]