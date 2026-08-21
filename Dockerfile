FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
RUN apk add --no-cache gosu ca-certificates icu-libs tzdata icu-data-full krb5-libs
COPY docker/entrypoint.sh /entrypoint.sh
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN chmod +x /entrypoint.sh
WORKDIR /app

# One build of the whole solution, shared by every service image. Nothing in this
# stage may depend on PROJECT, or buildkit stops deduplicating it.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG CONFIGURATION=Release
ARG BUILD_JOBS

# nodejs/npm come from alpine's own repos, so they are musl-native and need no compat shim
RUN apk add --no-cache protobuf protobuf-dev grpc grpc-plugins icu-libs nodejs npm \
    && npm install -g pnpm@10.13.1

ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin
WORKDIR /src
COPY . .

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=dotnet_tools,target=/root/.dotnet \
    dotnet restore ReLiveWP.slnx

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=dotnet_tools,target=/root/.dotnet \
    --mount=type=cache,id=pnpm,target=/root/.local/share/pnpm/store \
    dotnet publish ReLiveWP.slnx -c "$CONFIGURATION" ${BUILD_JOBS:+-m:$BUILD_JOBS} \
        -p:UseAppHost=false -p:DebugType=portable -p:DebugSymbols=true

# --- final image ---
FROM base AS final
ARG PROJECT
ARG TARGET_FRAMEWORK=net10.0
ENV SERVICE_DLL=$PROJECT.dll
WORKDIR /app
COPY --from=build /src/publish/$TARGET_FRAMEWORK/$PROJECT/ ./
ENTRYPOINT ["/entrypoint.sh"]
