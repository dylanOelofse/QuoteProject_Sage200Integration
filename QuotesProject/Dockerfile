# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# DevExpress reporting needs fontconfig + a font to render PDFs on Linux.
# Runs as root here, BEFORE the USER line drops privileges.
RUN apt-get update && apt-get install -y --no-install-recommends \
        fontconfig fonts-liberation libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NuGet.config", "./"]
COPY ["QuotesProject/QuotesProject.csproj", "QuotesProject/"]
RUN dotnet restore "./QuotesProject/QuotesProject.csproj"
COPY . .
WORKDIR "/src/QuotesProject"
RUN dotnet build "./QuotesProject.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./QuotesProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# DevExpress v25.1+ reads the license key from the DevExpress_License env var at runtime.
# Optionally baked in at build time (--build-arg); docker-compose also supplies it at runtime from .env.
ARG DevExpress_License
ENV DevExpress_License=$DevExpress_License

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuotesProject.dll"]