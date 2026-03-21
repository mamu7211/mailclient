---
name: aspire-run
description: Start the Aspire AppHost which orchestrates all services
user_invocable: true
---

# Run Aspire AppHost

Start the Feirb application via .NET Aspire. This is the primary way to run all services during development.

## Steps

1. Run the Aspire AppHost:
   ```bash
   dotnet run --project src/Feirb.AppHost
   ```

2. Report the service URLs once started:
   - **Aspire Dashboard:** https://localhost:18888
   - **Blazor Frontend:** https://localhost:7100
   - **API Backend:** https://localhost:7200
   - **Mailpit:** http://localhost:8025

3. If the command fails, check:
   - Docker is running (`docker info`)
   - No port conflicts on 7100, 7200, 8025, 11434, 18888
   - .NET 10 SDK is installed (`dotnet --version`)

4. On first run, note that the Ollama qwen3:4b model download (~2.6GB) may take several minutes.
