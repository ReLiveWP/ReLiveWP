# Build context: repo root
FROM node:20-alpine AS build

ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable && corepack prepare pnpm@10.13.1 --activate

WORKDIR /app

# Layer cache: reinstall only when lockfile changes
COPY ReLiveWP.Web/relivewp.web.client/package.json \
     ReLiveWP.Web/relivewp.web.client/pnpm-lock.yaml ./
RUN --mount=type=cache,id=pnpm,target=/tmp/pnpm-store \
    pnpm install --frozen-lockfile --store-dir=/tmp/pnpm-store

COPY styles/ /styles/

COPY ReLiveWP.Web/relivewp.web.client/ .

ARG CLIENT_BUILD_SCRIPT=build
RUN pnpm run $CLIENT_BUILD_SCRIPT

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY ReLiveWP.Web/nginx.conf /etc/nginx/conf.d/default.conf
