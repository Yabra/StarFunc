"""Content cache backed by Redis.

Caches game content (manifests, sectors, levels, balance, shop) and
per-player balance with type-specific TTLs.

Key schema (from Server architecture §5.2):

| Key pattern                      | Default TTL |
|----------------------------------|-------------|
| ``content:manifest``             | 5 min       |
| ``content:sector:{id}``          | 10 min      |
| ``content:levels:{id}``          | 10 min      |
| ``content:balance``              | 10 min      |
| ``content:shop``                 | 10 min      |
| ``player:balance:{playerId}``   | 5 min       |
"""

from __future__ import annotations

from typing import Any

from redis.asyncio import Redis

from app.infrastructure.redis import delete_cached, get_cached, set_cached

# Default TTLs in seconds
_DEFAULT_TTL: dict[str, int] = {
    "manifest": 300,  # 5 min
    "sector": 600,  # 10 min
    "levels": 600,  # 10 min
    "balance": 600,  # 10 min
    "shop": 600,  # 10 min
}

_PLAYER_BALANCE_TTL = 300  # 5 min


def _build_key(content_type: str, content_id: str | None = None) -> str:
    if content_type == "player_balance" and content_id:
        return f"player:balance:{content_id}"
    if content_id:
        return f"content:{content_type}:{content_id}"
    return f"content:{content_type}"


async def get_content(
    redis: Redis,
    content_type: str,
    content_id: str | None = None,
) -> dict[str, Any] | None:
    """Retrieve cached content, or *None* on cache miss."""
    key = _build_key(content_type, content_id)
    raw = await get_cached(redis, key)
    if raw is None:
        return None
    return dict(raw)


async def set_content(
    redis: Redis,
    content_type: str,
    data: dict[str, Any],
    content_id: str | None = None,
    ttl: int | None = None,
) -> None:
    """Cache *data* under the appropriate content key.

    If *ttl* is not provided the default for *content_type* is used.
    """
    key = _build_key(content_type, content_id)
    if ttl is None:
        ttl = _PLAYER_BALANCE_TTL if content_type == "player_balance" else _DEFAULT_TTL.get(content_type, 600)
    await set_cached(redis, key, data, ttl=ttl)


async def invalidate_content(
    redis: Redis,
    content_type: str,
    content_id: str | None = None,
) -> None:
    """Remove cached content for the given type (and optional id)."""
    key = _build_key(content_type, content_id)
    await delete_cached(redis, key)
