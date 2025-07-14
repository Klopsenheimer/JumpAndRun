using Godot;
using System;
namespace JumpAndRun.scripts
{
	public partial class PowerupJetpack : Area2D, IPowerup
	{
		public int Width { get; } = 20;
		public int Height { get; } = 20;
		public bool IsCollected { get; private set; } = false;
		
		private ColorRect powerupVisual;
		private ColorRect powerupBorder;
		
		public override void _Ready()
		{
			powerupVisual = GetNode<ColorRect>("PowerupVisual");
			powerupBorder = GetNode<ColorRect>("PowerupBorder");
			// multiplierLabel = GetNode<Label>("MultiplierLabel");
		}
		
		public bool CheckCollision(Player player)
		{
			if (IsCollected) return false;

			var playerRect = new Rect2(player.GlobalPosition.X - player.Width / 2, 
									  player.GlobalPosition.Y - player.Height / 2, 
									  player.Width, player.Height);
			var powerupRect = new Rect2(GlobalPosition.X - Width / 2, 
									   GlobalPosition.Y - Height / 2, 
									   Width, Height);
			
			return playerRect.Intersects(powerupRect);
		}
		
		public void OnCollision(Player player)
		{
			if (!IsCollected)
			{
				IsCollected = true;
				player.ActivateJetpack(2.75f);
				Visible = false;
			}
		}
		public void OnBodyEntered(Node2D body) => OnCollision(body as Player);
	}
}
