"""Tests for middleware pipeline — S1.3."""

import json
import time
from unittest.mock import AsyncMock
from uuid import uuid4

import pytest
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from httpx import ASGITransport, AsyncClient
from starlette.responses import PlainTextResponse

from app.api.middleware import (
    ClientInfoMiddleware,
    ExceptionHandlerMiddleware,
    IdempotencyMiddleware,
    RateLimitingMiddleware,
    RequestLoggingMiddleware,
    ServerTimeMiddleware,
)
from app.config import Settings
from app.domain.exceptions import (
    AppError,
    ConflictError,
    ForbiddenError,
    InsufficientFundsError,
    NoLivesError,
    NotFoundError,
)


def _make_settings() -> Settings:
    return Settings(
        _env_file=None,  # type: ignore[call-arg]
        database_url="postgresql+asyncpg://u:p@localhost/db",
        jwt_secret="test-secret-key-that-is-long-enough-for-validation",
    )


def _make_app(
    *,
    with_exception: bool = False,
    with_logging: bool = False,
    with_rate_limit: bool = False,
    with_client_info: bool = False,
    with_idempotency: bool = False,
    with_server_time: bool = False,
) -> FastAPI:
    """Build a minimal FastAPI app with selected middleware."""
    app = FastAPI()

    settings = _make_settings()
    app.state.settings = settings

    # Mock Redis
    mock_redis = AsyncMock()
    mock_redis.incr = AsyncMock(return_value=1)
    mock_redis.expire = AsyncMock()
    mock_redis.ttl = AsyncMock(return_value=42)
    mock_redis.get = AsyncMock(return_value=None)
    mock_redis.set = AsyncMock()
    app.state.redis = mock_redis

    if with_logging:
        app.add_middleware(RequestLoggingMiddleware)
    if with_exception:
        app.add_middleware(ExceptionHandlerMiddleware)
    if with_rate_limit:
        app.add_middleware(RateLimitingMiddleware)
    if with_client_info:
        app.add_middleware(ClientInfoMiddleware)
    if with_idempotency:
        app.add_middleware(IdempotencyMiddleware)
    if with_server_time:
        app.add_middleware(ServerTimeMiddleware)

    return app


# ─── Domain exceptions ───────────────────────────────────────────────


class TestDomainExceptions:
    def test_app_error_fields(self) -> None:
        err = AppError(code="TEST", message="boom", status_code=418, details={"x": 1})
        assert err.code == "TEST"
        assert err.message == "boom"
        assert err.status_code == 418
        assert err.details == {"x": 1}
        assert str(err) == "boom"

    def test_not_found_defaults(self) -> None:
        err = NotFoundError()
        assert err.code == "NOT_FOUND"
        assert err.status_code == 404

    def test_conflict_defaults(self) -> None:
        err = ConflictError()
        assert err.status_code == 409

    def test_forbidden_defaults(self) -> None:
        err = ForbiddenError()
        assert err.status_code == 403

    def test_insufficient_funds_defaults(self) -> None:
        err = InsufficientFundsError(details={"required": 50, "available": 30})
        assert err.code == "INSUFFICIENT_FUNDS"
        assert err.status_code == 422
        assert err.details == {"required": 50, "available": 30}

    def test_no_lives_defaults(self) -> None:
        err = NoLivesError()
        assert err.code == "NO_LIVES"
        assert err.status_code == 422


# ─── ExceptionHandlerMiddleware ──────────────────────────────────────


class TestExceptionHandler:
    @pytest.mark.anyio
    async def test_app_error_converted(self) -> None:
        app = _make_app(with_exception=True)

        @app.get("/boom")
        async def boom() -> None:
            raise NotFoundError("Player not found", details={"id": "abc"})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.get("/boom")
        assert resp.status_code == 404
        body = resp.json()
        assert body["status"] == "error"
        assert body["error"]["code"] == "NOT_FOUND"
        assert body["error"]["details"]["id"] == "abc"
        assert "serverTime" in body

    @pytest.mark.anyio
    async def test_unhandled_exception_500(self) -> None:
        app = _make_app(with_exception=True)

        @app.get("/crash")
        async def crash() -> None:
            raise RuntimeError("oops")

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.get("/crash")
        assert resp.status_code == 500
        body = resp.json()
        assert body["error"]["code"] == "INTERNAL_ERROR"

    @pytest.mark.anyio
    async def test_insufficient_funds_422(self) -> None:
        app = _make_app(with_exception=True)

        @app.get("/poor")
        async def poor() -> None:
            raise InsufficientFundsError(details={"required": 100, "available": 5})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.get("/poor")
        assert resp.status_code == 422
        assert resp.json()["error"]["code"] == "INSUFFICIENT_FUNDS"


# ─── ClientInfoMiddleware ────────────────────────────────────────────


