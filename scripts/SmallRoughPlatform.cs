using Godot;
using System;

namespace JumpAndRun.scripts
{
	public partial class SmallRoughPlatform : Platform
	{
		private CollisionShape2D collisionShape;
		private bool hasBeenUsed;
		public override void _Ready() => collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		public void OnPlayerLanded(Player player)
		{
			if(hasBeenUsed) return; 
			hasBeenUsed = true;
			player.JumpStrength = Math.Min(player.JumpStrength + 10, 500);
			GD.Print($"Small rough platform landed! JumpStrength increased to: {player.JumpStrength}");	
		}
	}
}
