using System;
using System.Collections.Generic;
using GameLogic.Sudoku;
using UnityEngine;

namespace GameLogic
{
    public sealed class SudokuBoardWidget : UIWidget
    {
        private readonly List<SudoCellWidget> _cells = new List<SudoCellWidget>(81);
        private readonly RectTransform[] _vertical = new RectTransform[10], _horizontal = new RectTransform[10];
        private Vector2 _lastSize = new Vector2(-1, -1);
        // 每圈先变浅色再恢复，随后播放外一圈。单位：秒。
        private const float CompletionFadeIn = 0.06f;
        private const float CompletionFadeOut = 0.12f;
        private const float CompletionStepDelay = 0.05f;//CompletionFadeIn + CompletionFadeOut;
        private readonly int[] _waveSteps = new int[81];
        private float _waveElapsed, _waveDuration;
        private int _levelRevision, _waveOrigin;
        private SudokuCompletedRegion _waveRegions;
        private bool _wavePlaying;
        private SudokuGameController _game;
        public bool IsReady => _cells.Count == 81;
        public int CellCount => _cells.Count;
        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent<SudokuGameController, int, SudokuCompletedRegion>(EventDefine.SudokuRegionsCompleted, PlayCompletion);
            SudokuThemeManager.Changed += RefreshTheme;
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            var template = FindChild("SudoCell");
            if (template == null) throw new InvalidOperationException("棋盘缺少 SudoCell 模板。");
            template.gameObject.SetActive(false);
            for (int i = 0; i < 10; i++)
            {
                _vertical[i] = FindChildComponent<RectTransform>("Vertical" + i);
                _horizontal[i] = FindChildComponent<RectTransform>("Horizontal" + i);
                if (_vertical[i] == null || _horizontal[i] == null) throw new InvalidOperationException("棋盘网格线缺失: " + i);
            }
            // 随窗口异步加载的本地模板，复制不涉及资源 IO。
            for (int i = 0; i < 81; i++)
            {
                int index = i;
                var cell = CreateWidgetByPrefab<SudoCellWidget>(template.gameObject, transform);
                cell.gameObject.name = "Cell" + i;
                cell.Bind(() => _game?.SelectCell(index));
                _cells.Add(cell);
            }
            LayoutBoard();
        }
        public void Refresh(SudokuGameController game)
        {
            if (_game != game || _levelRevision != game.LevelRevision || !CanAnimate(game) ||
                (_wavePlaying && (game.Session.CompletedRegions(_waveOrigin) & _waveRegions) != _waveRegions))
                StopCompletion();
            _game = game; _levelRevision = game.LevelRevision;
            for (int i = 0; i < _cells.Count; i++) _cells[i].Refresh(game.Session, i, game.SelectedCell, game.CanInteract);
            // 暂停时隐藏题面，保留原有布局，不额外生成遮罩 UI。
            foreach (var cell in _cells) cell.Visible = !game.IsPaused;
        }
        protected override void OnUpdate()
        {
            LayoutBoard();
            if (!_wavePlaying) return;
            if (!CanAnimate(_game)) { StopCompletion(); return; }
            _waveElapsed += Time.unscaledDeltaTime;
            UpdateCompletion();
        }
        private void RefreshTheme()
        {
            if (_game != null && OwnerWindow.IsOpen) Refresh(_game);
        }
        private static bool CanAnimate(SudokuGameController game) =>
            game != null && game.IsWindowActive && !game.IsPaused && !game.IsSuspended && !game.IsAdShowing;

        private void PlayCompletion(SudokuGameController game, int origin, SudokuCompletedRegion regions)
        {
            if (_game != game || _levelRevision != game.LevelRevision || !IsReady || !CanAnimate(game)) return;
            StopCompletion();
            _waveOrigin = origin; _waveRegions = regions;
            int maxStep = 0;
            for (int i = 0; i < _cells.Count; i++)
            {
                // 行、列、宫的并集：每格只加入一次，交叉位置不会重复闪烁。
                _waveSteps[i] = SudokuCompletion.WaveStep(origin, i, regions);
                maxStep = Math.Max(maxStep, _waveSteps[i]);
            }
            _waveElapsed = 0;
            _waveDuration = maxStep * CompletionStepDelay + CompletionFadeIn + CompletionFadeOut;
            _wavePlaying = true;
        }
        private void UpdateCompletion()
        {
            if (_waveElapsed >= _waveDuration) { StopCompletion(); return; }
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_waveSteps[i] < 0) continue;
                float localTime = _waveElapsed - _waveSteps[i] * CompletionStepDelay;
                float weight = localTime <= 0 ? 0 : localTime < CompletionFadeIn
                    ? localTime / CompletionFadeIn
                    : 1 - (localTime - CompletionFadeIn) / CompletionFadeOut;
                _cells[i].SetCompletionFlash(Mathf.Clamp01(weight));
            }
        }
        private void StopCompletion()
        {
            if (!_wavePlaying) return;
            _wavePlaying = false;
            foreach (var cell in _cells) cell.SetCompletionFlash(0);
        }
        protected override void OnClose()
        {
            StopCompletion();
            base.OnClose();
        }
        protected override void OnDestroy()
        {
            SudokuThemeManager.Changed -= RefreshTheme;
            StopCompletion();
            base.OnDestroy();
        }
        private void LayoutBoard()
        {
            Vector2 size = rectTransform.rect.size;
            if (size == _lastSize) return;
            _lastSize = size;
            Vector2 cellSize = size / 9f;
            for (int i = 0; i < _cells.Count; i++) Place(_cells[i].rectTransform, new Vector2(i % 9 * cellSize.x, -(i / 9) * cellSize.y), cellSize);
            for (int i = 0; i <= 9; i++)
            {
                float width = _vertical[i].rect.width, height = _horizontal[i].rect.height;
                Place(_vertical[i], new Vector2(Mathf.Clamp(i * cellSize.x - width / 2, 0, size.x - width), 0), new Vector2(width, size.y));
                Place(_horizontal[i], new Vector2(0, -Mathf.Clamp(i * cellSize.y - height / 2, 0, size.y - height)), new Vector2(size.x, height));
                _vertical[i].SetAsLastSibling(); _horizontal[i].SetAsLastSibling();
            }
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
    }
}
