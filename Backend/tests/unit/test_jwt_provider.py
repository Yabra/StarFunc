"""Unit tests for JwtProvider — S1.1."""

from datetime import UTC, datetime, timedelta
from uuid import uuid4

import pytest
from jose import JWTError, jwt

from app.config import Settings
from app.infrastructure.auth.jwt_provider import JwtProvider


@pytest.fixture
def settings() -> Settings:
    return Settings(
        _env_file=None,  # type: ignore[call-arg]
        database_url="postgresql+asyncpg://u:p@localhost/db",
        jwt_secret="test-secret-key-that-is-long-enough-for-validation",
    )


@pytest.fixture
def provider(settings: Settings) -> JwtProvider:
    return JwtProvider(settings)


class TestAccessToken:
    def test_create_and_decode(self, provider: JwtProvider) -> None:
        player_id = uuid4()
        token = provider.create_access_token(player_id, "android")
        claims = provider.decode(token)

        assert claims["sub"] == str(player_id)
        assert claims["platform"] == "android"

    def test_claims(self, provider: JwtProvider) -> None:
        player_id = uuid4()
        token = provider.create_access_token(player_id, "ios")
        claims = provider.decode(token)

        assert claims["iss"] == "starfunc-api"
        assert claims["aud"] == "starfunc-client"
        assert "iat" in claims
        assert "exp" in claims

    def test_expired_token_rejected(self, settings: Settings) -> None:
        expired_payload = {
            "sub": str(uuid4()),
            "platform": "android",
            "iat": datetime.now(UTC) - timedelta(hours=2),
            "exp": datetime.now(UTC) - timedelta(hours=1),
            "iss": "starfunc-api",
            "aud": "starfunc-client",
        }
        token = jwt.encode(expired_payload, settings.jwt_secret, algorithm=settings.jwt_algorithm)

        provider = JwtProvider(settings)
        with pytest.raises(JWTError):
            provider.decode(token)

    def test_wrong_signature_rejected(self, provider: JwtProvider, settings: Settings) -> None:
        token = provider.create_access_token(uuid4(), "android")

        other_settings = Settings(
            _env_file=None,  # type: ignore[call-arg]
            database_url=settings.database_url,
            jwt_secret="completely-different-secret-key-for-testing",
        )
        other_provider = JwtProvider(other_settings)

        with pytest.raises(JWTError):
            other_provider.decode(token)


class TestRefreshToken:
    def test_create_and_decode_raw(self, provider: JwtProvider, settings: Settings) -> None:
        player_id = uuid4()
        token = provider.create_refresh_token(player_id)

        # Refresh tokens don't carry aud/iss, so decode raw
        claims = jwt.decode(token, settings.jwt_secret, algorithms=[settings.jwt_algorithm])
        assert claims["sub"] == str(player_id)
        assert claims["type"] == "refresh"

    def test_refresh_ttl_90_days(self, provider: JwtProvider, settings: Settings) -> None:
        token = provider.create_refresh_token(uuid4())
        claims = jwt.decode(token, settings.jwt_secret, algorithms=[settings.jwt_algorithm])

        exp = datetime.fromtimestamp(claims["exp"], tz=UTC)
        iat = datetime.fromtimestamp(claims["iat"], tz=UTC)
        delta = exp - iat
        assert delta == timedelta(days=90)

    def test_refresh_token_rejected_by_access_decode(self, provider: JwtProvider) -> None:
        """decode() requires aud/iss, so a refresh token must be rejected."""
        token = provider.create_refresh_token(uuid4())

        with pytest.raises(JWTError):
            provider.decode(token)


class TestHashToken:
    def test_stable_hash(self, provider: JwtProvider) -> None:
        token = "some-token-value"
        assert provider.hash_token(token) == provider.hash_token(token)

    def test_different_tokens_different_hashes(self, provider: JwtProvider) -> None:
        assert provider.hash_token("token-a") != provider.hash_token("token-b")

    def test_hash_is_sha256_hex(self, provider: JwtProvider) -> None:
        h = provider.hash_token("x")
        assert len(h) == 64  # SHA-256 hex digest length
