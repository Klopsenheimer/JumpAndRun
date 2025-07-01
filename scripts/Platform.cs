using Godot;

public partial class Platform : StaticBody2D
{
	private CollisionShape2D _collisionShape;
	private ColorRect _visual;
	private ColorRect _border;

	public override void _Ready()
	{
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_visual = GetNode<ColorRect>("PlatformVisual");
		_border = GetNode<ColorRect>("PlatformBorder");		
	}

	public void SetSize(float width, float height)
	{
		if (_collisionShape?.Shape is RectangleShape2D rectShape) rectShape.Size = new Vector2(width, height);
		if (_visual != null)
		{
			_visual.OffsetLeft = -width/2;
			_visual.OffsetRight = width/2;
			_visual.OffsetTop = -height/2;
			_visual.OffsetBottom = height/2;
		}

		if (_border != null)
		{
			_border.OffsetLeft = -width/2;
			_border.OffsetRight = width/2;
			_border.OffsetTop = -height/2;
			_border.OffsetBottom = height/2;
		}
	}
}
