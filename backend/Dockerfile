# ---------- Build stage ----------
FROM node:alpine AS build

WORKDIR /app

# Build-time args (come from CLI / docker-compose)
ARG SENTRY_AUTH_TOKEN

# Make them available as env vars during build
ENV SENTRY_AUTH_TOKEN=$SENTRY_AUTH_TOKEN

# Install deps
COPY package*.json ./
RUN npm ci

# Copy source and build
COPY . .
RUN npm run build


# ---------- Runtime stage ----------
FROM node:alpine AS runtime

WORKDIR /app

# Copy build artifacts from previous stage
COPY package*.json ./
COPY --from=build /app/dist ./dist
COPY --from=build /app/node_modules ./node_modules

EXPOSE 3000

CMD ["npm", "run", "start:prod"]