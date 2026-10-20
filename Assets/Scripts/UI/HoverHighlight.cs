using UnityEngine;
using UnityEngine.EventSystems;

// カーソルが乗っている間だけボタンを少し上に持ち上げて，ホバー状態（選択候補）を
// 分かりやすくする．絶対位置を保存せず，乗った分だけ加算・外れた分だけ減算するため，
// GridLayoutGroup等による初期配置のタイミングに依存しない．
public class HoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float HoverOffsetY = 12f;

    private RectTransform rect;
    private bool isHovered;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered) return;
        isHovered = true;
        rect.anchoredPosition += new Vector2(0f, HoverOffsetY);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered) return;
        isHovered = false;
        rect.anchoredPosition -= new Vector2(0f, HoverOffsetY);
    }
}
