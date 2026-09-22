using System;
using UnityEngine;

// エリアID(0-7) → 物理カラーカードの正解色 の対応表．
// この対応は条件A/Bで不変（実環境の配置が固定のため）．
[CreateAssetMenu(fileName = "AreaColorConfig", menuName = "2BackTask/Area Color Config")]
public class AreaColorConfig : ScriptableObject
{
    [Serializable]
    public class PaletteColor
    {
        public string colorName = "Color";
        public Color color = Color.white;
    }

    public const int AreaCount = 8;

    public PaletteColor[] palette = new PaletteColor[AreaCount];
    public int[] correctColorIndexByArea = new int[AreaCount];

    public int GetCorrectColorIndex(int areaId)
    {
        if (areaId < 0 || areaId >= correctColorIndexByArea.Length)
        {
            Debug.LogError($"AreaColorConfig: 不正なareaId={areaId}");
            return 0;
        }
        return correctColorIndexByArea[areaId];
    }

    private void Reset()
    {
        string[] names = { "Red", "Blue", "Green", "Yellow", "Purple", "Orange", "Cyan", "Pink" };
        Color[] colors =
        {
            new Color(0.85f, 0.10f, 0.10f),
            new Color(0.10f, 0.35f, 0.85f),
            new Color(0.15f, 0.65f, 0.25f),
            new Color(0.95f, 0.85f, 0.10f),
            new Color(0.55f, 0.20f, 0.75f),
            new Color(0.95f, 0.55f, 0.10f),
            new Color(0.30f, 0.80f, 0.85f),
            new Color(0.95f, 0.45f, 0.65f),
        };

        palette = new PaletteColor[AreaCount];
        correctColorIndexByArea = new int[AreaCount];
        for (int i = 0; i < AreaCount; i++)
        {
            palette[i] = new PaletteColor { colorName = names[i], color = colors[i] };
            correctColorIndexByArea[i] = i;
        }
    }
}
