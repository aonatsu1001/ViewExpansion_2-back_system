using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TMP_Dropdownの代わりに使う簡易セレクタ（< 現在値 > の形でマウスのみで選択できる）．
// フィールドはBootstrapが構築時に直接代入するため，Setup()呼び出し前にButton参照が
// 揃っていることを前提とする（Awakeでのイベント購読は行わない＝生成順序に依存しない）．
public class SelectorControl : MonoBehaviour
{
    [HideInInspector] public TextMeshProUGUI label;
    [HideInInspector] public Button prevButton;
    [HideInInspector] public Button nextButton;

    private List<string> options;
    private bool wired;

    public int CurrentIndex { get; private set; }

    public void Setup(List<string> newOptions)
    {
        options = newOptions;
        CurrentIndex = 0;

        if (!wired)
        {
            prevButton.onClick.AddListener(() => Move(-1));
            nextButton.onClick.AddListener(() => Move(1));
            wired = true;
        }

        Refresh();
    }

    private void Move(int delta)
    {
        if (options == null || options.Count == 0) return;
        CurrentIndex = (CurrentIndex + delta + options.Count) % options.Count;
        Refresh();
    }

    private void Refresh()
    {
        if (options != null && options.Count > 0 && label != null)
        {
            label.text = options[CurrentIndex];
        }
    }
}
