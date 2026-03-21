---
name: build-publish
description: Build and publish the application for deployment
user_invocable: true
---

# Build & Publish

Build and publish the Feirb application for deployment.

## Steps

### Development Build

```bash
dotnet build Feirb.sln --configuration Release
```

### Publish

```bash
dotnet publish src/Feirb.AppHost --configuration Release --output ./publish
```

### Docker Images (via Aspire)

Aspire can generate Docker images for deployment:

```bash
dotnet publish src/Feirb.AppHost --configuration Release --output ./publish -p:ContainerImageName=mailclient
```

## Report

After building/publishing:
- Report build success/failure
- List any warnings
- Show output location and size
- Note any deployment-specific configuration needed (connection strings, ports)

## NAS Deployment Notes

- Ensure Docker is available on the target NAS
- PostgreSQL data needs a persistent volume mount
- Ollama model data needs a persistent volume mount
- Configure actual IMAP/SMTP settings in production appsettings
- Set PostgreSQL connection string for production
