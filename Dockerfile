FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview-alpine AS base
RUN apk add --no-cache gosu ca-certificates icu-libs tzdata icu-data-full krb5-libs
COPY docker/entrypoint.sh /entrypoint.sh
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN chmod +x /entrypoint.sh
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:11.0-preview-alpine AS build
ARG CONFIGURATION=Release
ARG BUILD_JOBS

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
        -p:ArtifactsPivots=out -p:UseAppHost=false -p:DebugType=portable -p:DebugSymbols=true

FROM base AS final
ARG PROJECT
ENV SERVICE_DLL=$PROJECT.dll
WORKDIR /app
COPY --from=build /src/artifacts/publish/$PROJECT/out/ ./
ENTRYPOINT ["/entrypoint.sh"]
