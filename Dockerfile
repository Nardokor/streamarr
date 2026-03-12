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
COPY Logo/ Logo/
COPY src/ src/

RUN dotnet publish src/Streamarr.Console/Streamarr.Console.csproj \
        -c Release \
        -f net10.0 \
        -r linux-x64 \
        --no-self-contained \
        /p:TreatWarningsAsErrors=false \
        /p:RunAnalyzers=false \
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

# Create streamarr group/user at 1000:1000, renaming existing entries if needed
# (dotnet/aspnet base image ships with app:app at 1000:1000)
RUN existing_group=$(getent group 1000 | cut -d: -f1); \
    if [ -n "$existing_group" ]; then \
        groupmod -n streamarr "$existing_group"; \
    else \
        groupadd -g 1000 streamarr; \
    fi && \
    existing_user=$(getent passwd 1000 | cut -d: -f1); \
    if [ -n "$existing_user" ]; then \
        usermod -l streamarr -g streamarr "$existing_user"; \
    else \
        useradd -u 1000 -g streamarr -s /bin/bash -M streamarr; \
    fi

# Install yt-dlp into a directory owned by the app user so the running
# process can self-update (yt-dlp --update-to nightly) without root.
RUN mkdir -p /opt/yt-dlp && \
    curl -fsSL https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux \
        -o /opt/yt-dlp/yt-dlp && \
    chmod +x /opt/yt-dlp/yt-dlp && \
    chown -R streamarr:streamarr /opt/yt-dlp

ENV PATH="/opt/yt-dlp:${PATH}"

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
