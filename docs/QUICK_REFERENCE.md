# Quick Reference - Build & Docker Commands

## PowerShell Build Script

### Basic Usage

```powershell
# Build everything (default)
.\build.ps1

# Specific targets
.\build.ps1 -Target Clean      # Clean artifacts
.\build.ps1 -Target Build      # Build solution
.\build.ps1 -Target Test       # Build + run tests
.\build.ps1 -Target Docs       # Build + copy XML docs
.\build.ps1 -Target FetchSwagger  # Fetch Swagger JSON from running services
.\build.ps1 -Target All        # Clean + Build + Test + Docs
```

### Get Help

```powershell
Get-Help .\build.ps1
Get-Help .\build.ps1 -Examples
Get-Help .\build.ps1 -Detailed
```

### Swagger Documentation

```powershell
# Get XML documentation (build time)
.\build.ps1 -Target Docs

# Get Swagger JSON (requires running services)
docker-compose up -d
Start-Sleep -Seconds 30
.\build.ps1 -Target FetchSwagger

# Access Swagger UI
# http://localhost:8080/api/users/swagger
# http://localhost:8080/api/projects/swagger
# (etc for other services)
```

See [SWAGGER_GENERATION.md](SWAGGER_GENERATION.md) for details.

## Docker Commands

### Start/Stop Services

```powershell
# Start all services (first time - builds images)
docker-compose up --build -d

# Start all services (subsequent runs)
docker-compose up -d

# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

### View Logs

```powershell
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f frontend
docker-compose logs -f gateway
docker-compose logs -f userservice
```

### Service Status

```powershell
# List running services
docker-compose ps

# List all services (including stopped)
docker-compose ps -a
```

### Rebuild Services

```powershell
# Rebuild specific service
docker-compose build frontend
docker-compose up -d frontend

# Rebuild all services
docker-compose build
docker-compose up -d

# Rebuild without cache
docker-compose build --no-cache
```

### Individual Service Control

```powershell
# Start specific service
docker-compose up -d frontend

# Stop specific service
docker-compose stop frontend

# Restart specific service
docker-compose restart frontend

# Remove specific service container
docker-compose rm -f frontend
```

### Development Workflow

```powershell
# Start only infrastructure
docker-compose up -d sqlserver rabbitmq consul

# Start infrastructure + gateway
docker-compose up -d sqlserver rabbitmq consul gateway

# Start everything except frontend (for frontend dev)
docker-compose up -d gateway userservice projectservice taskservice notificationservice auditservice reportingservice
```

### Cleanup

```powershell
# Remove all stopped containers
docker-compose rm -f

# Remove unused images
docker image prune -f

# Remove unused volumes
docker volume prune -f

# Full cleanup (nuclear option)
docker system prune -a --volumes
```

## Service URLs

After running `docker-compose up -d`:

| Service | URL | Credentials |
|---------|-----|-------------|
| Frontend | http://localhost:3000 | admin@tasktracker.local / Admin123! |
| API Gateway | http://localhost:8080 | - |
| Consul UI | http://localhost:8500 | - |
| RabbitMQ Management | http://localhost:15672 | guest / guest |
| SQL Server | localhost:1433 | sa / Your_password123 |

## Health Checks

```powershell
# Gateway health
curl http://localhost:8080/health

# Frontend health
curl http://localhost:3000/health

# Consul health
curl http://localhost:8500/v1/status/leader
```

## Troubleshooting Quick Fixes

### Port Already in Use

```powershell
# Find process using port (Windows)
netstat -ano | findstr :3000

# Kill process (replace PID)
taskkill /PID <PID> /F
```

### Service Won't Start

```powershell
# Check logs
docker-compose logs <service-name>

# Restart with fresh build
docker-compose rm -f <service-name>
docker-compose build --no-cache <service-name>
docker-compose up -d <service-name>
```

### Database Connection Issues

```powershell
# Wait for SQL Server (takes ~30 seconds)
docker-compose logs sqlserver

# Restart SQL Server
docker-compose restart sqlserver

# Check SQL Server health
docker exec -it task_tracker-sqlserver-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Your_password123 -Q "SELECT @@VERSION"
```

### Clear All Docker State

```powershell
# Nuclear option - removes everything
docker-compose down -v
docker system prune -a --volumes -f
docker-compose up --build -d
```

## Development Tips

### Frontend Hot Reload (without Docker)

```powershell
cd takstracker.client
npm install
npm run dev
# Access at http://localhost:5173
```

### Backend Local Development

```powershell
# Start only infrastructure
docker-compose up -d sqlserver rabbitmq consul

# Run service from IDE or:
cd src/Services/UserService
dotnet run
```

### Performance

```powershell
# Enable BuildKit for faster builds
$env:DOCKER_BUILDKIT = 1
docker-compose build

# Build in parallel
docker-compose build --parallel
```

## Common Workflows

### Daily Development Start

```powershell
# Start everything
docker-compose up -d

# Check status
docker-compose ps

# View logs if needed
docker-compose logs -f
```

### After Code Changes

```powershell
# Backend changes - rebuild affected service
docker-compose build userservice
docker-compose up -d userservice

# Frontend changes
docker-compose build frontend
docker-compose up -d frontend
```

### End of Day

```powershell
# Stop all services (keeps data)
docker-compose down

# Or stop and clean data
docker-compose down -v
```

### Full Reset

```powershell
# Complete clean start
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

## More Information

- **Detailed Docker Guide:** [docs/DOCKER_SETUP.md](DOCKER_SETUP.md)
- **Project README:** [../README.md](../README.md)
- **Recent Fixes:** [FIXES_2026-06-17.md](FIXES_2026-06-17.md)
