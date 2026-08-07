FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
RUN apk add --no-cache gosu ca-certificates icu-libs tzdata icu-data-full krb5-libs
COPY docker/entrypoint.sh /entrypoint.sh
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN chmod +x /entrypoint.sh
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG CONFIGURATION=Release

# nodejs/npm come from alpine's own repos, so they are musl-native and need no compat shim
RUN apk add --no-cache nodejs npm icu-libs && npm install -g pnpm@10.13.1

WORKDIR /src
COPY . .

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=dotnet_tools,target=/root/.dotnet \
    --mount=type=cache,id=pnpm,target=/root/.local/share/pnpm/store \
    dotnet publish ReLiveWP.Services.Support/ReLiveWP.Services.Support.csproj \
        -c "$CONFIGURATION" -o /app/publish \
        /p:UseAppHost=false /p:DebugType=portable /p:DebugSymbols=true

FROM base AS final
ENV SERVICE_DLL=ReLiveWP.Services.Support.dll
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["/entrypoint.sh"]
