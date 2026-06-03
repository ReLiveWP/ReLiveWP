# Distinct image for the web frontend (has an SPA/client build step).
# Build context is the repo root (DockerfileContext=..\.. in the csproj).

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends gosu \
    && rm -rf /var/lib/apt/lists/*
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
WORKDIR /app

# --- SDK + node/pnpm for the client build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS with-node
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && curl -sL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && corepack enable \
    && rm -rf /var/lib/apt/lists/*

FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
# install client deps up front; the JS SDK runs the build script during publish but does not install
RUN corepack prepare pnpm@10.13.1 --activate \
    && cd ReLiveWP.Web/relivewp.web.client \
    && pnpm install --frozen-lockfile
RUN dotnet publish "ReLiveWP.Web/ReLiveWP.Web.Server/ReLiveWP.Web.Server.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- final image ---
FROM base AS final
ENV SERVICE_DLL=ReLiveWP.Web.Server.dll
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["/entrypoint.sh"]
