using System;
using UnityEngine;
using StarFunc.Data;

namespace StarFunc.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public class StarInteraction : MonoBehaviour
    {
        [SerializeField] StarEntity _entity;

        public event Action<StarEntity> OnStarTapped;

        Collider2D _collider;

        void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        public StarEntity Entity => _entity;

        public void RaiseTapped()
        {
            if (_entity.CurrentState == StarState.Hidden) return;
            OnStarTapped?.Invoke(_entity);
        }

        public void SetInteractable(bool interactable)
        {
            _collider.enabled = interactable;
        }
    }
}
