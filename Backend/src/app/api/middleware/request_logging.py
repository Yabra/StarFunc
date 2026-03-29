"""RequestLoggingMiddleware — structured JSON logging of requests and responses."""

from __future__ import annotations

import time

import structlog
from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware, RequestResponseEndpoint

logger = structlog.stdlib.get_logger()


class RequestLoggingMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request: Request, call_next: RequestResponseEndpoint) -> Response:
        start = time.monotonic()

        player_id = getattr(request.state, "player_id", None)
        client_version = request.headers.get("x-client-version")

        await logger.ainfo(
            "request_started",
            method=request.method,
            path=request.url.path,
            player_id=str(player_id) if player_id else None,
            client_version=client_version,
        )

        response = await call_next(request)

        duration_ms = round((time.monotonic() - start) * 1000, 2)
        await logger.ainfo(
            "request_finished",
            method=request.method,
            path=request.url.path,
            status_code=response.status_code,
            duration_ms=duration_ms,
            player_id=str(player_id) if player_id else None,
        )

        return response
