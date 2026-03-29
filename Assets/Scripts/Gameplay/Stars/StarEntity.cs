using System;
using UnityEngine;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    public class StarEntity : MonoBehaviour
    {
        [SerializeField] StarVisuals _visuals;
        [SerializeField] StarAnimator _animator;
        [SerializeField] StarInteraction _interaction;

        StarConfig _config;
        StarState _currentState;

        public string StarId => _config.StarId;
        public StarState CurrentState => _currentState;
        public StarConfig Config => _config;

        public void Initialize(StarConfig config)
        {
            _config = config;
            gameObject.name = $"Star_{config.StarId}";
            SetState(config.InitialState);
        }

        public void SetState(StarState state)
        {
            _currentState = state;
            _visuals.ApplyState(state);
        }

        public Vector2 GetCoordinate() => _config.Coordinate;

        public Coroutine PlayAppear() => _animator.PlayAppear();
        public Coroutine PlayPlace() => _animator.PlayPlace();
        public Coroutine PlayError() => _animator.PlayError();
        public Coroutine PlayRestore() => _animator.PlayRestore();

        public event Action<StarEntity> OnTapped
        {
            add => _interaction.OnStarTapped += value;
            remove => _interaction.OnStarTapped -= value;
        }

        public void SetInteractable(bool interactable) =>
            _interaction.SetInteractable(interactable);
    }
}
