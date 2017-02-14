using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ContextMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject contextualMenuItemPrefab;
    
    public void Open(Tile tile)
    {
        gameObject.SetActive(true);

        IEnumerable<IContextActionProvider> providers = GetContextualActionProviderOnTile(tile);
        IEnumerable<ContextMenuAction> contextActions = GetContextualMenuActionFromProviders(providers);

        ClearInterface();
        BuildInterface(contextActions);
    }
    
    private void ClearInterface()
    {
        var childrens = gameObject.transform.Cast<Transform>().ToList();
        foreach (var child in childrens)
        {
            Destroy(child.gameObject);
        }
    }

    private void BuildInterface(IEnumerable<ContextMenuAction> contextualActions)
    {
        gameObject.transform.position = Input.mousePosition + new Vector3(10, -10, 0);

        bool characterSelected = WorldController.Instance.MouseController.IsCharacterSelected;

        foreach (var contextMenuAction in contextualActions)
        {
            if ((!contextMenuAction.RequiresCharacterSelection || !characterSelected) && contextMenuAction.RequiresCharacterSelection) continue;

            GameObject instance = Instantiate(contextualMenuItemPrefab);
            instance.transform.SetParent(gameObject.transform);

            ContextMenuItem contextMenuItem = instance.GetComponent<ContextMenuItem>();
            contextMenuItem.ContextMenu = this;
            contextMenuItem.Action = contextMenuAction;
            contextMenuItem.BuildInterface();
        }
    }

    private static IEnumerable<IContextActionProvider> GetContextualActionProviderOnTile(Tile tile)
    {
        List<IContextActionProvider> providers = new List<IContextActionProvider>();

        if (tile.Furniture != null)
        {
            providers.Add(tile.Furniture);
        }

        if (tile.Characters != null)
        {
            providers.AddRange(tile.Characters.Cast<IContextActionProvider>());
        }

        if (tile.Inventory != null)
        {
            providers.Add(tile.Inventory);
        }

        return providers;
    }

    private IEnumerable<ContextMenuAction> GetContextualMenuActionFromProviders(IEnumerable<IContextActionProvider> providers)
    {
        List<ContextMenuAction> contextualActions = new List<ContextMenuAction>();
        foreach (IContextActionProvider contextualActionProvider in providers)
        {
            contextualActions.AddRange(contextualActionProvider.GetContextMenuActions(this));
        }

        contextualActions = contextualActions.OrderBy(c => c.Text).ToList();
        return contextualActions;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}