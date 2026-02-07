# =============================================================================
# Artisan Studio - Multi-stage Docker Build
# =============================================================================
# Stage 1: Build Frontend
# Stage 2: Build Backend
# Stage 3: Production Runtime
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build React Frontend
# -----------------------------------------------------------------------------
FROM node:20-alpine AS frontend-build

WORKDIR /app/frontend

# Copy package files first (better caching)
COPY frontend/package*.json ./

# Install dependencies
RUN npm ci --silent

# Copy source code
COPY frontend/ ./

# Build production bundle
RUN npm run build

# -----------------------------------------------------------------------------
# Stage 2: Build .NET Backend
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build

WORKDIR /app/backend

# Copy project file first (better caching)
COPY backend/*.csproj ./

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY backend/ ./

# Build release version
RUN dotnet publish -c Release -o /app/publish --no-restore

# -----------------------------------------------------------------------------
# Stage 3: Production Runtime
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Add labels for container registry
LABEL maintainer="Natali Lerner <natali.koifman@lernersoft.onmicrosoft.com>"
LABEL description="Artisan Studio - AI Image & Video Generator"
LABEL version="1.0.0"

WORKDIR /app

# Create non-root user for security
RUN addgroup -g 1000 artisan && \
    adduser -u 1000 -G artisan -s /bin/sh -D artisan

# Copy built backend
COPY --from=backend-build /app/publish ./

# Copy built frontend to wwwroot
COPY --from=frontend-build /app/frontend/dist ./wwwroot

# Create media directories
RUN mkdir -p wwwroot/media/images wwwroot/media/videos && \
    chown -R artisan:artisan /app

# Switch to non-root user
USER artisan

# Expose port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5000/api/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "ArtisanStudio.dll"]
