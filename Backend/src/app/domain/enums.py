"""Domain enums: TaskType, FunctionType, SectorState, LevelType, etc."""

from enum import StrEnum


class Platform(StrEnum):
    ANDROID = "android"
    IOS = "ios"
