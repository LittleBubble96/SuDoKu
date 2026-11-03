using System;
using Cysharp.Threading.Tasks;
using GameLogic.Sudoku;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>通用奖励飘字；资源异步加载，播放完成即释放，不依赖领取窗口生命周期。</summary>
    public static class RewardEffectPlayer
    {
        public const float ScatterRadius = 42f, GrowSeconds = .18f, HoldSeconds = .12f;
        public const float FlySeconds = .65f, FlyDistance = 70f;

        public static UniTask PlayCoinAsync(int amount, Vector3 worldPosition) =>
            PlayAsync(true, amount, worldPosition);

        public static UniTask PlayPropAsync(int amount, Vector3 worldPosition, Sprite icon = null) =>
            PlayAsync(false, amount, worldPosition, icon);

        public static async UniTask PlayAsync(bool isCoin, int amount, Vector3 worldPosition, Sprite propIcon = null)
        {
            var token = GameModule.Base.GetCancellationTokenOnDestroy();
            var coinView = SudokuPresenter.Coins;
            GameObject effect = null;
            try
            {
                effect = await GameModule.Resource.LoadGameObjectAsync("RewardEffect", UIModule.UIRoot, token);
                token.ThrowIfCancellationRequested();
                effect.name = "RewardEffect";
                var rect = (RectTransform)effect.transform;
                rect.position = worldPosition;
                var canvas = effect.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = (int)UILayer.Tips * UIModule.LAYER_DEEP + UIModule.WINDOW_DEEP;
                var group = effect.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false; group.interactable = false;
                var coinRoot = effect.transform.Find("Coin").gameObject;
                var propRoot = effect.transform.Find("Prop").gameObject;
                coinRoot.SetActive(isCoin); propRoot.SetActive(!isCoin);
                var active = isCoin ? coinRoot : propRoot;
                var count = active.transform.Find("m_num").GetComponent<TMP_Text>();
                count.text = isCoin ? amount.ToString() : "× " + amount;
                UIThemeColor.Bind(count, SudokuThemeRole.Text);
                var coins = coinRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                var scales = new Vector3[coins.Length];
                if (isCoin)
                {
                    for (int i = 0; i < coins.Length; i++)
                    {
                        coins[i].rectTransform.anchoredPosition += UnityEngine.Random.insideUnitCircle * ScatterRadius;
                        scales[i] = coins[i].transform.localScale * UnityEngine.Random.Range(.6f, 1.1f);
                        coins[i].transform.localScale = Vector3.zero;
                    }
                }
                else
                {
                    var icon = propRoot.transform.Find("icon").GetComponent<UnityEngine.UI.Image>();
                    if (propIcon != null) icon.sprite = propIcon;
                    // 默认道具为可换色的提示；传入自有彩色道具时保留原色。
                    if (propIcon == null) UIThemeColor.Bind(icon, SudokuThemeRole.Accent);
                }
                foreach (var graphic in effect.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    graphic.raycastTarget = false;
                Vector2 start = rect.anchoredPosition;
                float reveal = isCoin ? GrowSeconds + HoldSeconds : 0f;
                float duration = reveal + FlySeconds;
                for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                {
                    if (isCoin)
                    {
                        float grow = Mathf.Clamp01(elapsed / GrowSeconds);
                        float eased = 1f - Mathf.Pow(1f - grow, 3f);
                        for (int i = 0; i < coins.Length; i++) coins[i].transform.localScale = scales[i] * eased;
                    }
                    float fly = Mathf.Clamp01((elapsed - reveal) / FlySeconds);
                    rect.anchoredPosition = start + Vector2.up * (FlyDistance * fly);
                    group.alpha = 1f - Mathf.SmoothStep(0f, 1f, fly);
                    await UniTask.Yield(cancellationToken: token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (effect != null) UnityEngine.Object.Destroy(effect);
                if (isCoin && !token.IsCancellationRequested && ReferenceEquals(coinView, SudokuPresenter.Coins))
                    coinView?.PlayGainAnimation();
            }
        }
    }
}
