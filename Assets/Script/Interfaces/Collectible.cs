using UnityEngine;

// These are interactibles that will go into inventory
public class Collectible : Interactible
{
    [Header("Display")]
    [SerializeField] protected string displayName;
    public string DisplayName { get; }
    [SerializeField] protected Sprite sprite;
    public string Sprite { get; }
    [SerializeField] protected bool collectable;
    public bool Collectable { get; }

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