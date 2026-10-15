using UnityEngine;

// These are interactibles that will go into inventory
public class Collectible : Interactible
{
    [Header("Display")]
    [SerializeField] protected string displayName;
    public string DisplayName => displayName;
    [SerializeField] protected Sprite sprite;
    public Sprite Sprite => sprite;
    [SerializeField] protected bool collectable;
    public bool Collectable => collectable;

    public virtual void Collect()
    {
        if (LevelInventory.Instance != null) LevelInventory.Instance.Collect(this);
    }

    public virtual void OnCollect()
    {
        Debug.Log($"[Collectible] Collected {displayName}");
    }
}
