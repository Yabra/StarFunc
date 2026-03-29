"""Player repository — database access for players table."""

from uuid import UUID

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.infrastructure.persistence.models import PlayerModel


class PlayerRepository:
    def __init__(self, session: AsyncSession) -> None:
        self._session = session

    async def find_by_device_id(self, device_id: UUID) -> PlayerModel | None:
        stmt = select(PlayerModel).where(PlayerModel.device_id == device_id)
        result = await self._session.execute(stmt)
        return result.scalar_one_or_none()

    async def create(self, device_id: UUID, platform: str, client_version: str) -> PlayerModel:
        player = PlayerModel(
            device_id=device_id,
            platform=platform,
            client_version=client_version,
        )
        self._session.add(player)
        await self._session.flush()
        return player

    async def find_by_id(self, player_id: UUID) -> PlayerModel | None:
        stmt = select(PlayerModel).where(PlayerModel.id == player_id)
        result = await self._session.execute(stmt)
        return result.scalar_one_or_none()

    async def find_by_google_play_id(self, google_play_id: str) -> PlayerModel | None:
        stmt = select(PlayerModel).where(PlayerModel.google_play_id == google_play_id)
        result = await self._session.execute(stmt)
        return result.scalar_one_or_none()

    async def find_by_apple_gc_id(self, apple_gc_id: str) -> PlayerModel | None:
        stmt = select(PlayerModel).where(PlayerModel.apple_gc_id == apple_gc_id)
        result = await self._session.execute(stmt)
        return result.scalar_one_or_none()
