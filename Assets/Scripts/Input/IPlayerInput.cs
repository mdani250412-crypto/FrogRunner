namespace FrogRunner.Input
{
    /// <summary>
    /// Abstraction so gameplay reads input without caring about keyboard/touch/network.
    /// </summary>
    public interface IPlayerInput
    {
        float Horizontal { get; }
        float Vertical { get; }
        bool JumpHeld { get; }
        bool JumpPressed { get; }
        bool DashPressed { get; }
        bool CrouchHeld { get; }
        bool PausePressed { get; }
        void Tick(bool active);
    }
}
