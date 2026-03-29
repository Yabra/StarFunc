"""Placeholder tests to verify CI pipeline works."""

from app.config import Settings


def test_settings_defaults() -> None:
    """Settings can be instantiated with only required fields."""
    s = Settings(
        _env_file=None,  # type: ignore[call-arg]
        database_url="postgresql+asyncpg://u:p@localhost/db",
        jwt_secret="test-secret-key-that-is-long-enough-for-validation",
    )
    assert s.max_lives == 5
    assert s.jwt_algorithm == "HS256"
    assert s.env == "development"


def test_addition() -> None:
    """Arithmetic still works. If this fails, we have bigger problems."""
    assert 2 + 2 == 4
