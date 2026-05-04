using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using StarFunc.Core;
using StarFunc.Data;
using StarFunc.Meta;

namespace StarFunc.Gameplay
{
    public class StarManager : MonoBehaviour
    {
        [SerializeField] StarEntity _starPrefab;
        [SerializeField] CoordinatePlane _plane;
        [SerializeField] Camera _camera;

        [Header("Constellation Lines")]
        [SerializeField] Color _constellationLineColor = new(1f, 0.85f, 0.3f, 1f);
        [SerializeField] float _constellationLineWidth = 0.06f;
        [SerializeField] float _constellationLineDrawDuration = 0.5f;
        [SerializeField] int _constellationLineSortingOrder = 4;

        readonly Dictionary<string, StarEntity> _stars = new();
        readonly List<LineRenderer> _constellationLines = new();
        Transform _constellationLineRoot;
        IFeedbackService _feedback;

        IFeedbackService Feedback
        {
            get
            {
                if (_feedback == null && ServiceLocator.Contains<IFeedbackService>())
                    _feedback = ServiceLocator.Get<IFeedbackService>();
                return _feedback;
            }
        }

        /// <summary>
        /// Fires when any managed star is tapped.
        /// </summary>
        public event Action<StarEntity> OnStarTapped;

        /// <summary>
        /// Fires when the player taps the plane and the tap doesn't land on any
        /// interactable star. Coordinate is in plane-space.
        /// Used by RestoreConstellation to capture placement attempts.
        /// </summary>
        public event Action<Vector2> OnPlaneTapped;

        void Update()
        {
            if (_stars.Count == 0) return;

            bool pressed = false;
            Vector2 screenPos = default;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                pressed = true;
                screenPos = mouse.position.ReadValue();
            }

            if (!pressed)
            {
                var touch = Touchscreen.current;
                if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                {
                    pressed = true;
                    screenPos = touch.primaryTouch.position.ReadValue();
                }
            }

            if (!pressed) return;

            var cam = _camera ? _camera : Camera.main;
            if (!cam) return;

            Vector3 worldPos3 = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            Vector2 worldPos = new(worldPos3.x, worldPos3.y);

            var hits = Physics2D.OverlapPointAll(worldPos);
            foreach (var hit in hits)
            {
                var interaction = hit.GetComponent<StarInteraction>();
                if (interaction)
                {
                    interaction.RaiseTapped();
                    return;
                }
            }

            // No star hit — surface as a plane-level tap for modes like RestoreConstellation.
            if (OnPlaneTapped != null && _plane != null)
            {
                Vector2 planePos = _plane.WorldToPlane(worldPos);
                OnPlaneTapped.Invoke(planePos);
            }
        }

        public void SpawnStars(StarConfig[] configs)
        {
            ClearAll();

            foreach (var config in configs)
            {
                var worldPos = _plane.PlaneToWorld(config.Coordinate);
                var instance = Instantiate(_starPrefab, worldPos, Quaternion.identity, transform);
                instance.Initialize(config);
                instance.OnTapped += HandleStarTapped;
                _stars[config.StarId] = instance;
            }
        }

        public StarEntity GetStar(string starId)
        {
            _stars.TryGetValue(starId, out var star);
            return star;
        }

        /// <summary>Enumerate every spawned star. Used by IdentifyError to wire taps.</summary>
        public IEnumerable<StarEntity> GetAllStars() => _stars.Values;

        public List<StarEntity> GetAllPlaced()
        {
            return _stars.Values
                .Where(s => s.CurrentState == StarState.Placed)
                .ToList();
        }

        public void ResetAll()
        {
            foreach (var star in _stars.Values)
                star.SetState(star.Config.InitialState);
        }

        /// <summary>
        /// Set all stars to Hidden state (used by Memory Mode after preview expires).
        /// </summary>
        public void HideAll()
        {
            foreach (var star in _stars.Values)
                star.SetState(StarState.Hidden);
        }

        /// <summary>
        /// Set all stars to their initial states (used by Memory Mode to show reference).
        /// </summary>
        public void ShowAll()
        {
            foreach (var star in _stars.Values)
                star.SetState(star.Config.InitialState);
        }

        public void ClearAll()
        {
            foreach (var star in _stars.Values)
            {
                star.OnTapped -= HandleStarTapped;
                Destroy(star.gameObject);
            }
            _stars.Clear();
            ClearConstellationLines();
        }

        /// <summary>
        /// Sequentially restore stars in the given order: each star plays its
        /// PlayRestore animation, then a gold line is drawn from the previous
        /// star to the current one. Callers typically pass solution stars in
        /// definition order (Architecture.md §5.5: "восстановить созвездие").
        /// </summary>
        public Coroutine PlayConstellationRestore(IList<StarConfig> orderedStars)
        {
            return StartCoroutine(ConstellationRestoreRoutine(orderedStars));
        }

        IEnumerator ConstellationRestoreRoutine(IList<StarConfig> orderedStars)
        {
            if (orderedStars == null || orderedStars.Count == 0) yield break;

            ClearConstellationLines();
            EnsureLineRoot();

            StarEntity prev = null;
            foreach (var config in orderedStars)
            {
                if (!_stars.TryGetValue(config.StarId, out var star) || star == null) continue;

                if (star.CurrentState == StarState.Hidden) star.SetState(StarState.Restored);

                yield return star.PlayRestore();

                if (prev != null)
                    yield return DrawConstellationLineRoutine(prev.transform.position, star.transform.position);

                prev = star;
            }
        }

        IEnumerator DrawConstellationLineRoutine(Vector3 from, Vector3 to)
        {
            EnsureLineRoot();

            // Cue the constellation glow at the destination star — fires per
            // line so the chain reaction reads as a sequence (Architecture.md
            // §5.5).
            Feedback?.PlayFeedback(FeedbackType.ConstellationRestored, to);

            var go = new GameObject("ConstellationLine");
            go.transform.SetParent(_constellationLineRoot, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = _constellationLineWidth;
            lr.endWidth = _constellationLineWidth;
            lr.startColor = _constellationLineColor;
            lr.endColor = _constellationLineColor;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.sortingOrder = _constellationLineSortingOrder;
            lr.SetPosition(0, from);
            lr.SetPosition(1, from);
            _constellationLines.Add(lr);

            // Tween the second endpoint from `from` → `to` via a captured 0..1
            // parameter; using DOTween.To with a lambda works without needing
            // any DOTween Module .cs files in our asmdef.
            var tween = DOTween.To(() => 0f, t => lr.SetPosition(1, Vector3.Lerp(from, to, t)),
                                   1f, _constellationLineDrawDuration)
                .SetEase(Ease.OutQuad);

            while (tween != null && tween.IsActive() && !tween.IsComplete())
                yield return null;

            lr.SetPosition(1, to);
        }

        void EnsureLineRoot()
        {
            if (_constellationLineRoot != null) return;
            var go = new GameObject("ConstellationLines");
            go.transform.SetParent(transform, false);
            _constellationLineRoot = go.transform;
        }

        void ClearConstellationLines()
        {
            foreach (var lr in _constellationLines)
                if (lr) Destroy(lr.gameObject);
            _constellationLines.Clear();
        }

        void HandleStarTapped(StarEntity star)
        {
            Debug.Log($"[StarManager] Star tapped: {star.StarId} at ({star.GetCoordinate().x:F1}, {star.GetCoordinate().y:F1}), state={star.CurrentState}");
            OnStarTapped?.Invoke(star);
        }

        void OnDestroy()
        {
            foreach (var star in _stars.Values)
                star.OnTapped -= HandleStarTapped;
        }
    }
}
