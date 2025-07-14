using Godot;
using System;

namespace JumpAndRun.scripts
{
	public partial class RoughPlatform : Platform
	{
		private bool hasBeenUsed;
		public override void _Ready() => base._Ready();
		
		public void OnPlayerLanded(Player player)
		{
			if(hasBeenUsed) return; 
			hasBeenUsed = true;
			player.JumpStrength = Math.Max(player.JumpStrength - 10, 400);
			GD.Print($"Rough platform landed! Jump strength decreased to: {player.JumpStrength}");
		}
	}
}
