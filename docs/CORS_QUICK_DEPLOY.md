# CORS Fix - Quick Deployment Guide

## Immediate Deployment

To apply the CORS fix immediately:

```powershell
# Option 1: Rebuild Gateway Docker image
docker-compose build gateway
docker-compose up -d gateway

# Option 2: Rebuild from source
dotnet build src/Gateway/Gateway.csproj -c Release
docker-compose build gateway
docker-compose up -d
```

## Verify Fix

1. **Start all services:**
   ```powershell
   docker-compose up -d
   ```

2. **Wait for startup (~30 seconds):**
   ```powershell
   docker-compose ps
   ```

3. **Open frontend:**
   - Navigate to http://localhost:3000
   - Open browser DevTools (F12)

4. **Test API call:**
   ```javascript
   // In browser console
   fetch('http://localhost:8080/health')
     .then(r => r.json())
     .then(data => console.log('✅ CORS works!', data))
     .catch(err => console.error('❌ CORS Error:', err));
   ```

## Expected Result

**Before fix:**
```
❌ CORS policy: No 'Access-Control-Allow-Origin' header
```

**After fix:**
```
✅ CORS works! { status: "Gateway running" }
```

## What Changed

Added to `src/Gateway/Program.cs`:
- `http://localhost:3000` (Docker frontend)
- `http://127.0.0.1:3000` (Docker frontend via IP)

## Rollback (if needed)

If you encounter issues:

```powershell
# Rollback Gateway
git checkout src/Gateway/Program.cs
docker-compose build gateway
docker-compose up -d gateway
```

## Full Documentation

See [CORS_FIX_2026-06-17.md](CORS_FIX_2026-06-17.md) for complete details.
