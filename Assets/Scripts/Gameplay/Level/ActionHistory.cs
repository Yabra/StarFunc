using System.Collections.Generic;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Stack-based action history for Undo / Reset support.
    /// Plain C# class — not a MonoBehaviour.
    /// </summary>
    public class ActionHistory
    {
        readonly Stack<PlayerAction> _actions = new();

        public bool CanUndo => _actions.Count > 0;

        /// <summary>Record a player action.</summary>
        public void Push(PlayerAction action)
        {
            _actions.Push(action);
        }

        /// <summary>Pop and return the most recent action, or null if empty.</summary>
        public PlayerAction Undo()
        {
            return _actions.Count > 0 ? _actions.Pop() : null;
        }

        /// <summary>Clear all recorded actions.</summary>
        public void Reset()
        {
            _actions.Clear();
        }
    }
}
