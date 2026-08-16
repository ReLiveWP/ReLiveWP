# Build context: repo root
FROM node:20-alpine AS build

ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable && corepack prepare pnpm@10.13.1 --activate

WORKDIR /app

ARG APP=relivewp.web.client

COPY ReLiveWP.Web/package.json \
     ReLiveWP.Web/pnpm-workspace.yaml \
     ReLiveWP.Web/pnpm-lock.yaml ./
COPY ReLiveWP.Web/relivewp.web.client/package.json relivewp.web.client/
COPY ReLiveWP.Web/relivewp.web.mail/package.json relivewp.web.mail/
COPY ReLiveWP.Web/ReLiveWP.Services.Support/package.json ReLiveWP.Services.Support/
COPY ReLiveWP.Web/packages/eas-client/package.json packages/eas-client/
COPY ReLiveWP.Web/packages/eas-store/package.json packages/eas-store/
COPY ReLiveWP.Web/packages/eas-sync/package.json packages/eas-sync/
COPY ReLiveWP.Web/packages/ui/package.json packages/ui/

COPY ReLiveWP.Web/packages/ packages/
COPY ReLiveWP.Web/$APP/ $APP/

RUN --mount=type=cache,id=pnpm,target=/tmp/pnpm-store \
    pnpm install --frozen-lockfile --store-dir=/tmp/pnpm-store
   
RUN --mount=type=cache,id=pnpm,target=/tmp/pnpm-store \
    pnpm --filter "./packages/**" run build

ARG CLIENT_BUILD_SCRIPT=build
RUN pnpm -C $APP run $CLIENT_BUILD_SCRIPT

FROM nginx:alpine AS final
ARG APP=relivewp.web.client
COPY --from=build /app/$APP/dist /usr/share/nginx/html
COPY ReLiveWP.Web/nginx.conf /etc/nginx/conf.d/default.conf
