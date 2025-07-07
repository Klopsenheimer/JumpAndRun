using Godot;
using JumpAndRun.scripts;
using System;

public partial class Player : CharacterBody2D
{
	[Signal] public delegate void PlayerDiedEventHandler();

	public string PlayerName { get; set; } = "pexlover";
	public float Score { get; set; } = 0;
	public float XVelocity { get; set; } = 200;
	public float JumpStrength { get; set; } = 400;
	public bool IsGrounded { get; set; } = false;
	public bool CanDoubleJump { get; set; } = true;
	public int Width { get; set; } = 40;
	public int Height { get; set; } = 40;
	
	// Animation
	private AnimatedSprite2D animatedSprite;
	private string currentDirection = "right";
	private bool wasGrounded = false;

	private const float Gravity = 980;
	private const float MaxFallSpeed = 800;
	private const float GroundLevel = 500;
	private bool hasDoubleJumped = false;
	private float highestY = 400;
	private float coyoteTime = 0f;
	private const float CoyoteTimeLimit = 0.1f;
	private float scoreMultiplier = 1.0f;
	private Label nameLabel;
	private bool isOnIce = false;
	private float iceTimer = 0f;
	private const float IceEffectDuration = 2f;
	private float iceSlipFactor = 0.7f;
	private bool wasOnFloorLastFrame = false;
	
	private bool jetpackActive = false;
	private float jetpackTimeLeft = 0f;
	private float jetpackLiftForce = 500f; 

	public override void _Ready()
	{
		nameLabel = GetNode<Label>("NameLabel");
		nameLabel.Text = PlayerName;
		highestY = GlobalPosition.Y;
		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		animatedSprite.Play("idle_right");
	}
	
	public void ActivateJetpack(float duration)
	{
		jetpackActive = true;
		jetpackTimeLeft = duration;
		GD.Print($"Jetpack activated for {duration} seconds");
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleInput((float)delta);

		bool wasGrounded = IsGrounded;
		IsGrounded = IsOnFloor();

		if (isOnIce)
		{
			iceTimer -= (float)delta;
			if (iceTimer <= 0)
			{
				isOnIce = false;
			}
		}
		
		if (jetpackActive)
		{
			if (jetpackTimeLeft > 0)
			{
				if (Input.IsActionPressed("jump"))
				{
					Velocity = new Vector2(Velocity.X, -jetpackLiftForce);
					animatedSprite.Play("jetpack_" + currentDirection);
				}
				else
				{
					Velocity = new Vector2(Velocity.X, Math.Min(Velocity.Y + Gravity * (float)delta, MaxFallSpeed));
					animatedSprite.Play("jetpackfall_" + currentDirection);
				}
				jetpackTimeLeft -= (float)delta;
			}
			else
			{
				jetpackActive = false;
			}
		}
		else
		{
			if (!IsGrounded)
			{
				Velocity = new Vector2(Velocity.X, Math.Min(Velocity.Y + Gravity * (float)delta, MaxFallSpeed));
				coyoteTime = wasGrounded ? CoyoteTimeLimit : Math.Max(0, coyoteTime - (float)delta);
				// Airborne animation
				if (Velocity.Y < 0)
				{
					animatedSprite.Play("jump_" + currentDirection);
				}
				else
				{
					animatedSprite.Play("fall_" + currentDirection);
				}
			}
			else
			{
				coyoteTime = CoyoteTimeLimit;
				hasDoubleJumped = false;
				CanDoubleJump = true;
				// Only play idle if not moving horizontally
				if (Mathf.Abs(Velocity.X) < 0.1f)
				{
					animatedSprite.Play("idle_" + currentDirection);
				}
			}
		}
		
		

		MoveAndSlide();

		CheckPlatformCollisions();

		if (GlobalPosition.Y < highestY)
		{
			highestY = GlobalPosition.Y;
			Score = Math.Max(0, (GroundLevel - highestY) / 10) * scoreMultiplier;
		}

		ClampToScreenBounds();
		wasOnFloorLastFrame = IsOnFloor();
		
		
	}

	private void CheckPlatformCollisions()
	{
		// Check if we just landed (transition from not on floor to on floor)
		if (IsOnFloor() && !wasOnFloorLastFrame)
		{
			for (int i = 0; i < GetSlideCollisionCount(); i++)
			{
				var collision = GetSlideCollision(i);
				var collider = collision.GetCollider();

				// Check for special platform types and call their OnPlayerLanded methods
				switch (collider)
				{
					case GoldPlatform goldPlatform:
						goldPlatform.OnPlayerLanded(this);
						break;
					case IcyPlatform icyPlatform:
						icyPlatform.OnPlayerLanded(this);
						break;
					case RoughPlatform roughPlatform:
						roughPlatform.OnPlayerLanded(this);
						break;
				}
			}
		}
	}

	public void ApplyIceEffect()
	{
		isOnIce = true;
		iceTimer = IceEffectDuration;
		GD.Print("Ice effect applied to player");
	}

	public void ApplyScoreMultiplier(float multiplier)
	{
		scoreMultiplier *= multiplier;
		GD.Print($"Score multiplier applied: {multiplier:F1}x, total multiplier: {scoreMultiplier:F2}x");
	}

	public void Reset()
	{
		GlobalPosition = new Vector2(100, 400);
		Velocity = Vector2.Zero;
		Score = 0;
		IsGrounded = false;
		CanDoubleJump = true;
		hasDoubleJumped = false;
		highestY = 400;
		coyoteTime = 0f;
		scoreMultiplier = 1.0f;
		isOnIce = false;
		iceTimer = 0f;
		wasOnFloorLastFrame = false;

		// Reset jump strength to default
		JumpStrength = 400;
	}

	private void ClampToScreenBounds()
	{
		var screenSize = GetViewportRect().Size;
		var pos = GlobalPosition;
		pos.X = Math.Max(Width / 2, Math.Min(pos.X, screenSize.X - Width / 2));
		GlobalPosition = pos;
	}

	private void HandleInput(float delta)
	{
		Vector2 velocity = Velocity;

		float currentXVelocity = XVelocity;
		if (isOnIce && IsGrounded)
		{
			currentXVelocity *= iceSlipFactor;
			velocity.X *= 0.8f; 
		}

		if (Input.IsActionPressed("move_left"))
		{
			velocity.X = -XVelocity;
			currentDirection = "left";
			if (IsGrounded)
			{
				animatedSprite.Play("walk_left");
			} 
			if (isOnIce && IsGrounded)
			{
				velocity.X -= currentXVelocity * delta * 3;
			}	
			else
			{
				velocity.X = -currentXVelocity;
			}
				
		}
		else if (Input.IsActionPressed("move_right"))
		{
			velocity.X = XVelocity;
			currentDirection = "right";
			if (IsGrounded)
			{
				animatedSprite.Play("walk_right");
			} 
			if (isOnIce && IsGrounded)
			{
				velocity.X += currentXVelocity * delta * 3;
			}
			else
			{
				velocity.X = currentXVelocity;
			}
				
		}
		else if (!isOnIce || !IsGrounded)
		{
			velocity.X = 0;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			if (IsGrounded || coyoteTime > 0)
			{
				velocity.Y = -JumpStrength;
				IsGrounded = false;
				coyoteTime = 0;
				animatedSprite.Play("jump_" + currentDirection);
			}
			else if (CanDoubleJump && !hasDoubleJumped)
			{
				velocity.Y = -JumpStrength;
				hasDoubleJumped = true;
				CanDoubleJump = false;
				animatedSprite.Play("jump_" + currentDirection);
			}
		}
		Velocity = velocity;
	}
	
	
}
