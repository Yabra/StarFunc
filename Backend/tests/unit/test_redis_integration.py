"""Tests for the Redis infrastructure layer (S0.3).

Uses fakeredis so no running Redis instance is required.
"""

from __future__ import annotations

import fakeredis.aioredis
import pytest

from app.infrastructure.cache import content_cache, idempotency_store, rate_limiter
from app.infrastructure.redis import delete_cached, get_cached, set_cached


@pytest.fixture
async def redis():
    """Provide a fresh fakeredis instance per test."""
    r = fakeredis.aioredis.FakeRedis(decode_responses=True)
    yield r
    await r.aclose()


# ── redis.py helpers ──────────────────────────────────────────────


class TestBasicCacheHelpers:
    async def test_set_and_get(self, redis):
        await set_cached(redis, "key1", {"hello": "world"}, ttl=60)
        result = await get_cached(redis, "key1")
        assert result == {"hello": "world"}

    async def test_get_missing_key_returns_none(self, redis):
        assert await get_cached(redis, "nonexistent") is None

    async def test_delete_removes_key(self, redis):
        await set_cached(redis, "del_me", "value", ttl=60)
        await delete_cached(redis, "del_me")
        assert await get_cached(redis, "del_me") is None

    async def test_ttl_is_set(self, redis):
        await set_cached(redis, "ttl_key", 42, ttl=120)
        remaining = await redis.ttl("ttl_key")
        assert 0 < remaining <= 120


# ── idempotency_store.py ─────────────────────────────────────────


class TestIdempotencyStore:
    async def test_store_and_retrieve(self, redis):
        await idempotency_store.store_response(
            redis,
            player_id="p1",
            endpoint="/economy/transaction",
            key="abc-123",
            response={"status": "ok", "data": {"balance": 100}},
        )
        result = await idempotency_store.get_response(
            redis, player_id="p1", endpoint="/economy/transaction", key="abc-123"
        )
        assert result == {"status": "ok", "data": {"balance": 100}}

    async def test_missing_key_returns_none(self, redis):
        result = await idempotency_store.get_response(redis, player_id="p1", endpoint="/foo", key="missing")
        assert result is None

    async def test_different_players_isolated(self, redis):
        resp = {"status": "ok"}
        await idempotency_store.store_response(redis, player_id="p1", endpoint="/x", key="k1", response=resp)
        assert await idempotency_store.get_response(redis, player_id="p2", endpoint="/x", key="k1") is None


# ── rate_limiter.py ───────────────────────────────────────────────


class TestRateLimiter:
    async def test_allows_within_limit(self, redis):
        for _ in range(5):
            assert await rate_limiter.check_rate_limit(
                redis, identifier="player1", endpoint="/auth", limit=5, window=60
            )

    async def test_blocks_after_limit(self, redis):
        for _ in range(3):
            await rate_limiter.check_rate_limit(redis, identifier="player2", endpoint="/auth", limit=3, window=60)
        assert not await rate_limiter.check_rate_limit(
            redis, identifier="player2", endpoint="/auth", limit=3, window=60
        )

    async def test_different_endpoints_independent(self, redis):
        for _ in range(3):
            await rate_limiter.check_rate_limit(redis, identifier="p", endpoint="/a", limit=3, window=60)
        # Endpoint /b should still be allowed
        assert await rate_limiter.check_rate_limit(redis, identifier="p", endpoint="/b", limit=3, window=60)

    async def test_different_identifiers_independent(self, redis):
        for _ in range(2):
            await rate_limiter.check_rate_limit(redis, identifier="x", endpoint="/e", limit=2, window=60)
        assert await rate_limiter.check_rate_limit(redis, identifier="y", endpoint="/e", limit=2, window=60)


# ── content_cache.py ──────────────────────────────────────────────


class TestContentCache:
    async def test_set_and_get_manifest(self, redis):
        data = {"version": "1.0", "sectors": []}
        await content_cache.set_content(redis, "manifest", data)
        assert await content_cache.get_content(redis, "manifest") == data

    async def test_set_and_get_sector(self, redis):
        data = {"id": "s1", "levels": [1, 2, 3]}
        await content_cache.set_content(redis, "sector", data, content_id="s1")
        assert await content_cache.get_content(redis, "sector", content_id="s1") == data

    async def test_get_missing_returns_none(self, redis):
        assert await content_cache.get_content(redis, "manifest") is None

    async def test_invalidate(self, redis):
        await content_cache.set_content(redis, "balance", {"coins": 100})
        await content_cache.invalidate_content(redis, "balance")
        assert await content_cache.get_content(redis, "balance") is None

    async def test_player_balance_key(self, redis):
        await content_cache.set_content(redis, "player_balance", {"fragments": 500}, content_id="player-42")
        result = await content_cache.get_content(redis, "player_balance", content_id="player-42")
        assert result == {"fragments": 500}

    async def test_invalidate_player_balance(self, redis):
        await content_cache.set_content(redis, "player_balance", {"fragments": 500}, content_id="player-42")
        await content_cache.invalidate_content(redis, "player_balance", content_id="player-42")
        assert await content_cache.get_content(redis, "player_balance", content_id="player-42") is None

    async def test_custom_ttl(self, redis):
        await content_cache.set_content(redis, "shop", {"items": []}, ttl=30)
        remaining = await redis.ttl("content:shop")
        assert 0 < remaining <= 30

    async def test_default_ttl_manifest(self, redis):
        await content_cache.set_content(redis, "manifest", {})
        remaining = await redis.ttl("content:manifest")
        assert 0 < remaining <= 300  # 5 min default

    async def test_default_ttl_sector(self, redis):
        await content_cache.set_content(redis, "sector", {}, content_id="s1")
        remaining = await redis.ttl("content:sector:s1")
        assert 0 < remaining <= 600  # 10 min default
