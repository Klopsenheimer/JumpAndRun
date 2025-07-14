using Godot;
using JumpAndRun.scripts;
using System;
using System.Collections.Generic;


public partial class Player : CharacterBody2D
{
	[Signal] public delegate void PlayerDiedEventHandler();
	[Export] public int MaxHealth { get; set; } = 100;
	[Export] public int CurrentHealth { get; set; } = 100;
	private HealthBar healthBar;
	[Export] public string PlayerName { get; set; } = "player1";
	[Export] public float Score { get; set; } = 0;
	[Export] public float XVelocity { get; set; } = 200;
	[Export] public float JumpStrength { get; set; } = 400;
	[Export] public bool IsGrounded { get; set; } = false;
	[Export] public bool CanDoubleJump { get; set; } = true;
	[Export] public int Width { get; set; } = 40;
	[Export] public int Height { get; set; } = 40;
	[Export] public int AttackDamage { get; set; } = 10;
	[Export] public float AttackRange { get; set; } = 40f;


	private AnimatedSprite2D animatedSprite;
	private string currentDirection = "right";
	private bool wasGrounded = false;

	private const float Gravity = 980;
	private const float MaxFallSpeed = 800;
	private const float GroundLevel = 500;
	[Export] private bool hasDoubleJumped = false;
	[Export] private float highestY = 400;
	[Export] private float coyoteTime = 0f;
	private const float CoyoteTimeLimit = 0.1f;
	[Export] private float scoreMultiplier = 1.0f;
	[Export] private Label nameLabel;
	[Export] private bool isOnIce = false;
	[Export] private bool isOnIcyPlatform = false;
	[Export] private float iceAcceleration = 0.5f;
	[Export] private float iceDeceleration = 0.2f;
	[Export] private float iceMaxSpeedMultiplier = 1.5f;

	private bool wasOnFloorLastFrame = false;

	private bool jetpackActive = false;
	private float jetpackTimeLeft = 0f;
	[Export] private float jetpackLiftForce = 500f;

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
	
	public void Attack()
	{
	var spaceState = GetWorld2D().DirectSpaceState;
	var attackDirection = currentDirection == "right" ? Vector2.Right : Vector2.Left;
	var attackPosition = GlobalPosition + attackDirection * AttackRange;
	var shape = new CircleShape2D();
	shape.Radius = AttackRange / 2;
	var shapeParams = new PhysicsShapeQueryParameters2D();
	shapeParams.Shape = shape;
	shapeParams.Transform = new Transform2D(0, attackPosition);
	shapeParams.CollisionMask = 1 << 0; // Adjust to your enemy collision layer
	shapeParams.CollideWithAreas = true;
	shapeParams.CollideWithBodies = true;
	var results = spaceState.IntersectShape(shapeParams, 32);
	
	foreach (var dict in results)
	{
		var colliderObj = dict["collider"];
		if (colliderObj.AsGodotObject() is Node collider)
		{
			if (collider is Enemy enemy)
			{
				enemy.TakeDamage(AttackDamage);
				GD.Print($"Attacked enemy: {enemy.Name} for {AttackDamage} damage.");
			}
		}
	}
	//animatedSprite.Play("attack_" + currentDirection);
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
			else jetpackActive = false;
		}
		else
		{
			if (!IsGrounded)
			{
				Velocity = new Vector2(Velocity.X, Math.Min(Velocity.Y + Gravity * (float)delta, MaxFallSpeed));
				coyoteTime = wasGrounded ? CoyoteTimeLimit : Math.Max(0, coyoteTime - (float)delta);
				if (Velocity.Y < 0) animatedSprite.Play("jump_" + currentDirection);
				else animatedSprite.Play("fall_" + currentDirection);
			}
			else
			{
				coyoteTime = CoyoteTimeLimit;
				hasDoubleJumped = false;
				CanDoubleJump = true;
				if (Mathf.Abs(Velocity.X) < 0.1f) animatedSprite.Play("idle_" + currentDirection);
			}
		}
		MoveAndSlide();
		CheckPlatformCollisions();
		if (GlobalPosition.Y < highestY)
		{
			highestY = GlobalPosition.Y;
			Score = Math.Max(0, (GroundLevel - highestY) / 10) * scoreMultiplier;
		}
		if (Input.IsActionJustPressed("attack")) Attack();
		
