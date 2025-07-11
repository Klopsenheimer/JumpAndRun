using Godot;
using JumpAndRun.scripts;
using System;

public partial class Player : CharacterBody2D
{
	[Signal] public delegate void PlayerDiedEventHandler();
	public int MaxHealth { get; set; } = 100;
	public int CurrentHealth { get; set; } = 100;
	private HealthBar healthBar;
	public string PlayerName { get; set; } = "player1";
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
	private bool isOnIcyPlatform = false;
	private float iceAcceleration = 0.5f;
	private float iceDeceleration = 0.2f;
	private float iceMaxSpeedMultiplier = 1.5f;
	
	// More slippery (harder to control):
	// private float iceAcceleration = 0.3f;
	//private float iceDeceleration = 0.1f;

	// Less slippery (easier to control):
	// private float iceAcceleration = 0.7f;
	// private float iceDeceleration = 0.4f;
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
		healthBar = GetNodeOrNull<HealthBar>("HealthBar");
		if (healthBar != null)
		{
			healthBar.MaxHealth = MaxHealth;
			healthBar.CurrentHealth = CurrentHealth;
			healthBar.HealthDepleted += OnHealthDepleted;
		}
	}
	
	public void ActivateJetpack(float duration)
	{
		jetpackActive = true;
		jetpackTimeLeft = duration;
		GD.Print($"Jetpack activated for {duration} seconds");
	}
	public void TakeDamage(int damage)
	{
		if (CurrentHealth <= 0) return;
		CurrentHealth -= damage;
		healthBar?.TakeDamage(damage);
		var tween = CreateTween();
		tween.TweenProperty(animatedSprite, "modulate", Colors.Red, 0.1f);
		tween.TweenProperty(animatedSprite, "modulate", Colors.White, 0.1f);
	}

	public void Heal(int healAmount)
	{
		CurrentHealth = Math.Min(CurrentHealth + healAmount, MaxHealth);
		healthBar?.Heal(healAmount);
	}

	private void OnHealthDepleted() => EmitSignal(SignalName.PlayerDied);


	public bool IsAlive() { return CurrentHealth > 0; }
	
	public override void _PhysicsProcess(double delta)
	{
		HandleInput((float)delta);

		bool wasGrounded = IsGrounded;
		IsGrounded = IsOnFloor();
		/* 
		if (isOnIce)
		{
			iceTimer -= (float)delta;
			if (iceTimer <= 0)
			{
				isOnIce = false;
			}
		}
		*/
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
	/* 
	private void CheckPlatformCollisions()
	{
		bool wasOnIcyPlatform = isOnIcyPlatform;
		isOnIcyPlatform = false; // Reset flag each frame
		
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
						isOnIcyPlatform = true;
						break;
					case RoughPlatform roughPlatform:
						roughPlatform.OnPlayerLanded(this);
						break;
					case SmallRoughPlatform smallRoughPlatform:
						smallRoughPlatform.OnPlayerLanded(this);
						break;
					case SmallGoldPlatform smallGoldPLatform:
						smallGoldPLatform.OnPlayerLanded(this);
						break;
					case SmallIcyPlatforms smallIcyPlatforms:
						smallIcyPlatforms.OnPlayerLanded(this);
						isOnIcyPlatform = true;
						break;
				}
			}
		}
		else if (IsOnFloor())
		{
			for(int i = 0; i < GetSlideCollisionCount(); i++)
			{
				var collision = GetSlideCollision(i);
				var collider = collision.GetCollider();
				if(collider is IcyPlatform || collider is SmallIcyPlatforms)
				{
					isOnIcyPlatform = true;
					break;
				}
			}
		}
		if(wasOnIcyPlatform && !isOnIcyPlatform)
		{
			isOnIce = false;
		}
	}
	*/
	private void CheckPlatformCollisions()
	{
		bool wasOnIcyPlatform = isOnIcyPlatform;
		isOnIcyPlatform = false; // Reset flag each frame
	
		// Check if we just landed (transition from not on floor to on floor)
		if (IsOnFloor())
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
						isOnIcyPlatform = true;
						if (!wasOnIcyPlatform)
						{
							isOnIce = true;
							GD.Print("Player landed on icy platform");
						
							// Visual feedback
							var tween = CreateTween();
							tween.TweenProperty(animatedSprite, "modulate", new Color(0.7f, 0.9f, 1f, 1f), 0.2f);
							tween.TweenProperty(animatedSprite, "modulate", Colors.White, 0.2f).SetDelay(1.6f);
						}
						break;
					case RoughPlatform roughPlatform:
						roughPlatform.OnPlayerLanded(this);
						break;
					case SmallRoughPlatform smallRoughPlatform:
						smallRoughPlatform.OnPlayerLanded(this);
						break;
					case SmallGoldPlatform smallGoldPLatform:
						smallGoldPLatform.OnPlayerLanded(this);
						break;
					case SmallIcyPlatforms smallIcyPlatforms:
						smallIcyPlatforms.OnPlayerLanded(this);
						isOnIcyPlatform = true;
						if (!wasOnIcyPlatform)
						{
							isOnIce = true;
							GD.Print("Player landed on small icy platform");
						
							// Visual feedback
							var tween = CreateTween();
							tween.TweenProperty(animatedSprite, "modulate", new Color(0.7f, 0.9f, 1f, 1f), 0.2f);
							tween.TweenProperty(animatedSprite, "modulate", Colors.White, 0.2f).SetDelay(1.6f);
						}
						break;
				}
			}
		}

		// Check if we're still on an icy platform (for cases where we didn't just land)
		if (!isOnIcyPlatform && IsOnFloor())
		{
			for (int i = 0; i < GetSlideCollisionCount(); i++)
			{
				var collision = GetSlideCollision(i);
				var collider = collision.GetCollider();

				if (collider is IcyPlatform || collider is SmallIcyPlatforms)
				{
					isOnIcyPlatform = true;
					if (!wasOnIcyPlatform)
					{
						isOnIce = true;
					}
					break;
				}
			}
		}

		// Remove ice effect when leaving icy platforms
		if (wasOnIcyPlatform && !isOnIcyPlatform)
		{
			isOnIce = false;
			GD.Print("Player left icy platform");
		}
	}
	
	public void ApplyIceEffect()
	{
		if(isOnIcyPlatform == true)
		{
			isOnIce = true;
			GD.Print("Ice effect applied to player");
		
			// Visual feedback
			var tween = CreateTween();
			tween.TweenProperty(animatedSprite, "modulate", new Color(0.7f, 0.9f, 1f, 1f), 0.2f);
			tween.TweenProperty(animatedSprite, "modulate", Colors.White, 0.2f).SetDelay(1.6f);
		}
		
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
		wasOnFloorLastFrame = false;

		CurrentHealth = MaxHealth;
		healthBar?.SetHealth(CurrentHealth);

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
		float targetVelocityX = 0f;

		// Handle left/right movement input
		if (Input.IsActionPressed("move_left"))
		{
			targetVelocityX = -XVelocity;
			currentDirection = "left";
		
			if (IsGrounded)
			{
				animatedSprite.Play("walk_left");
			}
		}
		else if (Input.IsActionPressed("move_right"))
		{
			targetVelocityX = XVelocity;
			currentDirection = "right";
		
			if (IsGrounded)
			{
				animatedSprite.Play("walk_right");
			}
		}

		// Apply movement based on whether we're on ice or not
		if (isOnIce && isOnIcyPlatform && IsGrounded)
		{
			// Ice physics - smooth acceleration/deceleration
			float acceleration = (Mathf.Sign(targetVelocityX) == Mathf.Sign(velocity.X)) 
				? iceAcceleration 
				: iceDeceleration;
		
			velocity.X = Mathf.Lerp(
				velocity.X, 
				targetVelocityX * iceMaxSpeedMultiplier, 
				acceleration * delta * 10f
			);
		
			// Gradual stop when no input
			if (targetVelocityX == 0f)
			{
				velocity.X = Mathf.Lerp(velocity.X, 0, iceDeceleration * delta * 5f);
			}
		}
		else
		{
			// Normal ground movement - immediate response
			velocity.X = targetVelocityX;
		
			// Immediate stop when no input
			if (targetVelocityX == 0f)
			{
				velocity.X = 0;
			}
		}

		// Handle jumping
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
