public interface IInteractable
{
    string InteractionText { get; }

    void Interact(global::ProjectLx.PlayerRuntime.PlayerActor player);
}