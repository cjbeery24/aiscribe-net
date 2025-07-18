# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first and restore dependencies (for better caching)
COPY ["SermonTranscription.Api/SermonTranscription.Api.csproj", "SermonTranscription.Api/"]
COPY ["SermonTranscription.Application/SermonTranscription.Application.csproj", "SermonTranscription.Application/"]
COPY ["SermonTranscription.Domain/SermonTranscription.Domain.csproj", "SermonTranscription.Domain/"]
COPY ["SermonTranscription.Infrastructure/SermonTranscription.Infrastructure.csproj", "SermonTranscription.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "SermonTranscription.Api/SermonTranscription.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/SermonTranscription.Api"
RUN dotnet build "SermonTranscription.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SermonTranscription.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create a non-root user for security
RUN addgroup --gid 1000 appgroup && \
    adduser --uid 1000 --gid 1000 --disabled-password --gecos "" appuser

# Copy the published application
COPY --from=publish /app/publish .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && \
    chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose the port the app runs on
EXPOSE 8080

# Configure environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check will be handled by the orchestrator via /health endpoint

# Start the application
ENTRYPOINT ["dotnet", "SermonTranscription.Api.dll"] 