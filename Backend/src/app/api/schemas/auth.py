"""Auth Pydantic schemas: RegisterRequest, RefreshRequest, AuthResponse, LinkAccountRequest, LinkResponse."""

from typing import Literal
from uuid import UUID

from pydantic import BaseModel, Field

from app.domain.enums import Platform


class RegisterRequest(BaseModel):
    device_id: UUID = Field(alias="deviceId")
    platform: Platform
    client_version: str = Field(alias="clientVersion", max_length=20)

    model_config = {"populate_by_name": True}


class RefreshRequest(BaseModel):
    refresh_token: str = Field(alias="refreshToken")

    model_config = {"populate_by_name": True}


class AuthResponse(BaseModel):
    player_id: UUID = Field(alias="playerId")
    access_token: str = Field(alias="accessToken")
    refresh_token: str = Field(alias="refreshToken")
    expires_in: int = Field(alias="expiresIn")
    is_new_player: bool = Field(alias="isNewPlayer")

    model_config = {"populate_by_name": True}


class LinkAccountRequest(BaseModel):
    provider: Literal["google_play", "apple_game_center"]
    provider_token: str = Field(alias="providerToken", min_length=1)

    model_config = {"populate_by_name": True}


class LinkResponse(BaseModel):
    provider: str
    provider_id: str = Field(alias="providerId")
    linked: bool

    model_config = {"populate_by_name": True}
