# ── Stage 1: Frontend ───────────────────────────────────────────────────────
FROM node:22-alpine AS frontend-build

WORKDIR /src

COPY package.json yarn.lock tsconfig.json ./
COPY frontend/ frontend/

RUN yarn install --frozen-lockfile && \
    yarn build -- --env production

# ── Stage 2: Backend ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build

WORKDIR /src

COPY global.json ./
COPY src/ src/

RUN dotnet publish src/Streamarr.Console/Streamarr.Console.csproj \
        -c Release \
        -f net10.0 \
        -r linux-x64 \
        --no-self-contained \
        /p:TreatWarningsAsErrors=false \
        -o /app

# ── Stage 3: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Install runtime dependencies: ffmpeg for yt-dlp muxing, gosu for PUID/PGID
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        ffmpeg \
        curl \
        gosu \
        ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Install yt-dlp binary
RUN curl -fsSL https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp \
        -o /usr/local/bin/yt-dlp && \
    chmod +x /usr/local/bin/yt-dlp

# Create app user (PUID/PGID can be remapped at runtime via entrypoint)
RUN groupadd -g 1000 streamarr && \
    useradd -u 1000 -g streamarr -s /bin/bash -M streamarr

# Copy published app and frontend UI
COPY --from=backend-build /app /app
COPY --from=frontend-build /src/_output/UI /app/UI

# Entrypoint script (handles PUID/PGID remapping)
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

VOLUME ["/config", "/downloads"]

EXPOSE 8990

ENV TZ=UTC

WORKDIR /app

ENTRYPOINT ["/entrypoint.sh"]
