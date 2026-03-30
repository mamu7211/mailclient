# Phase 5: Deployment & Production

## Overview

Container packaging and deployment infrastructure for Feirb. Production-ready Docker image, tiered docker-compose stacks for self-hosted NAS deployment, and CI/CD pipeline for automated image builds.

**Depends on:** Phase 3 (functional mail client), Phase 4 recommended (AI features)

## Features (in implementation order)

| # | Feature | Issue | Description |
|---|---------|-------|-------------|
| 1 | [Docker + CI/CD](01-docker-cicd.md) | #158 | Dockerfile, three compose tiers, GitHub Actions release workflow |
| 2 | Graceful Ollama Degradation | #169 | API starts without Ollama, AI features degrade instead of crashing |

## Dependencies

- Feature 2 is recommended before Feature 1 (Tier 1/2 compose stacks need graceful degradation)
- Both features can be worked in parallel if needed
