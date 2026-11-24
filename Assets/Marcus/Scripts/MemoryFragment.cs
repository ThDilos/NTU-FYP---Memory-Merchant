using UnityEngine;

public class MemoryFragment : Collectible
{
    public enum MemoryFragmentType
    {
        PERSON,
        OBJECT,
        PLACE
    }

    public override void OnCollect()
    {
        base.OnCollect();
        MemoryCollectionEffects.FragmentCollected(transform);
    }
}
