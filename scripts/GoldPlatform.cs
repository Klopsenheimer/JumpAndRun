using Godot;

namespace JumpAndRun.scripts
{
	public partial class GoldPlatform : Platform
	{
		private bool hasBeenUsed = false;
		private bool isMovingX = false;
		private bool isMovingY = false;
		private float moveSpeed;
		private float moveRange = 100f;
		private Vector2 startPosition;
		private float currentDistance = 0f;

		public override void _Ready()
		{
			base._Ready();
			startPosition = Position;

			if (GD.Randf() < 0.8f) isMovingX = true;
			else isMovingY = true;
		}

		public override void _Process(double delta)
		{
			if (isMovingX) MoveX((float)delta);
			else if (isMovingY) MoveY((float)delta);
		}

		private void MoveX(float delta)
		{
			if (Position.Y > 10000) moveSpeed = 50;
			else moveSpeed = Position.Y / 1000;
			currentDistance += moveSpeed * delta;
			Position = new Vector2(
				startPosition.X + Mathf.Sin(currentDistance) * moveRange,
				startPosition.Y
			);
		}

		private void MoveY(float delta)
		{
			if (Position.Y > 10000) moveSpeed = 50;
			else moveSpeed = Position.Y / 1000;
			currentDistance += moveSpeed * delta;
			Position = new Vector2(
				startPosition.X,
				startPosition.Y + Mathf.Sin(currentDistance) * (moveRange * 0.5f) 
			);
		}

		public void OnPlayerLanded(Player player)
		{
			if (!hasBeenUsed)
			{
				hasBeenUsed = true;
				player.ApplyScoreMultiplier(2);
				GD.Print($"Gold platform bonus! Updated Score: {player.Score}");
			}
		}
	}
}
