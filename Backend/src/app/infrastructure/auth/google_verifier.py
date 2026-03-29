"""Google Play Games token verification."""

import httpx

from app.domain.exceptions import AppError

_GOOGLE_TOKEN_INFO_URL = "https://www.googleapis.com/oauth2/v3/tokeninfo"


class GooglePlayVerifier:
    """Verifies a Google Play Games auth code / token and returns the player ID."""

    async def verify(self, provider_token: str) -> str:
        async with httpx.AsyncClient(timeout=10) as client:
            resp = await client.get(
                _GOOGLE_TOKEN_INFO_URL,
                params={"access_token": provider_token},
            )

        if resp.status_code != 200:
            raise AppError(
                code="INVALID_PROVIDER_TOKEN",
                message="Google Play token verification failed",
                status_code=401,
            )

        data = resp.json()
        google_play_id: str | None = data.get("sub")
        if not google_play_id:
            raise AppError(
                code="INVALID_PROVIDER_TOKEN",
                message="Google Play token missing subject",
                status_code=401,
            )

        return google_play_id
