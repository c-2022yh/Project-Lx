public interface IInteractable
{
    string InteractionText { get; }

    void Interact(Player player);
}