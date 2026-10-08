// Expected Behaviour: The key that on top of the fridge.
// When closing in, play animation of "Mother use key to unlock & open the locked closet"
// Interact to Add "Key" to MemoryEchoInventory
public class Key : MemoryEcho
{
    public override void Interact()
    {
        base.Collect();
    }
}