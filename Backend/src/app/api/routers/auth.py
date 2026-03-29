"""Auth router — POST /register, POST /refresh, POST /link."""

from fastapi import APIRouter, Depends

from app.api.schemas.auth import AuthResponse, LinkAccountRequest, LinkResponse, RefreshRequest, RegisterRequest
from app.api.schemas.common import ApiResponse
from app.dependencies import CurrentPlayerDep, JwtProviderDep, SessionDep, SettingsDep
from app.services.auth_service import AuthService

router = APIRouter(prefix="/auth", tags=["auth"])


def _get_auth_service(jwt_provider: JwtProviderDep, settings: SettingsDep) -> AuthService:
    return AuthService(jwt_provider, settings)


AuthServiceDep = Depends(_get_auth_service)


@router.post("/register", response_model=ApiResponse[AuthResponse])
async def register(
    request: RegisterRequest,
    session: SessionDep,
    auth_service: AuthService = AuthServiceDep,
) -> ApiResponse[AuthResponse]:
    result = await auth_service.register(request, session)
    return ApiResponse(data=result)


@router.post("/refresh", response_model=ApiResponse[AuthResponse])
async def refresh(
    request: RefreshRequest,
    session: SessionDep,
    auth_service: AuthService = AuthServiceDep,
) -> ApiResponse[AuthResponse]:
    result = await auth_service.refresh(request, session)
    return ApiResponse(data=result)


@router.post("/link", response_model=ApiResponse[LinkResponse])
async def link_account(
    request: LinkAccountRequest,
    session: SessionDep,
    player_id: CurrentPlayerDep,
    auth_service: AuthService = AuthServiceDep,
) -> ApiResponse[LinkResponse]:
    result = await auth_service.link_account(player_id, request, session)
    return ApiResponse(data=result)
