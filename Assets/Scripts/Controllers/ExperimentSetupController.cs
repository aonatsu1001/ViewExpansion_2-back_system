using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 起動画面：参加者ID・条件・エリア順序セットの入力を受け付け，
// ExperimentContextに確定させてから練習セッションを開始する．
public class ExperimentSetupController : MonoBehaviour
{
    [HideInInspector] public TMP_InputField participantIdInput;
    [HideInInspector] public SelectorControl conditionSelector;
    [HideInInspector] public SelectorControl sequenceSetSelector;
    [HideInInspector] public GameObject setupPanel;
    [HideInInspector] public ExperimentSessionController session;
    [HideInInspector] public SequenceSetLibrary sequenceLibrary;

    public void PopulateOptions()
    {
        conditionSelector.Setup(new List<string> { "Condition A (Proposed)", "Condition B (Baseline)" });

        var sequenceOptions = new List<string>();
        if (sequenceLibrary != null && sequenceLibrary.sequenceSets != null)
        {
            foreach (var s in sequenceLibrary.sequenceSets)
            {
                sequenceOptions.Add(s.setName);
            }
        }
        sequenceSetSelector.Setup(sequenceOptions);
    }

    public void OnStartPressed()
    {
        var condition = conditionSelector.CurrentIndex == 0 ? ExperimentCondition.Proposed : ExperimentCondition.Baseline;
        ExperimentContext.Configure(participantIdInput.text, condition, sequenceSetSelector.CurrentIndex);

        setupPanel.SetActive(false);
        session.StartPractice();
    }
}
