"""Tests for AuthService.link_account — account linking flow."""

import uuid
from unittest.mock import AsyncMock, MagicMock, patch

import pytest
import pytest_asyncio

from app.api.schemas.auth import LinkAccountRequest
from app.config import Settings
from app.domain.exceptions import ConflictError, NotFoundError
from app.infrastructure.auth.jwt_provider import JwtProvider
from app.infrastructure.persistence.models import PlayerModel
from app.services.auth_service import AuthService


@pytest.fixture
def settings() -> Settings:
    return Settings(  # type: ignore[call-arg]
        database_url="postgresql+asyncpg://test:test@localhost:5432/test",
        jwt_secret="test-secret-key-minimum-256-bits-long-key",
        _env_file=None,
    )


@pytest.fixture
def auth_service(settings: Settings) -> AuthService:
    jwt = JwtProvider(settings)
    return AuthService(jwt, settings)


def _make_player(player_id: uuid.UUID | None = None, **kwargs) -> PlayerModel:
    p = PlayerModel()
    p.id = player_id or uuid.uuid4()
    p.device_id = uuid.uuid4()
    p.platform = "android"
    p.client_version = "1.0.0"
    p.google_play_id = kwargs.get("google_play_id")
    p.apple_gc_id = kwargs.get("apple_gc_id")
    return p


@pytest.mark.asyncio
async def test_link_google_play_success(auth_service: AuthService) -> None:
    player_id = uuid.uuid4()
    player = _make_player(player_id)
    request = LinkAccountRequest(provider="google_play", provider_token="valid-token")

    session = AsyncMock()
    session.commit = AsyncMock()

    with (
        patch(
            "app.services.auth_service.GooglePlayVerifier.verify",
            new_callable=AsyncMock,
            return_value="google-player-123",
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_google_play_id",
            new_callable=AsyncMock,
            return_value=None,
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_id",
            new_callable=AsyncMock,
            return_value=player,
        ),
    ):
        result = await auth_service.link_account(player_id, request, session)

    assert result.provider == "google_play"
    assert result.provider_id == "google-player-123"
    assert result.linked is True
    assert player.google_play_id == "google-player-123"
    session.commit.assert_awaited_once()


@pytest.mark.asyncio
async def test_link_apple_game_center_success(auth_service: AuthService) -> None:
    player_id = uuid.uuid4()
    player = _make_player(player_id)
    request = LinkAccountRequest(provider="apple_game_center", provider_token="valid-token")

    session = AsyncMock()
    session.commit = AsyncMock()

    with (
        patch(
            "app.services.auth_service.AppleGameCenterVerifier.verify",
            new_callable=AsyncMock,
            return_value="apple-player-456",
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_apple_gc_id",
            new_callable=AsyncMock,
            return_value=None,
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_id",
            new_callable=AsyncMock,
            return_value=player,
        ),
    ):
        result = await auth_service.link_account(player_id, request, session)

    assert result.provider == "apple_game_center"
    assert result.provider_id == "apple-player-456"
    assert result.linked is True
    assert player.apple_gc_id == "apple-player-456"


@pytest.mark.asyncio
async def test_link_account_already_linked_to_other_player(auth_service: AuthService) -> None:
    player_id = uuid.uuid4()
    other_player = _make_player(google_play_id="google-player-123")
    request = LinkAccountRequest(provider="google_play", provider_token="valid-token")

    session = AsyncMock()

    with (
        patch(
            "app.services.auth_service.GooglePlayVerifier.verify",
            new_callable=AsyncMock,
            return_value="google-player-123",
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_google_play_id",
            new_callable=AsyncMock,
            return_value=other_player,
        ),
    ):
        with pytest.raises(ConflictError) as exc_info:
            await auth_service.link_account(player_id, request, session)

    assert exc_info.value.code == "ACCOUNT_ALREADY_LINKED"


@pytest.mark.asyncio
async def test_link_account_re_link_same_player_updates(auth_service: AuthService) -> None:
    """Re-linking the same provider for the same player should update the ID."""
    player_id = uuid.uuid4()
    player = _make_player(player_id, google_play_id="old-google-id")
    request = LinkAccountRequest(provider="google_play", provider_token="new-token")

    session = AsyncMock()
    session.commit = AsyncMock()

    with (
        patch(
            "app.services.auth_service.GooglePlayVerifier.verify",
            new_callable=AsyncMock,
            return_value="new-google-id",
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_google_play_id",
            new_callable=AsyncMock,
            return_value=None,
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_id",
            new_callable=AsyncMock,
            return_value=player,
        ),
    ):
        result = await auth_service.link_account(player_id, request, session)

    assert result.linked is True
    assert player.google_play_id == "new-google-id"


@pytest.mark.asyncio
async def test_link_account_same_provider_id_same_player_ok(auth_service: AuthService) -> None:
    """If the same provider_id is already on this player, it should succeed (idempotent)."""
    player_id = uuid.uuid4()
    player = _make_player(player_id, google_play_id="google-player-123")
    request = LinkAccountRequest(provider="google_play", provider_token="valid-token")

    session = AsyncMock()
    session.commit = AsyncMock()

    with (
        patch(
            "app.services.auth_service.GooglePlayVerifier.verify",
            new_callable=AsyncMock,
            return_value="google-player-123",
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_google_play_id",
            new_callable=AsyncMock,
            return_value=player,
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_id",
            new_callable=AsyncMock,
            return_value=player,
        ),
    ):
        result = await auth_service.link_account(player_id, request, session)

    assert result.linked is True
    assert result.provider_id == "google-player-123"


@pytest.mark.asyncio
async def test_link_account_player_not_found(auth_service: AuthService) -> None:
    player_id = uuid.uuid4()
    request = LinkAccountRequest(provider="google_play", provider_token="valid-token")

    session = AsyncMock()

    with (
        patch(
            "app.services.auth_service.GooglePlayVerifier.verify",
            new_callable=AsyncMock,
            return_value="google-player-123",
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_google_play_id",
            new_callable=AsyncMock,
            return_value=None,
        ),
        patch(
            "app.services.auth_service.PlayerRepository.find_by_id",
            new_callable=AsyncMock,
            return_value=None,
        ),
    ):
        with pytest.raises(NotFoundError):
            await auth_service.link_account(player_id, request, session)
