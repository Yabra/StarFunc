using StarFunc.Data;

namespace StarFunc.Meta
{
    public struct EndlessOptions
    {
        public TaskType TaskType;
        public EndlessDifficulty Difficulty;

        /// <summary>
        /// Null = pick a fresh random seed (Environment.TickCount). Set to a
        /// fixed value to reproduce the same level — used by future "share
        /// puzzle" UI.
        /// </summary>
        public int? Seed;
    }
}
