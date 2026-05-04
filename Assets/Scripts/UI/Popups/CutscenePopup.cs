using System;
using System.Collections;
using DG.Tweening;
using StarFunc.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StarFunc.UI
{
    /// <summary>
    /// Sector intro/outro cutscene player. Iterates over
    /// <see cref="CutsceneData.Frames"/>: swaps background, character sprite,
    /// types out text, and advances either by timer (<c>Frame.Duration &gt; 0</c>)
    /// or by player tap. Tapping mid-typewriter completes the text instantly;
    /// the second tap advances to the next frame.
    /// </summary>
    public class CutscenePopup : UIPopup, IPointerClickHandler
    {
        [Header("Visuals")]
        [SerializeField] Image _background;
        [SerializeField] Image _character;
        [SerializeField] TMP_Text _text;
        [SerializeField] GameObject _tapHint;

        [Header("Buttons")]
        [SerializeField] Button _skipButton;

        [Header("Behaviour")]
        [SerializeField, Range(10f, 200f)] float _typewriterCharsPerSecond = 40f;

        CutsceneData _data;
        Action _onComplete;
        int _frameIndex;
        Coroutine _typewriterRoutine;
        Coroutine _autoAdvanceRoutine;
        bool _typewriterDone;
        bool _waitingForTap;

        // Character animation baseline + active tween. Captured in Awake so
        // looping ambient motion (Pulse/Sway/etc.) can return the rect to its
        // designed pose between frames.
        RectTransform _characterRect;
        Vector3 _characterBaseScale;
        Vector2 _characterBaseAnchoredPos;
        Quaternion _characterBaseRotation;
        Tween _characterTween;

        void Awake()
        {
            if (_skipButton) _skipButton.onClick.AddListener(Skip);

            if (_character != null)
            {
                _characterRect = _character.rectTransform;
                _characterBaseScale = _characterRect.localScale;
                _characterBaseAnchoredPos = _characterRect.anchoredPosition;
                _characterBaseRotation = _characterRect.localRotation;
            }
        }

        void OnDestroy()
        {
            if (_skipButton) _skipButton.onClick.RemoveListener(Skip);
            StopAllRoutines();
            KillCharacterTween();
        }

        /// <summary>Show the popup with cutscene data; <paramref name="onComplete"/> fires on skip or last-frame advance.</summary>
        public void Show(CutsceneData data, Action onComplete = null)
        {
            _data = data;
            _onComplete = onComplete;
            _frameIndex = 0;

            base.Show((PopupData)null);

            if (_data == null || _data.Frames == null || _data.Frames.Length == 0)
            {
                Finish();
                return;
            }

            ShowFrame(0);
        }

        public override void Show(PopupData data)
        {
            base.Show(data);
            // PopupData entry is unused by this popup — see Show(CutsceneData, Action).
        }

        public override void Hide()
        {
            StopAllRoutines();
            KillCharacterTween();
            base.Hide();
        }

        // =========================================================================
        // Frame flow
        // =========================================================================

        void ShowFrame(int index)
        {
            StopAllRoutines();

            _frameIndex = index;
            var frame = _data.Frames[index];

            if (_background)
            {
                _background.sprite = frame.Background;
                _background.enabled = frame.Background != null;
            }
            if (_character)
            {
                _character.sprite = frame.CharacterSprite;
                _character.enabled = frame.CharacterSprite != null;
            }

            PlayCharacterAnimation(frame.Animation);

            _typewriterDone = false;
            _waitingForTap = false;
            if (_tapHint) _tapHint.SetActive(false);

            _typewriterRoutine = StartCoroutine(TypewriterRoutine(frame.Text));

            if (frame.Duration > 0f)
                _autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine(frame.Duration));
        }

        IEnumerator TypewriterRoutine(string text)
        {
            if (_text == null)
            {
                _typewriterDone = true;
                ShowTapHintIfNeeded();
                yield break;
            }

            if (string.IsNullOrEmpty(text))
            {
                _text.text = string.Empty;
                _typewriterDone = true;
                ShowTapHintIfNeeded();
                yield break;
            }

            _text.text = string.Empty;
            float charDelay = 1f / Mathf.Max(1f, _typewriterCharsPerSecond);

            for (int i = 1; i <= text.Length; i++)
            {
                _text.text = text.Substring(0, i);
                yield return new WaitForSeconds(charDelay);
            }

            _typewriterDone = true;
            ShowTapHintIfNeeded();
        }

        IEnumerator AutoAdvanceRoutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Advance();
        }

        void ShowTapHintIfNeeded()
        {
            if (_data == null || _frameIndex >= _data.Frames.Length) return;
            // Tap-driven frames have Duration <= 0.
            if (_data.Frames[_frameIndex].Duration > 0f) return;

            _waitingForTap = true;
            if (_tapHint) _tapHint.SetActive(true);
        }

        void Advance()
        {
            int next = _frameIndex + 1;
            if (next >= _data.Frames.Length)
            {
                Finish();
                return;
            }
            ShowFrame(next);
        }

        void Skip() => Finish();

        void Finish()
        {
            StopAllRoutines();
            var cb = _onComplete;
            _onComplete = null;
            Hide();
            cb?.Invoke();
        }

        void StopAllRoutines()
        {
            if (_typewriterRoutine != null) { StopCoroutine(_typewriterRoutine); _typewriterRoutine = null; }
            if (_autoAdvanceRoutine != null) { StopCoroutine(_autoAdvanceRoutine); _autoAdvanceRoutine = null; }
        }

        // =========================================================================
        // Character animation (per-frame DOTween loops on _character)
        // =========================================================================

        void PlayCharacterAnimation(CutsceneFrameAnimation animation)
        {
            KillCharacterTween();
            if (_characterRect == null) return;

            // Always restore baseline before applying the next loop, so frames
            // don't accumulate drift if a tween was killed mid-cycle.
            _characterRect.localScale = _characterBaseScale;
            _characterRect.anchoredPosition = _characterBaseAnchoredPos;
            _characterRect.localRotation = _characterBaseRotation;

            switch (animation)
            {
                case CutsceneFrameAnimation.Pulse:
                    _characterTween = _characterRect
                        .DOScale(_characterBaseScale * 1.04f, 0.7f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                    break;

                case CutsceneFrameAnimation.Bounce:
                    _characterTween = _characterRect
                        .DOScale(_characterBaseScale * 1.10f, 0.35f)
                        .SetEase(Ease.OutQuad)
                        .SetLoops(-1, LoopType.Yoyo);
                    break;

                case CutsceneFrameAnimation.Sway:
                    _characterTween = TweenAnchoredPosX(
                        _characterBaseAnchoredPos.x + 12f, 0.9f, Ease.InOutSine);
                    break;

                case CutsceneFrameAnimation.Shake:
                    // Quick alternating yoyo at 0.06s/cycle reads as a
                    // continuous jitter — designed for tense / surprised lines.
                    _characterTween = TweenAnchoredPosX(
                        _characterBaseAnchoredPos.x + 6f, 0.06f, Ease.OutQuad);
                    break;

                case CutsceneFrameAnimation.FloatUp:
                    _characterTween = TweenAnchoredPosY(
                        _characterBaseAnchoredPos.y + 8f, 1.1f, Ease.InOutSine);
                    break;

                case CutsceneFrameAnimation.Tilt:
                    _characterTween = _characterRect
                        .DOLocalRotate(new Vector3(0f, 0f, 5f), 1.0f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                    break;

                case CutsceneFrameAnimation.Wiggle:
                    _characterTween = _characterRect
                        .DOLocalRotate(new Vector3(0f, 0f, 8f), 0.18f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                    break;

                case CutsceneFrameAnimation.None:
                default:
                    // Nothing to do — baseline is already restored above.
                    break;
            }
        }

        // RectTransform.DOAnchorPos* lives in DOTween's UI module which the
        // project compiles into Assembly-CSharp (inaccessible from asmdefs),
        // so we go through DOTween.To with explicit getters/setters — same
        // pattern as StarManager.DrawConstellationLineRoutine.
        Tween TweenAnchoredPosX(float endX, float duration, Ease ease,
            int loops = -1, LoopType loopType = LoopType.Yoyo)
        {
            return DOTween
                .To(() => _characterRect.anchoredPosition.x,
                    x =>
                    {
                        var p = _characterRect.anchoredPosition;
                        p.x = x;
                        _characterRect.anchoredPosition = p;
                    },
                    endX, duration)
                .SetEase(ease)
                .SetLoops(loops, loopType);
        }

        Tween TweenAnchoredPosY(float endY, float duration, Ease ease,
            int loops = -1, LoopType loopType = LoopType.Yoyo)
        {
            return DOTween
                .To(() => _characterRect.anchoredPosition.y,
                    y =>
                    {
                        var p = _characterRect.anchoredPosition;
                        p.y = y;
                        _characterRect.anchoredPosition = p;
                    },
                    endY, duration)
                .SetEase(ease)
                .SetLoops(loops, loopType);
        }

        void KillCharacterTween()
        {
            if (_characterTween != null && _characterTween.IsActive())
                _characterTween.Kill();
            _characterTween = null;

            if (_characterRect != null)
            {
                _characterRect.localScale = _characterBaseScale;
                _characterRect.anchoredPosition = _characterBaseAnchoredPos;
                _characterRect.localRotation = _characterBaseRotation;
            }
        }

        // =========================================================================
        // Tap handling
        // =========================================================================

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null || _frameIndex >= _data.Frames.Length) return;

            // First tap during typewriter: finish text immediately.
            if (!_typewriterDone)
            {
                if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
                if (_text != null) _text.text = _data.Frames[_frameIndex].Text ?? string.Empty;
                _typewriterDone = true;
                ShowTapHintIfNeeded();
                return;
            }

            // Second tap on a tap-driven frame: advance.
            if (_waitingForTap) Advance();
        }
    }
}
