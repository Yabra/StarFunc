"""Unit tests for AuthService — S1.2."""

from datetime import UTC, datetime, timedelta
from unittest.mock import AsyncMock, MagicMock, patch
from uuid import UUID, uuid4

import pytest

from app.api.schemas.auth import RefreshRequest, RegisterRequest
from app.config import Settings
from app.domain.enums import Platform
from app.infrastructure.auth.jwt_provider import JwtProvider
from app.infrastructure.persistence.models import PlayerModel, PlayerSaveModel, RefreshTokenModel
from app.services.auth_service import AuthService


@pytest.fixture
def settings() -> Settings:
    return Settings(
        _env_file=None,  # type: ignore[call-arg]
        database_url="postgresql+asyncpg://u:p@localhost/db",
        jwt_secret="test-secret-key-that-is-long-enough-for-validation",
        jwt_access_token_expire_minutes=60,
        jwt_refresh_token_expire_days=90,
    )


@pytest.fixture
def jwt_provider(settings: Settings) -> JwtProvider:
    return JwtProvider(settings)


@pytest.fixture
def auth_service(jwt_provider: JwtProvider, settings: Settings) -> AuthService:
    return AuthService(jwt_provider, settings)


def _make_session() -> AsyncMock:
    """Create a mock AsyncSession with the methods AuthService uses."""
    session = AsyncMock()
    session.add = MagicMock()
    session.flush = AsyncMock()
    session.commit = AsyncMock()
    session.execute = AsyncMock()
    return session


def _make_player(device_id: UUID | None = None, player_id: UUID | None = None) -> PlayerModel:
    player = PlayerModel(
        id=player_id or uuid4(),
        device_id=device_id or uuid4(),
        platform="android",
        client_version="1.0.0",
    )
    return player


class TestRegisterNewPlayer:
    @pytest.mark.asyncio
    async def test_creates_player_and_returns_tokens(self, auth_service: AuthService, settings: Settings) -> None:
        session = _make_session()
        device_id = uuid4()
        request = RegisterRequest(device_id=device_id, platform=Platform.ANDROID, client_version="1.0.0")

        # Mock: no existing player found
        result_mock = MagicMock()
        result_mock.scalar_one_or_none.return_value = None
        session.execute.return_value = result_mock

        # Patch PlayerRepository so create() actually returns a controlled PlayerModel
        new_player = _make_player(device_id=device_id)

        with patch("app.services.auth_service.PlayerRepository") as repo_cls:
            repo_instance = AsyncMock()
            repo_instance.find_by_device_id.return_value = None
            repo_instance.create.return_value = new_player
            repo_cls.return_value = repo_instance

            response = await auth_service.register(request, session)

        assert response.is_new_player is True
        assert response.player_id == new_player.id
        assert response.access_token
        assert response.refresh_token
        assert response.expires_in == 3600

        # Verify initial save was added
        session.add.assert_called()
        added_objects = [call.args[0] for call in session.add.call_args_list]
        saves = [o for o in added_objects if isinstance(o, PlayerSaveModel)]
        assert len(saves) == 1
        assert saves[0].player_id == new_player.id

    @pytest.mark.asyncio
    async def test_stores_refresh_token_hash(self, auth_service: AuthService, jwt_provider: JwtProvider) -> None:
        session = _make_session()
        request = RegisterRequest(device_id=uuid4(), platform=Platform.ANDROID, client_version="1.0.0")

        new_player = _make_player()

        with patch("app.services.auth_service.PlayerRepository") as repo_cls:
            repo_instance = AsyncMock()
            repo_instance.find_by_device_id.return_value = None
            repo_instance.create.return_value = new_player
            repo_cls.return_value = repo_instance

            response = await auth_service.register(request, session)

        # The refresh token record should have been added
        added_objects = [call.args[0] for call in session.add.call_args_list]
        token_records = [o for o in added_objects if isinstance(o, RefreshTokenModel)]
        assert len(token_records) == 1

        expected_hash = jwt_provider.hash_token(response.refresh_token)
        assert token_records[0].token_hash == expected_hash
        assert token_records[0].player_id == new_player.id


class TestRegisterExistingPlayer:
    @pytest.mark.asyncio
    async def test_returns_existing_player_tokens(self, auth_service: AuthService) -> None:
        session = _make_session()
        existing_player = _make_player()
        request = RegisterRequest(
            device_id=existing_player.device_id, platform=Platform.ANDROID, client_version="1.0.0"
        )

        with patch("app.services.auth_service.PlayerRepository") as repo_cls:
            repo_instance = AsyncMock()
            repo_instance.find_by_device_id.return_value = existing_player
            repo_cls.return_value = repo_instance

            response = await auth_service.register(request, session)

        assert response.is_new_player is False
        assert response.player_id == existing_player.id
        assert response.access_token
        assert response.refresh_token

    @pytest.mark.asyncio
    async def test_idempotent_no_new_save_created(self, auth_service: AuthService) -> None:
        session = _make_session()
        existing_player = _make_player()
        request = RegisterRequest(device_id=existing_player.device_id, platform=Platform.IOS, client_version="1.0.0")

        with patch("app.services.auth_service.PlayerRepository") as repo_cls:
            repo_instance = AsyncMock()
            repo_instance.find_by_device_id.return_value = existing_player
            repo_cls.return_value = repo_instance

            await auth_service.register(request, session)

        # Should not create a PlayerSaveModel for existing player
        added_objects = [call.args[0] for call in session.add.call_args_list]
        saves = [o for o in added_objects if isinstance(o, PlayerSaveModel)]
        assert len(saves) == 0


