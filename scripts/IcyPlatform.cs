using Godot;

namespace JumpAndRun.scripts
{
    public partial class IcyPlatform : Platform
    {
        public override void _Ready()
        {
            base._Ready();
        }

        public void OnPlayerLanded(Player player)
        {
            player.ApplyIceEffect();
            GD.Print("Player landed on icy platform - applying ice effect");
        }
    }
}