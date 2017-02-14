using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogBoxQuests : DialogBox
{
    public Transform QuestItemListPanel;
    public GameObject QuestItemPrefab;

    public override void Show()
    {
        base.Show();

        ClearInterface();
        GenerateInterface();
    }

    private void ClearInterface()
    {
        var childrens = QuestItemListPanel.Cast<Transform>().ToList();
        foreach (var child in childrens)
        {
            Destroy(child.gameObject);
        }
    }

    private void GenerateInterface()
    {
        List<Quest> quests = World.Current.Quests.Where(IsQuestAvailable).ToList();

        foreach (Quest quest in quests)
        {
            var go = Instantiate(QuestItemPrefab);
            go.transform.SetParent(QuestItemListPanel);

            var questItemBehaviour = go.GetComponent<DialogBoxQuestItem>();
            questItemBehaviour.Setup(this, quest);
        }
    }

    private static bool IsQuestAvailable(Quest quest)
    {
        if (quest.IsAccepted)
        {
            return false;
        }

        if (quest.Requirements.Count == 0)
        {
            return true;
        }

        List<Quest> preQuests = World.Current.Quests.Where(q => quest.Requirements.Contains(q.Name)).ToList();
        return preQuests.All(q => q.IsCompleted);
    }
}