		ClampToScreenBounds();
		wasOnFloorLastFrame = IsOnFloor();
	}

	private void CheckPlatformCollisions()
	{
		bool wasOnIcyPlatform = isOnIcyPlatform;
		isOnIcyPlatform = false;

		if (IsOnFloor())
		{
			for (int i = 0; i < GetSlideCollisionCount(); i++)
			{
				var collision = GetSlideCollision(i);
				var collider = collision.GetCollider();
				switch (collider)
				{
					case GoldPlatform goldPlatform: goldPlatform.OnPlayerLanded(this); break;
					case IcyPlatform icyPlatform:
						{
							icyPlatform.OnPlayerLanded(this);
							isOnIcyPlatform = true;
							if (!wasOnIcyPlatform)
							{
								isOnIce = true;
								GD.Print("Player landed on icy platform");
								var tween = CreateTween();
								tween.TweenProperty(animatedSprite, "modulate", new Color(0.7f, 0.9f, 1f, 1f), 0.2f);
								tween.TweenProperty(animatedSprite, "modulate", Colors.White, 0.2f).SetDelay(1.6f);
							}
							break;
						}
					case RoughPlatform roughPlatform: roughPlatform.OnPlayerLanded(this); break;
					case SmallRoughPlatform smallRoughPlatform: smallRoughPlatform.OnPlayerLanded(this); break;
					case SmallGoldPlatform smallGoldPLatform: smallGoldPLatform.OnPlayerLanded(this); break;
					case SmallIcyPlatforms smallIcyPlatforms:
						{
							smallIcyPlatforms.OnPlayerLanded(this);
							isOnIcyPlatform = true;
							if (!wasOnIcyPlatform)
							{
								isOnIce = true;
								GD.Print("Player landed on small icy platform");

								var tween = CreateTween();
								tween.TweenProperty(animatedSprite, "modulate", new Color(0.7f, 0.9f, 1f, 1f), 0.2f);
								tween.TweenProperty(animatedSprite, "modulate", Colors.White, 0.2f).SetDelay(1.6f);
							}
							break;
						}
				}
			}
		}

		if (!isOnIcyPlatform && IsOnFloor())
		{
			for (int i = 0; i < GetSlideCollisionCount(); i++)
			{
				var collision = GetSlideCollision(i);
				var collider = collision.GetCollider();

				if (collider is IcyPlatform || collider is SmallIcyPlatforms)
				{
					isOnIcyPlatform = true;
					if (!wasOnIcyPlatform) isOnIce = true;
					break;
				}
			}
		}

		if (wasOnIcyPlatform && !isOnIcyPlatform)
		{
			isOnIce = false;
			GD.Print("Player left icy platform");
		}
	}

	public void ApplyIceEffect()
	{
		if (isOnIcyPlatform == true)
		{
			isOnIce = true;
			GD.Print("Ice effect applied to player");

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
		if (Input.IsActionPressed("move_left"))
		{
			targetVelocityX = -XVelocity;
			currentDirection = "left";
			if (IsGrounded) animatedSprite.Play("walk_left");
		}
		else if (Input.IsActionPressed("move_right"))
		{
			targetVelocityX = XVelocity;
			currentDirection = "right";
			if (IsGrounded) animatedSprite.Play("walk_right");
		}

		if (isOnIce && isOnIcyPlatform && IsGrounded)
		{
			float acceleration = (Mathf.Sign(targetVelocityX) == Mathf.Sign(velocity.X)) ? iceAcceleration : iceDeceleration;
			velocity.X = Mathf.Lerp(velocity.X, targetVelocityX * iceMaxSpeedMultiplier, acceleration * delta * 10f);
			if (targetVelocityX == 0f) velocity.X = Mathf.Lerp(velocity.X, 0, iceDeceleration * delta * 5f);
		}
		else
		{
			velocity.X = targetVelocityX;
			if (targetVelocityX == 0f) velocity.X = 0;
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

	public bool CheckKillEnemy(Enemy enemy)
	{
		return GlobalPosition.Y < enemy.GlobalPosition.Y && Velocity.Y > 0;
	}

	public void BounceAfterKill()
	{
		Velocity = new Vector2(Velocity.X, -JumpStrength);
	}
}
