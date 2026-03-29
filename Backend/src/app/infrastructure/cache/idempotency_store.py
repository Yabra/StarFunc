"""Idempotency-Key store backed by Redis.

Stores and retrieves cached API responses keyed by
``{player_id}:{endpoint}:{idempotency_key}`` to prevent duplicate mutations.
"""

from __future__ import annotations

from typing import Any

from redis.asyncio import Redis

from app.infrastructure.redis import get_cached, set_cached

_PREFIX = "idempotency"


def _build_key(player_id: str, endpoint: str, key: str) -> str:
    return f"{_PREFIX}:{player_id}:{endpoint}:{key}"


async def get_response(redis: Redis, *, player_id: str, endpoint: str, key: str) -> dict[str, Any] | None:
    """Return the previously stored response for an idempotency key, or *None*."""
    full_key = _build_key(player_id, endpoint, key)
    raw = await get_cached(redis, full_key)
    if raw is None:
        return None
    return dict(raw)


async def store_response(
    redis: Redis,
    *,
    player_id: str,
    endpoint: str,
    key: str,
    response: dict[str, Any],
    ttl_hours: int = 24,
) -> None:
    """Persist *response* under the idempotency key with a TTL (default 24 h)."""
    full_key = _build_key(player_id, endpoint, key)
    await set_cached(redis, full_key, response, ttl=ttl_hours * 3600)
