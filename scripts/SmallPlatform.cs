using Godot;
using System;
namespace JumpAndRun.scripts
{
	public partial class SmallPlatform : Platform
	{
		private CollisionShape2D collisionShape;
		
		public override void _Ready()
		{
			collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		}
		
		public void SetSize(float width, float height)
		{
			if (collisionShape?.Shape is RectangleShape2D rectShape) rectShape.Size = new Vector2(width, height);
		}
		
	}
}
