# TaladPOS web (Next.js 14) - built by docker-compose.yml at the repo root.
# Build context is web/, see web/.dockerignore.

FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# Every page is a client component, so the browser - not this container -
# calls the API. NEXT_PUBLIC_* values are inlined into the JS bundle at build
# time, which means this must be a URL the student's browser can reach
# (http://localhost:5054), not the compose service name.
ARG NEXT_PUBLIC_API_BASE_URL=http://localhost:5054
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# output: "standalone" (next.config.mjs) bundles only the node_modules the
# server needs; static assets and public/ are not included and must be copied.
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static
COPY --from=build --chown=node:node /app/public ./public

USER node
ENV PORT=3000
# Docker sets HOSTNAME to the container id; the standalone server binds to it,
# so force all interfaces or the published port is unreachable.
ENV HOSTNAME=0.0.0.0
EXPOSE 3000
CMD ["node", "server.js"]
