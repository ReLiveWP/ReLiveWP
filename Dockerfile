ARG PROJECT

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
RUN apk add --no-cache gosu ca-certificates icu-libs tzdata icu-data-full
COPY docker/entrypoint.sh /entrypoint.sh
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN chmod +x /entrypoint.sh
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS base-build
RUN apk add --no-cache protobuf protobuf-dev grpc grpc-plugins icu-libs
ENV PROTOBUF_PROTOC=/usr/bin/protoc
ENV gRPC_PluginFullPath=/usr/bin/grpc_csharp_plugin

FROM base-build AS build
ARG PROJECT
WORKDIR /src

COPY . .

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=dotnet_tools,target=/root/.dotnet \
    dotnet publish "$PROJECT/$PROJECT.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- final image ---
FROM base AS final
ARG PROJECT
ENV SERVICE_DLL=$PROJECT.dll
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["/entrypoint.sh"]
