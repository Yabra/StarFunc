# STAR FUNC — Backend

Backend server for the STAR FUNC mobile game. Provides authentication, cloud saves, economy management, lives system, level validation, shop, content delivery, and analytics.

## Tech Stack

- **Python 3.12+** / **FastAPI** / **Uvicorn** + **Gunicorn**
- **PostgreSQL 16** (via SQLAlchemy 2.0 async)
- **Redis 7** (caching, sessions, rate limiting)
- **Alembic** (migrations)
- **Pydantic v2** (validation & settings)

## Quick Start

```bash
# Copy env template
cp .env.example .env

# Start all services
docker compose up --build

# Swagger UI
open http://localhost:8000/docs
```

## Development

```bash
# Install dependencies
pip install -e ".[dev]"

# Run tests
pytest

# Lint
ruff check src/ tests/
mypy src/
```

## Project Structure

```txt
src/app/
├── main.py            # App factory & lifespan
├── config.py          # Pydantic Settings
├── dependencies.py    # FastAPI Depends
├── domain/            # Domain models & business rules
├── services/          # Use-case layer
├── api/               # Routers, middleware, schemas
└── infrastructure/    # DB, Redis, auth providers
```
