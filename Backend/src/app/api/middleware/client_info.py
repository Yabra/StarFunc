"""ClientInfoMiddleware — extract X-Client-Version and X-Platform into request.state."""

from __future__ import annotations

from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware, RequestResponseEndpoint


class ClientInfoMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request: Request, call_next: RequestResponseEndpoint) -> Response:
        request.state.client_version = request.headers.get("x-client-version")
        request.state.platform = request.headers.get("x-platform")
        return await call_next(request)
