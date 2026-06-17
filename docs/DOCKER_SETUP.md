# Docker Setup Guide

This guide covers the complete Docker setup for Task Tracker, including all backend services and the frontend application.

## Architecture Overview

The Task Tracker application runs as a multi-container setup with:

- **Frontend**: React/Vite app served by Nginx (port 3000)
- **API Gateway**: Ocelot gateway (port 8080)
- **Microservices**: 6 backend services (UserService, ProjectService, TaskService, NotificationService, AuditService, ReportingService)
- **Infrastructure**: SQL Server, RabbitMQ, Consul

## Prerequisites

- Docker Desktop for Windows (with WSL2 backend recommended)
- At least 8GB RAM available for Docker
- PostSharp license key (optional, set via environment variable)

## Quick Start

### 1. Build and Start All Services

```powershell
# Start all services (backend + frontend)
docker-compose up -d

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f frontend
docker-compose logs -f gateway
```

### 2. Access the Application

- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:8080
- **Consul UI**: http://localhost:8500
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

### 3. Stop Services

```powershell
# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

## Service Details

### Frontend Service

**Container**: `frontend`
**Port**: 3000 (external) → 80 (internal)
**Technology**: React + Vite + TypeScript, served by Nginx

The frontend is built in two stages:
1. **Build stage**: Uses Node.js 20 Alpine to compile the React app
2. **Runtime stage**: Serves static files via Nginx

**Environment Variables**:
- `VITE_API_BASE_URL`: Points to the API Gateway (http://localhost:8080)

**Health Check**: Available at http://localhost:3000/health

### API Gateway

**Container**: `gateway`
**Port**: 8080
**Technology**: .NET 9 + Ocelot

Routes all API requests to appropriate microservices using Consul for service discovery.

### Backend Services

Each service follows the same pattern:

| Service | Database | Port (internal) |
|---------|----------|-----------------|
| UserService | UserServiceDb | 8080 |
| ProjectService | ProjectServiceDb | 8080 |
| TaskService | TaskServiceDb | 8080 |
| NotificationService | NotificationServiceDb | 8080 |
| AuditService | AuditServiceDb | 8080 |
| ReportingService | ReportingServiceDb | 8080 |

**Common Environment Variables**:
- JWT configuration (shared secret)
- Database connection string
- Consul service registration
- RabbitMQ host

### Infrastructure Services

#### SQL Server
- **Image**: `mcr.microsoft.com/mssql/server:2022-latest`
- **Port**: 1433
- **SA Password**: `Your_password123`
- **Health Check**: TCP connection test every 10s

#### RabbitMQ
- **Image**: `rabbitmq:3-management`
- **Ports**: 5672 (AMQP), 15672 (Management UI)

#### Consul
- **Image**: `hashicorp/consul:1.17`
- **Port**: 8500
- **Mode**: Development agent

## Building Individual Services

### Rebuild Frontend Only

```powershell
docker-compose build frontend
docker-compose up -d frontend
```

### Rebuild Gateway Only

```powershell
docker-compose build gateway
docker-compose up -d gateway
```

### Rebuild Specific Backend Service

```powershell
docker-compose build userservice
docker-compose up -d userservice
```

## Development Workflow

### 1. Frontend Development

For rapid frontend development without Docker:

```powershell
cd takstracker.client
npm install
npm run dev
```

The dev server will run on http://localhost:5173 with hot reload.

**Note**: Update `VITE_API_BASE_URL` in `.env.local` if needed.

### 2. Backend Development

Run only infrastructure and backend services:

```powershell
# Start infrastructure only
docker-compose up -d sqlserver rabbitmq consul

# Or start everything except frontend
docker-compose up -d gateway userservice projectservice taskservice notificationservice auditservice reportingservice
```

Then run services locally using your IDE or `dotnet run`.

## Troubleshooting

### Frontend Container Fails to Start

**Issue**: Build fails or container exits immediately

**Solutions**:
1. Check Node.js dependencies:
   ```powershell
   cd takstracker.client
   npm install
   ```

2. Verify Dockerfile builds locally:
   ```powershell
   docker build -f takstracker.client/Dockerfile -t test-frontend .
   ```

3. Check logs:
   ```powershell
   docker-compose logs frontend
   ```

### SQL Server Health Check Failing

**Issue**: Services can't connect to database

**Solutions**:
1. Wait longer (SQL Server takes ~30s to initialize)
2. Check SQL Server logs:
   ```powershell
   docker-compose logs sqlserver
   ```

3. Restart SQL Server:
   ```powershell
   docker-compose restart sqlserver
   ```

### Consul Service Discovery Issues

**Issue**: Gateway can't find backend services

**Solutions**:
1. Check Consul UI: http://localhost:8500
2. Verify services are registered:
   ```powershell
   curl http://localhost:8500/v1/agent/services
   ```

3. Restart affected services:
   ```powershell
   docker-compose restart gateway userservice
   ```

### Port Already in Use

**Issue**: Port conflict (3000, 8080, 1433, etc.)

**Solutions**:
1. Find what's using the port (Windows):
   ```powershell
   netstat -ano | findstr :3000
   taskkill /PID <PID> /F
   ```

2. Change port in `docker-compose.yml`:
   ```yaml
   ports:
     - "3001:80"  # Use 3001 instead of 3000
   ```

### PostSharp License Issues

**Issue**: Backend services fail with PostSharp license error

**Solution**: Set environment variable before building:
```powershell
$env:PostSharpLicense = "YOUR-LICENSE-KEY"
docker-compose build
docker-compose up -d
```

## Performance Optimization

### Reduce Build Time

1. **Use BuildKit**:
   ```powershell
   $env:DOCKER_BUILDKIT = 1
   docker-compose build
   ```

2. **Build in Parallel**:
   ```powershell
   docker-compose build --parallel
   ```

3. **Cache Dependencies**:
   The Dockerfiles are already optimized to cache dependencies separately from application code.

### Reduce Resource Usage

1. **Limit Service Memory**:
   Add to service definition in `docker-compose.yml`:
   ```yaml
   deploy:
     resources:
       limits:
         memory: 512M
   ```

2. **Run Fewer Services**:
   Start only what you need:
   ```powershell
   docker-compose up -d frontend gateway userservice sqlserver consul
   ```

## Production Considerations

**⚠️ This setup is for development only. For production:**

1. **Security**:
   - Change all default passwords
   - Use secrets management (Docker Secrets, Azure Key Vault)
   - Enable HTTPS/TLS
   - Implement proper authentication

2. **Networking**:
   - Use custom networks with proper isolation
   - Configure firewall rules
   - Use production-ready load balancer

3. **Data Persistence**:
   - Use managed database services (Azure SQL, AWS RDS)
   - Configure backup strategies
   - Use named volumes with proper drivers

4. **Monitoring**:
   - Add health checks for all services
   - Implement logging aggregation (ELK, Seq)
   - Set up application monitoring (Application Insights)

5. **Scaling**:
   - Use orchestration (Kubernetes, Docker Swarm)
   - Configure horizontal scaling
   - Implement circuit breakers and retry policies

## Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Dockerfile Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [Nginx Configuration](https://nginx.org/en/docs/)
- [Consul Service Discovery](https://www.consul.io/docs/discovery/services)

## Related Documentation

- [Build Script Documentation](../README.md#build-script)
- [Architecture Overview](./ARCHITECTURE.md)
- [API Gateway Configuration](../src/Gateway/README.md)
