using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DialogBoxQuestItem : MonoBehaviour
{
    public Quest Quest { get; private set; }
    [SerializeField]
    private Text titleText;
    [SerializeField]
    private Text descriptionText;
    [SerializeField]
    private Text goalText;
    [SerializeField]
    private Text rewardText;
    [SerializeField]
    private Button acceptButton;
    private DialogBoxQuests parentDialogBoxQuests;

    public void Update()
    {
        if (parentDialogBoxQuests == null)
        {
            GenerateInterface();
        }
    }

    public void Setup(DialogBoxQuests parent, Quest item)
    {
        parentDialogBoxQuests = parent;
        Setup(item);
    }

    public void Setup(Quest item)
    {
        Quest = item;
        GenerateInterface();
    }

    private void GenerateInterface()
    {
        if (Quest == null)
        {
            return;
        }

        titleText.text = Quest.Name;
        descriptionText.text = Quest.Description;
        goalText.text = "Goals: " + Environment.NewLine + string.Join(Environment.NewLine, Quest.Goals.Select(g => GetGoalText(g)).ToArray());
        rewardText.text = "Rewards: " + Environment.NewLine + string.Join(Environment.NewLine, Quest.Rewards.Select(r => "   - " + r.Description).ToArray());
        acceptButton.gameObject.SetActive(!Quest.IsAccepted);
    }

    private static string GetGoalText(QuestGoal questGoal)
    {
        if (questGoal.IsCompleted)
        {
            return "<color=green>   - " + questGoal.Description + "</color>";
        }
        return "<color=red>   - " + questGoal.Description + "</color>";
    }

    public void OnAccept()
    {
        Quest.IsAccepted = true;

        if (parentDialogBoxQuests != null)
        {
            parentDialogBoxQuests.Close();
        }
    }
}