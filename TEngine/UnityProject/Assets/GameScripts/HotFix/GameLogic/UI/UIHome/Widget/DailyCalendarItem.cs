using System;
using GameLogic.Sudoku;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class DailyCalendarItem : UIWidget
    {
        private XYButton _button;
        private GameObject _background, _progress, _success, _selection, _outline;
        private UnityEngine.UI.Image _fill;
        private TMP_Text _number;
        private UIThemeColor _numberColor, _fillColor;
        private DateTime _date;
        private Action<DateTime> _select;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _button = CreateWidget<XYButton>("Btn");
            _button.OnAddListener(() => _select?.Invoke(_date));
            _background = FindChild("Btn/Bg").gameObject;
            _progress = FindChild("Btn/Bg/ProgressRoot").gameObject;
            _success = FindChild("Btn/Bg/SuccessRoot").gameObject;
            _selection = FindChild("Btn/Bg/SelectRoot").gameObject;
            _outline = FindChild("Btn/Bg/outLine").gameObject;
            _fill = FindChildComponent<UnityEngine.UI.Image>("Btn/Bg/ProgressRoot/Fill");
            _fill.type = UnityEngine.UI.Image.Type.Filled;
            _fill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            _number = FindChildComponent<TMP_Text>("Btn/Bg/m_num");
            _numberColor = UIThemeColor.Bind(_number, SudokuThemeRole.Text);
            // 选中底色在最下层，数字和完成标记始终覆盖在状态图形上方。
            _selection.transform.SetAsFirstSibling();
            _number.transform.SetAsLastSibling();
            _success.transform.SetAsLastSibling();
            Theme("Btn/Bg", SudokuThemeRole.KeyBackground);
            Theme("Btn/Bg/ProgressRoot", SudokuThemeRole.Matching);
            _fillColor = UIThemeColor.Bind(_fill, SudokuThemeRole.Accent);
            Theme("Btn/Bg/SelectRoot", SudokuThemeRole.Accent);
            Theme("Btn/Bg/SuccessRoot", SudokuThemeRole.Accent);
            Theme("Btn/Bg/SuccessRoot/icon", SudokuThemeRole.Surface);
            Theme("Btn/Bg/outLine", SudokuThemeRole.Accent);
        }

        public void Refresh(DateTime date, bool inMonth, DateTime today, DateTime selected, SudokuDailySaveData record, Action<DateTime> select)
        {
            _date = date; _select = select;
            _background.SetActive(inMonth);
            _button.Interactable = inMonth && SudokuDailyRules.CanStart(date, today);
            if (!inMonth) return;
            bool completed = record?.completed == true;
            bool isSelected = date == selected;
            _number.text = date.Day.ToString();
            _progress.SetActive(record != null && !completed);
            _success.SetActive(completed);
            _selection.SetActive(isSelected);
            _outline.SetActive(date == today && !isSelected);
            _fill.fillAmount = record == null ? 0 : Mathf.Clamp01(record.progressPercent / 100f);
            _fillColor.SetRole(isSelected ? SudokuThemeRole.Surface : SudokuThemeRole.Accent);
            _numberColor.SetRole(isSelected ? SudokuThemeRole.Surface : date > today ? SudokuThemeRole.MutedText : record != null ? SudokuThemeRole.Accent : SudokuThemeRole.Text);
        }

        private void Theme(string path, SudokuThemeRole role)
        {
            var graphic = FindChild(path).GetComponent<UnityEngine.UI.Image>();
            if (graphic != null) UIThemeColor.Bind(graphic, role);
        }
    }
}