class TestClientInfo:
    @pytest.mark.anyio
    async def test_extracts_headers(self) -> None:
        app = _make_app(with_client_info=True)
        captured: dict = {}

        @app.get("/info")
        async def info(request: Request) -> JSONResponse:
            captured["version"] = request.state.client_version
            captured["platform"] = request.state.platform
            return JSONResponse({"ok": True})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            await client.get("/info", headers={"X-Client-Version": "1.2.3", "X-Platform": "android"})
        assert captured["version"] == "1.2.3"
        assert captured["platform"] == "android"

    @pytest.mark.anyio
    async def test_missing_headers_none(self) -> None:
        app = _make_app(with_client_info=True)
        captured: dict = {}

        @app.get("/info")
        async def info(request: Request) -> JSONResponse:
            captured["version"] = request.state.client_version
            captured["platform"] = request.state.platform
            return JSONResponse({"ok": True})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            await client.get("/info")
        assert captured["version"] is None
        assert captured["platform"] is None


# ─── ServerTimeMiddleware ────────────────────────────────────────────


class TestServerTime:
    @pytest.mark.anyio
    async def test_injects_server_time(self) -> None:
        app = _make_app(with_server_time=True)

        @app.get("/ping")
        async def ping() -> JSONResponse:
            return JSONResponse({"status": "ok", "data": {}})

        before = int(time.time())
        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.get("/ping")
        after = int(time.time())
        body = resp.json()
        assert "serverTime" in body
        assert before <= body["serverTime"] <= after

    @pytest.mark.anyio
    async def test_non_json_untouched(self) -> None:
        app = _make_app(with_server_time=True)

        @app.get("/plain")
        async def plain() -> PlainTextResponse:
            return PlainTextResponse("hello")

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.get("/plain")
        assert resp.text == "hello"


# ─── RateLimitingMiddleware ──────────────────────────────────────────


class TestRateLimiting:
    @pytest.mark.anyio
    async def test_allows_within_limit(self) -> None:
        app = _make_app(with_rate_limit=True)

        @app.post("/api/v1/auth/register")
        async def register() -> JSONResponse:
            return JSONResponse({"status": "ok"})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.post("/api/v1/auth/register")
        assert resp.status_code == 200

    @pytest.mark.anyio
    async def test_blocks_over_limit(self) -> None:
        app = _make_app(with_rate_limit=True)
        # Make redis.incr return a value over the limit
        app.state.redis.incr = AsyncMock(return_value=999)

        @app.post("/api/v1/auth/register")
        async def register() -> JSONResponse:
            return JSONResponse({"status": "ok"})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.post("/api/v1/auth/register")
        assert resp.status_code == 429
        body = resp.json()
        assert body["error"]["code"] == "RATE_LIMITED"
        assert "Retry-After" in resp.headers

    @pytest.mark.anyio
    async def test_non_api_path_not_limited(self) -> None:
        app = _make_app(with_rate_limit=True)
        app.state.redis.incr = AsyncMock(return_value=999)

        @app.get("/docs")
        async def docs() -> JSONResponse:
            return JSONResponse({"status": "ok"})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.get("/docs")
        assert resp.status_code == 200


# ─── IdempotencyMiddleware ───────────────────────────────────────────


class TestIdempotency:
    @pytest.mark.anyio
    async def test_no_key_passes_through(self) -> None:
        app = _make_app(with_idempotency=True)

        @app.post("/api/v1/save")
        async def save() -> JSONResponse:
            return JSONResponse({"status": "ok", "data": {}})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.post("/api/v1/save")
        assert resp.status_code == 200

    @pytest.mark.anyio
    async def test_get_ignored(self) -> None:
        app = _make_app(with_idempotency=True)

        @app.get("/api/v1/save")
        async def get_save() -> JSONResponse:
            return JSONResponse({"status": "ok", "data": {}})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.get(
                "/api/v1/save",
                headers={"Idempotency-Key": str(uuid4())},
            )
        assert resp.status_code == 200

    @pytest.mark.anyio
    async def test_cached_response_returned(self) -> None:
        app = _make_app(with_idempotency=True)
        # Simulate a cached response stored in Redis
        cached = json.dumps({"status_code": 200, "body": {"status": "ok", "data": {"cached": True}}})
        app.state.redis.get = AsyncMock(return_value=cached)

        player_id = str(uuid4())
        from app.infrastructure.auth.jwt_provider import JwtProvider

        jwt_prov = JwtProvider(app.state.settings)
        token = jwt_prov.create_access_token(player_id, "android")

        @app.post("/api/v1/save")
        async def save() -> JSONResponse:
            return JSONResponse({"status": "ok", "data": {"cached": False}})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.post(
                "/api/v1/save",
                headers={
                    "Idempotency-Key": str(uuid4()),
                    "Authorization": f"Bearer {token}",
                },
            )
        body = resp.json()
        assert body["data"]["cached"] is True


# ─── RequestLoggingMiddleware ────────────────────────────────────────


class TestRequestLogging:
    @pytest.mark.anyio
    async def test_does_not_break_response(self) -> None:
        app = _make_app(with_logging=True)

        @app.get("/hello")
        async def hello() -> JSONResponse:
            return JSONResponse({"msg": "hi"})

        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            resp = await client.get("/hello")
        assert resp.status_code == 200
        assert resp.json()["msg"] == "hi"
