using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainScreenQuestList : MonoBehaviour
{
    [SerializeField]
    private Transform questItemListPanel;
    [SerializeField]
    private GameObject questItemPrefab;

    private readonly List<Quest> visibleQuests;
    private readonly float checkDelayInSeconds;
    private float totalDeltaTime;

    public MainScreenQuestList()
    {
        checkDelayInSeconds = 0.5f;
        visibleQuests = new List<Quest>();
    }

    public void Update()
    {
        totalDeltaTime += Time.deltaTime;
        if (!(totalDeltaTime >= checkDelayInSeconds)) return;

        totalDeltaTime = 0f;
        RefreshInterface();
    }

    private void RefreshInterface()
    {
        ClearInterface();
        BuildInterface();
    }
    
    private void ClearInterface()
    {
        List<Quest> quests = World.Current.Quests.Where(q => q.IsAccepted && !q.IsCompleted).ToList();
        List<Transform> childrens = questItemListPanel.Cast<Transform>().ToList();
        foreach (Transform child in childrens)
        {
            DialogBoxQuestItem questItem = child.GetComponent<DialogBoxQuestItem>();
            if (quests.Contains(questItem.Quest)) continue;

            visibleQuests.Remove(questItem.Quest);
            Destroy(child.gameObject);
        }
    }

    private void BuildInterface()
    {
        List<Quest> quests = World.Current.Quests.Where(q => q.IsAccepted && !q.IsCompleted).ToList();
        foreach (Quest quest in quests)
        {
            if (visibleQuests.Contains(quest)) continue;
            GameObject instance = Instantiate(questItemPrefab);
            instance.transform.SetParent(questItemListPanel);

            DialogBoxQuestItem questItemBehaviour = instance.GetComponent<DialogBoxQuestItem>();
            questItemBehaviour.Setup(quest);
            visibleQuests.Add(quest);
        }
    }
}