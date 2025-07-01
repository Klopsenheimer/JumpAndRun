using Godot;
using System;

namespace JumpAndRun.scripts
{
    public partial class RoughPlatform : Platform
    {
        public override void _Ready()
        {
            base._Ready();
        }

        public void OnPlayerLanded(Player player)
        {
            player.JumpStrength = Math.Min(player.JumpStrength + 20, 500);
            GD.Print($"Rough platform landed! Jump strength increased to: {player.JumpStrength}");
        }
    }
}