using Godot;
	namespace JumpAndRun.scripts{
	public partial class PowerupMultiplier : Area2D
	{
		public int Width { get; } = 20;
		public int Height { get; } = 20;
		public bool IsCollected { get; private set; } = false;
		
		private ColorRect powerupVisual;
		private ColorRect powerupBorder;
		private Label multiplierLabel;

		public override void _Ready()
		{
			powerupVisual = GetNode<ColorRect>("PowerupVisual");
			powerupBorder = GetNode<ColorRect>("PowerupBorder");
			multiplierLabel = GetNode<Label>("MultiplierLabel");
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
				player.ApplyScoreMultiplier(1.2f);
				Visible = false;
			}
		}
		
		public void OnBodyEntered(Node2D body)
		{
			if (body is Player player)
			{
				OnCollision(player);
			}
		}
	}
}
