# Swagger Documentation Generation

## Overview

The Task Tracker project uses Swagger/OpenAPI for API documentation. Due to .NET version compatibility issues with the Swashbuckle CLI tool, the build script uses a two-stage approach for documentation generation.

## The Problem

The Swashbuckle CLI tool version 6.9.0 requires .NET 8 runtime, but our project uses .NET 9. While a newer version (10.2.1) exists, the most reliable approach is to fetch Swagger JSON directly from running services, which ensures the documentation matches the actual runtime behavior.

## Solution: Two-Stage Documentation

### Stage 1: XML Documentation (Build Time)

The `.\build.ps1 -Target Docs` command copies XML documentation files from compiled assemblies:

```powershell
.\build.ps1 -Target Docs
```

**What it does:**
- Builds the solution in Release mode
- Copies XML documentation files to `docs/swagger/`
- Provides instructions for fetching Swagger JSON

**Output:**
- `docs/swagger/UserService.xml`
- `docs/swagger/ProjectService.xml`
- `docs/swagger/TaskService.xml`
- `docs/swagger/NotificationService.xml`
- `docs/swagger/AuditService.xml`
- `docs/swagger/ReportingService.xml`

### Stage 2: Swagger JSON (Runtime)

The `.\build.ps1 -Target FetchSwagger` command fetches Swagger JSON from running services:

```powershell
# First, start the services
docker-compose up -d

# Wait for services to be healthy (~30 seconds)
docker-compose ps

# Then fetch Swagger JSON
.\build.ps1 -Target FetchSwagger
```

