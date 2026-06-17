# Task Tracker Documentation

This directory contains comprehensive documentation for the Task Tracker project.

## 📚 Main Guides

### Getting Started

- **[../README.md](../README.md)** - Main project README with quick start guide
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Command cheat sheet for daily use

### Docker & Deployment

- **[DOCKER_SETUP.md](DOCKER_SETUP.md)** - Complete Docker guide
  - Architecture overview
  - Prerequisites
  - Quick start
  - Service details
  - Troubleshooting
  - Production considerations

### API Documentation

- **[SWAGGER_GENERATION.md](SWAGGER_GENERATION.md)** - Swagger/OpenAPI documentation guide
  - Two-stage approach explanation
  - Build-time XML documentation
  - Runtime Swagger JSON fetching
  - Accessing Swagger UI
  - Troubleshooting

## 🔧 Fix History

Recent fixes and improvements:

- **[ALL_FIXES_SUMMARY.md](ALL_FIXES_SUMMARY.md)** - Complete summary of all fixes (2026-06-17)
- **[CORS_FIX_2026-06-17.md](CORS_FIX_2026-06-17.md)** - CORS configuration fix for Docker frontend
- **[SWAGGER_FIX_2026-06-17.md](SWAGGER_FIX_2026-06-17.md)** - Swagger generation fix details
- **[COMPLETED_2026-06-17.md](COMPLETED_2026-06-17.md)** - PowerShell + Docker fixes summary
- **[FIXES_2026-06-17.md](FIXES_2026-06-17.md)** - Detailed technical fix history

## 📖 Reference Documentation

- **[csharp-mechanisms.md](csharp-mechanisms.md)** - C# mechanisms and patterns used in the project

## 🚀 Quick Navigation

### I want to...

#### ...start the application
→ [DOCKER_SETUP.md - Quick Start](DOCKER_SETUP.md#quick-start)

#### ...build the project locally
→ [QUICK_REFERENCE.md - PowerShell Build Script](QUICK_REFERENCE.md#powershell-build-script)

#### ...run tests
```powershell
.\build.ps1 -Target Test
```

#### ...generate API documentation
→ [SWAGGER_GENERATION.md](SWAGGER_GENERATION.md)

#### ...troubleshoot Docker issues
→ [DOCKER_SETUP.md - Troubleshooting](DOCKER_SETUP.md#troubleshooting)

#### ...see all available commands
→ [QUICK_REFERENCE.md](QUICK_REFERENCE.md)

#### ...understand recent changes
→ [ALL_FIXES_SUMMARY.md](ALL_FIXES_SUMMARY.md)

## 📋 Build Script Targets

```powershell
.\build.ps1                      # Run all (Clean + Build + Test + Docs)
.\build.ps1 -Target Clean        # Clean artifacts
.\build.ps1 -Target Build        # Restore + Build
.\build.ps1 -Target Test         # Restore + Build + Test
.\build.ps1 -Target Docs         # Restore + Build + Copy XML docs
.\build.ps1 -Target FetchSwagger # Fetch Swagger JSON from running services
.\build.ps1 -Target All          # Full pipeline
```

## 🌐 Service URLs

After starting with `docker-compose up -d`:

| Service | URL | Credentials |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | admin@tasktracker.local / Admin123! |
| **API Gateway** | http://localhost:8080 | - |
| **Consul UI** | http://localhost:8500 | - |
| **RabbitMQ Management** | http://localhost:15672 | guest / guest |

### Swagger UIs

- http://localhost:8080/api/users/swagger
- http://localhost:8080/api/projects/swagger
- http://localhost:8080/api/tasks/swagger
- http://localhost:8080/api/notifications/swagger
- http://localhost:8080/api/audit/swagger
- http://localhost:8080/api/reports/swagger

## 🔍 Document Summary

| Document | Type | Purpose | Size |
|----------|------|---------|------|
| DOCKER_SETUP.md | Guide | Complete Docker reference | 8.1 KB |
| SWAGGER_GENERATION.md | Guide | API documentation generation | 8.0 KB |
| QUICK_REFERENCE.md | Reference | Command cheat sheet | 6.1 KB |
| ALL_FIXES_SUMMARY.md | Summary | Complete fix overview | 8.7 KB |
| CORS_FIX_2026-06-17.md | Fix Log | CORS configuration fix | 7.9 KB |
| SWAGGER_FIX_2026-06-17.md | Fix Log | Swagger fix details | 6.7 KB |
| COMPLETED_2026-06-17.md | Fix Log | Initial fixes summary | 5.0 KB |
| FIXES_2026-06-17.md | Fix Log | Technical fix history | 4.5 KB |
| csharp-mechanisms.md | Reference | C# patterns used | 2.6 KB |

**Total documentation:** ~57.6 KB across 9 files

## 📝 Documentation Standards

All documentation in this directory follows these principles:

1. **Clear Structure**: Hierarchical organization with table of contents
2. **Practical Examples**: Real commands and code snippets
3. **Troubleshooting**: Common issues with solutions
4. **Cross-References**: Links to related documentation
5. **Up-to-Date**: Last updated dates and version information

## 🤝 Contributing

When adding new documentation:

1. Use clear, descriptive filenames
2. Include a summary section at the top
3. Add cross-references to related docs
4. Update this README with the new document
5. Include practical examples
6. Add troubleshooting sections where applicable

## 📅 Recent Updates

**2026-06-17:**
- ✅ Fixed PowerShell script encoding issues
- ✅ Verified Docker frontend integration
- ✅ Fixed Swagger documentation generation
- ✅ Fixed CORS configuration for Docker frontend
- ✅ Added comprehensive Docker guide
- ✅ Added Swagger generation guide
- ✅ Created quick reference guide
- ✅ Created detailed fix history

## 🆘 Need Help?

1. **Check Quick Reference**: [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
2. **Check Troubleshooting**: 
   - Docker issues: [DOCKER_SETUP.md#troubleshooting](DOCKER_SETUP.md#troubleshooting)
   - Swagger issues: [SWAGGER_GENERATION.md#troubleshooting](SWAGGER_GENERATION.md#troubleshooting)
3. **Review Fix History**: [ALL_FIXES_SUMMARY.md](ALL_FIXES_SUMMARY.md)
4. **Check Main README**: [../README.md](../README.md)

---

**Last updated:** 2026-06-17
