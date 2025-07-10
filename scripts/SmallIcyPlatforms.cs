using Godot;
using System;
namespace JumpAndRun.scripts
{
	public partial class SmallIcyPlatforms : Platform
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