**What it does:**
- Checks if API Gateway is running (http://localhost:8080)
- Fetches Swagger JSON from each service endpoint
- Saves files to `docs/swagger/`

**Output:**
- `docs/swagger/UserService-swagger.json`
- `docs/swagger/ProjectService-swagger.json`
- `docs/swagger/TaskService-swagger.json`
- `docs/swagger/NotificationService-swagger.json`
- `docs/swagger/AuditService-swagger.json`
- `docs/swagger/ReportingService-swagger.json`

## Usage Examples

### Get XML Documentation Only

```powershell
# Build and copy XML docs
.\build.ps1 -Target Build
.\build.ps1 -Target Docs
```

### Get Complete Documentation (XML + Swagger JSON)

```powershell
# Build and get XML docs
.\build.ps1 -Target Docs

# Start services in Docker
docker-compose up -d

# Wait for services to be ready
Start-Sleep -Seconds 30

# Fetch Swagger JSON
.\build.ps1 -Target FetchSwagger
```

### One-Line Complete Documentation

```powershell
# PowerShell one-liner
.\build.ps1 -Target Docs; docker-compose up -d; Start-Sleep 30; .\build.ps1 -Target FetchSwagger
```

## Accessing Swagger UI

Each service also exposes a Swagger UI for interactive documentation:

### Through API Gateway (Recommended)

Access through the gateway on port 8080:

- **UserService**: http://localhost:8080/api/users/swagger
- **ProjectService**: http://localhost:8080/api/projects/swagger
- **TaskService**: http://localhost:8080/api/tasks/swagger
- **NotificationService**: http://localhost:8080/api/notifications/swagger
- **AuditService**: http://localhost:8080/api/audit/swagger
- **ReportingService**: http://localhost:8080/api/reports/swagger

### Direct Service Access (Docker)

Services are not exposed individually by default, but you can modify `docker-compose.yml` to expose them:

```yaml
userservice:
  ports:
    - "5001:8080"
```

Then access: http://localhost:5001/swagger

## Manual Swagger JSON Download

If the build script doesn't work, you can manually download Swagger JSON:

### Using PowerShell

```powershell
# Create directory
New-Item -ItemType Directory -Force -Path docs/swagger

# Download each service
$services = @("users", "projects", "tasks", "notifications", "audit", "reports")
foreach ($svc in $services) {
    Invoke-WebRequest `
        -Uri "http://localhost:8080/api/$svc/swagger/v1/swagger.json" `
        -OutFile "docs/swagger/$svc-swagger.json"
}
```

### Using curl

```bash
# Linux/Mac/Windows (with curl)
mkdir -p docs/swagger
curl http://localhost:8080/api/users/swagger/v1/swagger.json > docs/swagger/UserService-swagger.json
curl http://localhost:8080/api/projects/swagger/v1/swagger.json > docs/swagger/ProjectService-swagger.json
curl http://localhost:8080/api/tasks/swagger/v1/swagger.json > docs/swagger/TaskService-swagger.json
curl http://localhost:8080/api/notifications/swagger/v1/swagger.json > docs/swagger/NotificationService-swagger.json
curl http://localhost:8080/api/audit/swagger/v1/swagger.json > docs/swagger/AuditService-swagger.json
curl http://localhost:8080/api/reports/swagger/v1/swagger.json > docs/swagger/ReportingService-swagger.json
```

## Troubleshooting

### Error: "API Gateway nie jest dostepny"

**Problem:** The `FetchSwagger` target can't reach the API Gateway.

**Solutions:**

1. Check if Docker services are running:
   ```powershell
   docker-compose ps
   ```

2. Start services if not running:
   ```powershell
   docker-compose up -d
   ```

3. Wait for services to initialize (30-60 seconds):
   ```powershell
   docker-compose logs gateway
   ```

4. Test gateway manually:
   ```powershell
   curl http://localhost:8080/health
   ```

### Error: "BLAD: 404 Not Found"

**Problem:** A service is not registered with the gateway or its Swagger endpoint is different.

**Solutions:**

1. Check Consul to see registered services:
   - Open http://localhost:8500
   - Verify all services are listed

2. Check service logs:
   ```powershell
   docker-compose logs userservice
   ```

3. Verify the endpoint path by accessing Swagger UI directly

### Partial Download Success

**Problem:** Some services downloaded successfully, others failed.

**Solutions:**

1. Check which services failed:
   ```powershell
   ls docs/swagger/*.json
   ```

2. Restart failed services:
   ```powershell
   docker-compose restart userservice
   ```

3. Retry after services stabilize:
   ```powershell
   Start-Sleep -Seconds 10
   .\build.ps1 -Target FetchSwagger
   ```

## Alternative: Using Swashbuckle CLI with .NET 8

If you need to generate Swagger JSON at build time without running services, you can:

1. Install .NET 8 SDK alongside .NET 9
2. Install compatible Swashbuckle CLI:
   ```powershell
   dotnet tool install -g Swashbuckle.AspNetCore.Cli --version 6.9.0
   ```

3. Manually generate (requires .NET 8 runtime):
   ```powershell
   swagger tofile --output docs/swagger/UserService-swagger.json `
       src/Services/UserService/bin/Release/net9.0/UserService.dll v1
   ```

**Note:** This approach is not recommended as it requires maintaining multiple .NET SDK versions.

## Why Not Use Swashbuckle CLI?

### Issues with Swashbuckle CLI

1. **Version Compatibility**: 
   - Version 6.9.0 requires .NET 8
   - Version 10.2.1 supports .NET 9 but may have compatibility issues

2. **Runtime Dependency**:
   - CLI tool needs to load the compiled DLL
   - Requires matching runtime version
   - May not work with AOP frameworks (PostSharp)

3. **Build-Time Limitations**:
   - Generated at compile time, not runtime
   - May not reflect actual API behavior with middleware
   - Doesn't include runtime-configured options

### Advantages of Runtime Generation

1. **Always Accurate**: Documentation matches actual running code
2. **No CLI Dependencies**: No need to install/maintain external tools
3. **Environment Specific**: Can generate docs for different environments
4. **Framework Agnostic**: Works regardless of .NET version or frameworks used

## References

- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [OpenAPI Specification](https://swagger.io/specification/)
- [PowerShell Invoke-WebRequest](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/invoke-webrequest)
