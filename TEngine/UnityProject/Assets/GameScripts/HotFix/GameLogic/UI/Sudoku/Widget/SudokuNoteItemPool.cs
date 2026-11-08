using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>一张棋盘共用的笔记池，使用 UISudoku 已加载的本地模板，无额外资源 IO。</summary>
    internal sealed class SudokuNoteItemPool
    {
        private readonly UIWidget _owner;
        private readonly GameObject _template;
        private readonly Transform _storage;
        private readonly Stack<NoteItem> _unused = new Stack<NoteItem>();

        public SudokuNoteItemPool(UIWidget owner, GameObject template)
        {
            _owner = owner;
            _template = template;
            _template.SetActive(false);
            var storage = new GameObject("NoteItemPool", typeof(RectTransform));
            _storage = storage.transform;
            _storage.SetParent(owner.transform, false);
            storage.SetActive(false);
        }

        public NoteItem Rent(Transform parent)
        {
            var item = _unused.Count > 0 ? _unused.Pop() :
                _owner.CreateWidgetByPrefab<NoteItem>(_template, _storage, false);
            item.transform.SetParent(parent, false);
            item.transform.SetAsLastSibling();
            return item;
        }

        public void Return(NoteItem item)
        {
            item.Visible = false;
            item.ResetDisplay();
            item.transform.SetParent(_storage, false);
            _unused.Push(item);
        }
    }
}
