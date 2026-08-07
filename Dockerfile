FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
RUN apk add --no-cache gosu ca-certificates icu-libs tzdata icu-data-full krb5-libs
COPY docker/entrypoint.sh /entrypoint.sh
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN chmod +x /entrypoint.sh
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS base-build
RUN apk add --no-cache protobuf protobuf-dev grpc grpc-plugins icu-libs
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin
WORKDIR /src
COPY . .

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=dotnet_tools,target=/root/.dotnet \
    dotnet restore ReLiveWP.slnx

FROM base-build AS build
ARG PROJECT
ARG CONFIGURATION=Release

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=dotnet_tools,target=/root/.dotnet \
    dotnet publish "$PROJECT/$PROJECT.csproj" -c "$CONFIGURATION" -o /app/publish \
        /p:UseAppHost=false /p:DebugType=portable /p:DebugSymbols=true

# --- final image ---
FROM base AS final
ARG PROJECT
ENV SERVICE_DLL=$PROJECT.dll
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["/entrypoint.sh"]
