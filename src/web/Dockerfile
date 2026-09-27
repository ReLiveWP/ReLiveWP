# Build context: repo root
FROM node:20-alpine AS build

ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable && corepack prepare pnpm@10.13.1 --activate

WORKDIR /app

ARG APP=relivewp.web.client

COPY src/web/package.json \
     src/web/pnpm-workspace.yaml \
     src/web/pnpm-lock.yaml ./
COPY src/web/relivewp.web.client/package.json relivewp.web.client/
COPY src/web/relivewp.web.mail/package.json relivewp.web.mail/
COPY src/web/relivewp.web.login/package.json relivewp.web.login/
COPY src/web/relivewp.web.sso/package.json relivewp.web.sso/
COPY src/web/relivewp.web.support/package.json relivewp.web.support/
COPY src/web/packages/auth/package.json packages/auth/
COPY src/web/packages/calendar/package.json packages/calendar/
COPY src/web/packages/eas-client/package.json packages/eas-client/
COPY src/web/packages/eas-store/package.json packages/eas-store/
COPY src/web/packages/eas-sync/package.json packages/eas-sync/
COPY src/web/packages/ui/package.json packages/ui/

COPY src/web/packages/ packages/
COPY src/web/$APP/ $APP/

RUN --mount=type=cache,id=pnpm,target=/tmp/pnpm-store \
    pnpm install --frozen-lockfile --store-dir=/tmp/pnpm-store
   
RUN --mount=type=cache,id=pnpm,target=/tmp/pnpm-store \
    pnpm --filter "./packages/**" run build

ARG CLIENT_BUILD_SCRIPT=build
RUN pnpm -C $APP run $CLIENT_BUILD_SCRIPT

FROM nginx:alpine AS final
ARG APP=relivewp.web.client
COPY --from=build /app/$APP/dist /usr/share/nginx/html
COPY src/web/nginx.conf /etc/nginx/conf.d/default.conf
