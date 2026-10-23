#if !TE_RELEASE
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed class GmTopBtnWidget : UIWidget
    {
        private Button _button;
        private TextMeshProUGUI _title;
        private Action<string> _clicked;
        protected override void OnCreate()
        {
            _button = gameObject.GetComponent<Button>();
            _title = FindChildComponent<TextMeshProUGUI>("Text");
            _button.onClick.AddListener(() => _clicked?.Invoke(_title.text));
        }
        public void Init(string title, Action<string> clicked) { _title.text = title; _clicked = clicked; }
    }

    public sealed class GmGroupWidget : UIWidget
    {
        private RectTransform _content;
        private TextMeshProUGUI _title;
        protected override void OnCreate()
        {
            _content = FindChildComponent<RectTransform>("Content");
            _title = FindChildComponent<TextMeshProUGUI>("Title/Image/Text");
        }
        public void SetTitle(string title) => _title.text = title;
        public async UniTask CreateBtnAsync(string label, Action action)
        {
            var item = await CreateWidgetByPathAsync<GmBtnAndInputItemWidget>(_content, "BtnAndInput_GM");
            if (item == null) throw new InvalidOperationException("GM 按钮预制体加载失败。");
            item.InitBtn(label, action);
        }
        public async UniTask CreateBtnAndInputAsync(string label, Action<string> action)
        {
            var item = await CreateWidgetByPathAsync<GmBtnAndInputItemWidget>(_content, "BtnAndInput_GM");
            if (item == null) throw new InvalidOperationException("GM 输入项预制体加载失败。");
            item.InitBtnAndInput(label, action);
        }
    }

    public sealed class GmBtnAndInputItemWidget : UIWidget
    {
        private Button _button;
        private TMP_InputField _input;
        private TextMeshProUGUI _label;
        private Action _clicked;
        private Action<string> _submitted;
        protected override void OnCreate()
        {
            _button = FindChildComponent<Button>("Button");
            _input = FindChildComponent<TMP_InputField>("InputField");
            _label = FindChildComponent<TextMeshProUGUI>("Button/Text");
            _button.onClick.AddListener(() => { if (_submitted != null) _submitted(_input.text); else _clicked?.Invoke(); });
        }
        public void InitBtn(string label, Action clicked)
        {
            _label.text = label; _clicked = clicked; _submitted = null; _input.gameObject.SetActive(false);
        }
        public void InitBtnAndInput(string label, Action<string> submitted)
        {
            _label.text = label; _submitted = submitted; _clicked = null; _input.gameObject.SetActive(true);
        }
    }
}
#endif
