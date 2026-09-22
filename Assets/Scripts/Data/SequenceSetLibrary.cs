using System;
using UnityEngine;

// 参加者ごとに割り当てる「8エリアの訪問順」の決め打ちシーケンス集．
// ランダム化はせず，起動画面で実験者が選択したセットをそのまま使用する．
[CreateAssetMenu(fileName = "SequenceSetLibrary", menuName = "2BackTask/Sequence Set Library")]
public class SequenceSetLibrary : ScriptableObject
{
    [Serializable]
    public class SequenceSet
    {
        public string setName = "SequenceSet_1";
        public int[] areaVisitOrder = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
    }

    public SequenceSet[] sequenceSets;

    public SequenceSet Get(int index)
    {
        if (sequenceSets == null || sequenceSets.Length == 0)
        {
            Debug.LogError("SequenceSetLibrary: sequenceSetsが空です");
            return new SequenceSet();
        }
        int clamped = Mathf.Clamp(index, 0, sequenceSets.Length - 1);
        return sequenceSets[clamped];
    }

    private void Reset()
    {
        sequenceSets = new SequenceSet[]
        {
            new SequenceSet { setName = "SequenceSet_1", areaVisitOrder = new[] { 0, 1, 2, 3, 4, 5, 6, 7 } },
            new SequenceSet { setName = "SequenceSet_2", areaVisitOrder = new[] { 3, 7, 1, 5, 0, 6, 2, 4 } },
            new SequenceSet { setName = "SequenceSet_3", areaVisitOrder = new[] { 6, 2, 5, 0, 7, 3, 1, 4 } },
            new SequenceSet { setName = "SequenceSet_4", areaVisitOrder = new[] { 2, 5, 7, 0, 4, 1, 6, 3 } },
        };
    }
}
