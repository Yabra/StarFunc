"""Apple Game Center signature verification."""

import httpx

from app.domain.exceptions import AppError

_APPLE_VERIFY_URL = "https://appleid.apple.com/auth/token"


class AppleGameCenterVerifier:
    """Verifies an Apple Game Center identity token and returns the player ID."""

    async def verify(self, provider_token: str) -> str:
        async with httpx.AsyncClient(timeout=10) as client:
            resp = await client.post(
                _APPLE_VERIFY_URL,
                json={"identityToken": provider_token},
            )

        if resp.status_code != 200:
            raise AppError(
                code="INVALID_PROVIDER_TOKEN",
                message="Apple Game Center token verification failed",
                status_code=401,
            )

        data = resp.json()
        apple_gc_id: str | None = data.get("gamePlayerId")
        if not apple_gc_id:
            raise AppError(
                code="INVALID_PROVIDER_TOKEN",
                message="Apple Game Center token missing player ID",
                status_code=401,
            )

        return apple_gc_id
