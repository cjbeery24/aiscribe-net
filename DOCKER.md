# Docker Deployment Guide

This guide covers how to deploy the Sermon Transcription API using Docker containers.

## 📋 Prerequisites

- Docker Engine 20.10+
- Docker Compose 2.0+
- For production: SSL certificates and properly configured domain

## 🚀 Quick Start (Development)

### 1. Basic Development Setup

```bash
# Start all services (PostgreSQL, Redis, API)
docker-compose up -d

# View logs
docker-compose logs -f sermon-transcription-api

# Stop all services
docker-compose down
```

The API will be available at: `http://localhost:5000`

### 2. Development with Database Only

If you prefer to run the API locally but use containerized databases:

```bash
# Start only databases
docker-compose up -d postgres redis

# Run API locally
cd SermonTranscription.Api
dotnet run
```

### 3. Include PgAdmin for Database Management

```bash
# Start with PgAdmin
docker-compose --profile dev up -d

# Access PgAdmin at: http://localhost:5050
# Email: dev@example.com
# Password: devpassword
```

## 🏭 Production Deployment

### 1. Prepare Environment Variables

```bash
# Copy and customize environment variables
cp docker.env.example .env

# Edit .env with your production values
nano .env
```

### 2. SSL Certificates

Create SSL certificates for HTTPS:

```bash
# Create SSL directory
mkdir -p ssl

# Place your SSL certificates
# ssl/cert.pem    - SSL certificate
# ssl/key.pem     - Private key
```

### 3. Deploy Production Stack

```bash
# Deploy with production configuration
docker-compose -f docker-compose.prod.yml up -d

# Monitor deployment
docker-compose -f docker-compose.prod.yml logs -f
```

The API will be available at: `https://yourdomain.com`

## 🔧 Configuration

### Environment Variables

#### Required Variables

| Variable                     | Description                 | Example                                                                     |
| ---------------------------- | --------------------------- | --------------------------------------------------------------------------- |
| `DATABASE_CONNECTION_STRING` | PostgreSQL connection       | `Host=postgres;Database=SermonTranscription;Username=postgres;Password=...` |
| `JWT_SECRET_KEY`             | JWT signing key (32+ chars) | `your-super-secret-jwt-key-32-chars-min`                                    |
| `REDIS_CONNECTION_STRING`    | Redis connection            | `redis:6379`                                                                |

#### Optional Variables

| Variable                 | Default       | Description       |
| ------------------------ | ------------- | ----------------- |
| `API_PORT`               | `5000`        | External API port |
| `JWT_EXPIRATION_MINUTES` | `15`          | JWT token expiry  |
| `LOG_LEVEL`              | `Information` | Logging level     |

### Port Mapping

| Service    | Internal Port | External Port | Purpose                  |
| ---------- | ------------- | ------------- | ------------------------ |
| API        | 8080          | 5000          | HTTP API                 |
| PostgreSQL | 5432          | 5432          | Database (dev only)      |
| Redis      | 6379          | 6379          | Cache (dev only)         |
| PgAdmin    | 80            | 5050          | DB Management (dev only) |
| Nginx      | 80/443        | 80/443        | Reverse Proxy (prod)     |

## 🛠️ Docker Commands

### Building and Running

```bash
# Build the API image
docker build -t sermon-transcription-api .

# Run a single container
docker run -p 5000:8080 sermon-transcription-api

# Rebuild and restart services
docker-compose up -d --build

# Scale the API service
docker-compose up -d --scale sermon-transcription-api=3
```

### Monitoring and Debugging

```bash
# View logs for all services
docker-compose logs

# View logs for specific service
docker-compose logs -f sermon-transcription-api

# Execute commands in container
docker-compose exec sermon-transcription-api bash

# Check container health
docker-compose ps

# Monitor resource usage
docker stats
```

### Database Operations

```bash
# Access PostgreSQL shell
docker-compose exec postgres psql -U postgres -d SermonTranscriptionDb_Dev

# Backup database
docker-compose exec postgres pg_dump -U postgres SermonTranscriptionDb_Dev > backup.sql

# Restore database
docker-compose exec -T postgres psql -U postgres -d SermonTranscriptionDb_Dev < backup.sql

# Reset database (WARNING: destructive)
docker-compose down -v
docker-compose up -d
```