class TestRefresh:
    def _make_token_record(
        self,
        player_id: UUID,
        token_hash: str,
        *,
        is_revoked: bool = False,
        expires_at: datetime | None = None,
    ) -> RefreshTokenModel:
        record = RefreshTokenModel(
            id=uuid4(),
            player_id=player_id,
            token_hash=token_hash,
            expires_at=expires_at or (datetime.now(UTC) + timedelta(days=90)),
            is_revoked=is_revoked,
        )
        return record

    @pytest.mark.asyncio
    async def test_successful_refresh(self, auth_service: AuthService, jwt_provider: JwtProvider) -> None:
        session = _make_session()
        player = _make_player()

        refresh_token = jwt_provider.create_refresh_token(player.id)
        token_hash = jwt_provider.hash_token(refresh_token)
        token_record = self._make_token_record(player.id, token_hash)

        # First execute → find token by hash; second execute → store new token (flush)
        result_mock = MagicMock()
        result_mock.scalar_one_or_none.return_value = token_record
        session.execute.return_value = result_mock

        request = RefreshRequest(refresh_token=refresh_token)

        with patch("app.services.auth_service.PlayerRepository") as repo_cls:
            repo_instance = AsyncMock()
            repo_instance.find_by_id.return_value = player
            repo_cls.return_value = repo_instance

            response = await auth_service.refresh(request, session)

        assert response.player_id == player.id
        assert response.access_token
        assert response.refresh_token
        assert response.is_new_player is False
        assert token_record.is_revoked is True

        # New refresh token record should have been stored
        added_objects = [call.args[0] for call in session.add.call_args_list]
        new_token_records = [o for o in added_objects if isinstance(o, RefreshTokenModel)]
        assert len(new_token_records) == 1
        assert new_token_records[0].player_id == player.id
        assert new_token_records[0].token_hash != token_hash  # different hash from old token
        # Old token linked to new via replaced_by_id
        assert token_record.replaced_by_id == new_token_records[0].id

    @pytest.mark.asyncio
    async def test_revoked_token_triggers_chain_revocation(
        self, auth_service: AuthService, jwt_provider: JwtProvider
    ) -> None:
        session = _make_session()
        player = _make_player()

        refresh_token = jwt_provider.create_refresh_token(player.id)
        token_hash = jwt_provider.hash_token(refresh_token)
        token_record = self._make_token_record(player.id, token_hash, is_revoked=True)

        result_mock = MagicMock()
        result_mock.scalar_one_or_none.return_value = token_record
        session.execute.return_value = result_mock

        request = RefreshRequest(refresh_token=refresh_token)

        from fastapi import HTTPException

        with pytest.raises(HTTPException) as exc_info:
            await auth_service.refresh(request, session)

        assert exc_info.value.status_code == 401
        assert exc_info.value.detail["code"] == "INVALID_TOKEN"
        assert "reuse" in exc_info.value.detail["message"].lower()

        # Session should have executed the bulk revocation update
        # commit is called to persist the revocation
        session.commit.assert_called()

    @pytest.mark.asyncio
    async def test_expired_token_raises_error(
        self, auth_service: AuthService, jwt_provider: JwtProvider, settings: Settings
    ) -> None:
        session = _make_session()
        player = _make_player()

        # Create a token that's technically valid JWT-wise but the DB record is expired
        refresh_token = jwt_provider.create_refresh_token(player.id)
        token_hash = jwt_provider.hash_token(refresh_token)
        token_record = self._make_token_record(player.id, token_hash, expires_at=datetime.now(UTC) - timedelta(hours=1))

        result_mock = MagicMock()
        result_mock.scalar_one_or_none.return_value = token_record
        session.execute.return_value = result_mock

        request = RefreshRequest(refresh_token=refresh_token)

        from fastapi import HTTPException

        with pytest.raises(HTTPException) as exc_info:
            await auth_service.refresh(request, session)

        assert exc_info.value.status_code == 401
        assert exc_info.value.detail["code"] == "TOKEN_EXPIRED"

    @pytest.mark.asyncio
    async def test_unknown_token_raises_error(self, auth_service: AuthService) -> None:
        session = _make_session()

        result_mock = MagicMock()
        result_mock.scalar_one_or_none.return_value = None
        session.execute.return_value = result_mock

        request = RefreshRequest(refresh_token="some.invalid.token")

        from fastapi import HTTPException

        with pytest.raises(HTTPException) as exc_info:
            await auth_service.refresh(request, session)

        assert exc_info.value.status_code == 401
        assert exc_info.value.detail["code"] == "INVALID_TOKEN"
