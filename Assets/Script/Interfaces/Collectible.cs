using UnityEngine;

// These are interactibles that will go into inventory
public class Collectible : Interactible
{
    [Header("Display")]
    [SerializeField] protected string displayName;
    public string DisplayName { get { return displayName; } }
    [SerializeField] protected Sprite sprite;
    public Sprite Sprite { get { return sprite; } }
    public override void Interact()
    {
        Collect();
    }

    public virtual void Collect()
    {
        if (LevelInventory.Instance != null) LevelInventory.Instance.Collect(this);
        OnCollect();
    }

    public virtual void OnCollect()
    {
        Debug.Log($"[Collectible] Collected {displayName}");
    }
}