## 🔒 Security Considerations

### Development Security

- Default passwords are used (change for production)
- Database ports are exposed (for development tools)
- Detailed error messages enabled

### Production Security

- All passwords should be secure and unique
- Database ports are not exposed to host
- SSL/TLS encryption enforced
- Security headers configured in nginx
- Rate limiting enabled
- Non-root user in containers

### Secret Management

Never store secrets in Docker images or compose files. Use:

1. Environment variables from secure sources
2. Docker secrets (Docker Swarm)
3. External secret management (Azure Key Vault, AWS Secrets Manager)
4. Init containers for secret retrieval

## 📊 Monitoring and Logging

### Health Checks

The API includes built-in health checks:

```bash
# Check API health
curl http://localhost:5000/health

# Check container health
docker-compose ps
```

### Log Aggregation

Logs are available via Docker:

```bash
# Stream all logs
docker-compose logs -f

# Export logs
docker-compose logs > application.log
```

### Metrics (Future Enhancement)

Consider adding:

- Prometheus metrics
- Grafana dashboards
- Application Performance Monitoring (APM)

## 🚨 Troubleshooting

### Common Issues

#### Container Won't Start

```bash
# Check logs
docker-compose logs sermon-transcription-api

# Common issues:
# - Missing environment variables
# - Database connection failure
# - Port conflicts
```

#### Database Connection Issues

```bash
# Ensure PostgreSQL is healthy
docker-compose ps postgres

# Check connection string format
# Correct: Host=postgres;Database=...
# Wrong: Server=postgres;Database=...
```

#### Permission Issues

```bash
# Check container user
docker-compose exec sermon-transcription-api whoami

# Check file permissions
docker-compose exec sermon-transcription-api ls -la /app
```

### Performance Tuning

#### Resource Limits

Adjust resource limits in production:

```yaml
deploy:
  resources:
    limits:
      cpus: "2.0"
      memory: 1G
    reservations:
      cpus: "1.0"
      memory: 512M
```

#### Database Performance

```bash
# Monitor PostgreSQL performance
docker-compose exec postgres psql -U postgres -c "
  SELECT pid, usename, application_name, state, query
  FROM pg_stat_activity
  WHERE state = 'active';
"
```

## 🔄 Updates and Maintenance

### Updating the Application

```bash
# Pull latest changes
git pull

# Rebuild and restart
docker-compose up -d --build

# Or for production
docker-compose -f docker-compose.prod.yml up -d --build
```

### Database Migrations

```bash
# Run migrations manually if needed
docker-compose exec sermon-transcription-api dotnet ef database update
```

### Backup Strategy

```bash
#!/bin/bash
# backup.sh - Production backup script

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups"

# Backup database
docker-compose exec postgres pg_dump -U postgres SermonTranscriptionDb_Prod > "${BACKUP_DIR}/db_${DATE}.sql"

# Backup application logs
docker-compose logs sermon-transcription-api > "${BACKUP_DIR}/logs_${DATE}.log"

# Cleanup old backups (keep 30 days)
find "${BACKUP_DIR}" -name "*.sql" -mtime +30 -delete
find "${BACKUP_DIR}" -name "*.log" -mtime +30 -delete
```

## 🌐 Multi-Environment Setup

### Staging Environment

```bash
# Use staging override
docker-compose -f docker-compose.yml -f docker-compose.staging.yml up -d
```

### Load Balancing

For high-availability production:

```yaml
# docker-compose.prod.yml
services:
  sermon-transcription-api:
    deploy:
      replicas: 3
      update_config:
        parallelism: 1
        delay: 10s
      restart_policy:
        condition: on-failure
```

## 📞 Support

For deployment issues:

1. Check this documentation
2. Review container logs
3. Verify environment variables
4. Check network connectivity between containers
5. Ensure required ports are available

Remember to never expose sensitive information in logs or error messages in production environments.
