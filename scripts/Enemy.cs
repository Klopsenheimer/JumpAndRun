using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 50f;
	[Export] public int Health { get; set; } = 30;
	[Export] public int Damage { get; set; } = 10;
	[Export] public float DetectionRange { get; set; } = 150f;
	[Export] public float AttackRange { get; set; } = 40f;
	[Export] public float AttackCooldown { get; set; } = 2f;
	[Export] public float PatrolDistance { get; set; } = 100f;
	
	private AnimatedSprite2D sprite;
	private CollisionShape2D collisionShape;
	private Area2D detectionArea;
	private Area2D attackArea;
	private Timer attackTimer;
	private HealthBar healthBar;
	
	private Player targetPlayer;
	private Vector2 startPosition;
	private Vector2 patrolTarget;
	private bool isPatrolling = true;
	private bool facingRight = true;
	private bool isAttacking = false;
	private bool isDead = false;
	private int maxHealth;
	
	private const float Gravity = 980f;
	private const float MaxFallSpeed = 800f;
	
	public enum EnemyState
	{
		Patrol,
		Chase,
		Attack,
		Dead
	}
	
	private EnemyState currentState = EnemyState.Patrol;
	
	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		detectionArea = GetNode<Area2D>("DetectionArea");
		attackArea = GetNode<Area2D>("AttackArea");
		attackTimer = GetNode<Timer>("AttackTimer");
		healthBar = GetNode<HealthBar>("HealthBar");
		
		startPosition = GlobalPosition;
		patrolTarget = startPosition + Vector2.Right * PatrolDistance;
		maxHealth = Health;
		
		// Setup health bar
		healthBar.MaxHealth = maxHealth;
		healthBar.CurrentHealth = Health;
		healthBar.HealthDepleted += OnHealthDepleted;
		
		// Setup attack timer
		attackTimer.WaitTime = AttackCooldown;
		attackTimer.Timeout += OnAttackCooldownFinished;
		
		// Setup detection area
		detectionArea.BodyEntered += OnDetectionAreaBodyEntered;
		detectionArea.BodyExited += OnDetectionAreaBodyExited;
		
		// Setup attack area
		attackArea.BodyEntered += OnAttackAreaBodyEntered;
		attackArea.BodyExited += OnAttackAreaBodyExited;
		
		// Setup collision shapes for areas
		SetupDetectionArea();
		SetupAttackArea();
		
		sprite.Play("idle");
	}
	
	private void SetupDetectionArea()
	{
		var detectionShape = new CircleShape2D();
		detectionShape.Radius = DetectionRange;
		var detectionCollision = detectionArea.GetNode<CollisionShape2D>("CollisionShape2D");
		detectionCollision.Shape = detectionShape;
	}
	
	private void SetupAttackArea()
	{
		var attackShape = new CircleShape2D();
		attackShape.Radius = AttackRange;
		var attackCollision = attackArea.GetNode<CollisionShape2D>("CollisionShape2D");
		attackCollision.Shape = attackShape;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (isDead) return;
		
		// Apply gravity
		if (!IsOnFloor())
		{
			Velocity = new Vector2(Velocity.X, Math.Min(Velocity.Y + Gravity * (float)delta, MaxFallSpeed));
		}
		
		switch (currentState)
		{
			case EnemyState.Patrol:
				HandlePatrol();
				break;
			case EnemyState.Chase:
				HandleChase();
				break;
			case EnemyState.Attack:
				HandleAttack();
				break;
		}
		
		MoveAndSlide();
		UpdateAnimation();
	}
	
	private void HandlePatrol()
	{
		float distanceToTarget = GlobalPosition.DistanceTo(patrolTarget);
		
		if (distanceToTarget < 10f)
		{
			// Switch patrol direction
			if (patrolTarget.X > startPosition.X)
			{
				patrolTarget = startPosition - Vector2.Right * PatrolDistance;
			}
			else
			{
				patrolTarget = startPosition + Vector2.Right * PatrolDistance;
			}
		}
		
		// Move towards patrol target
		Vector2 direction = (patrolTarget - GlobalPosition).Normalized();
		Velocity = new Vector2(direction.X * Speed, Velocity.Y);
		
		// Update facing direction
		if (direction.X > 0 && !facingRight)
		{
			Flip();
		}
		else if (direction.X < 0 && facingRight)
		{
			Flip();
		}
	}
	
	private void HandleChase()
	{
		if (targetPlayer == null) 
		{
			currentState = EnemyState.Patrol;
			return;
		}
		
		Vector2 direction = (targetPlayer.GlobalPosition - GlobalPosition).Normalized();
		Velocity = new Vector2(direction.X * Speed * 1.5f, Velocity.Y);
		
		// Update facing direction
		if (direction.X > 0 && !facingRight)
		{
			Flip();
		}
		else if (direction.X < 0 && facingRight)
		{
			Flip();
		}
	}
	
	private void HandleAttack()
	{
		Velocity = new Vector2(0, Velocity.Y);
		
		if (!isAttacking && attackTimer.IsStopped())
		{
			PerformAttack();
		}
	}
	
	private void PerformAttack()
	{
		isAttacking = true;
		sprite.Play("attack");
		
		// Deal damage to player if in range
		if (targetPlayer != null && GlobalPosition.DistanceTo(targetPlayer.GlobalPosition) <= AttackRange)
		{
			// Try to get players health bar and deal damage
			var playerHealthBar = targetPlayer.GetNodeOrNull<HealthBar>("HealthBar");
			if (playerHealthBar != null)
			{
				playerHealthBar.TakeDamage(Damage);
			}
		}
		
		attackTimer.Start();
	}
	
	private void OnAttackCooldownFinished()
	{
		isAttacking = false;
		
		// Check if player is still in attack range
		if (targetPlayer != null && GlobalPosition.DistanceTo(targetPlayer.GlobalPosition) <= AttackRange)
		{
			currentState = EnemyState.Attack;
		}
		else if (targetPlayer != null && GlobalPosition.DistanceTo(targetPlayer.GlobalPosition) <= DetectionRange)
		{
			currentState = EnemyState.Chase;
		}
		else
		{
			currentState = EnemyState.Patrol;
			targetPlayer = null;
		}
	}
	
	private void OnDetectionAreaBodyEntered(Node2D body)
	{
		if (body is Player player && !isDead)
		{
			targetPlayer = player;
			currentState = EnemyState.Chase;
		}
	}
	
	private void OnDetectionAreaBodyExited(Node2D body)
	{
		if (body is Player && currentState == EnemyState.Chase)
		{
			currentState = EnemyState.Patrol;
			targetPlayer = null;
		}
	}
	
	private void OnAttackAreaBodyEntered(Node2D body)
	{
		if (body is Player && !isDead)
		{
			currentState = EnemyState.Attack;
		}
	}
	
	private void OnAttackAreaBodyExited(Node2D body)
	{
		if (body is Player && currentState == EnemyState.Attack)
		{
			currentState = EnemyState.Chase;
		}
	}
	
	public void TakeDamage(int damage)
	{
		if (isDead) return;
		
		Health -= damage;
		healthBar.TakeDamage(damage);
		
		// Flash red when taking damage
		sprite.Modulate = Colors.Red;
		var tween = CreateTween();
		tween.TweenProperty(sprite, "modulate", Colors.White, 0.2f);
		
		if (Health <= 0)
		{
			Die();
		}
	}
	
	private void OnHealthDepleted()
	{
		Die();
	}
	
	private void Die()
	{
		isDead = true;
		currentState = EnemyState.Dead;
		Velocity = Vector2.Zero;
		
		sprite.Play("death");
		collisionShape.SetDeferred("disabled", true);
		detectionArea.SetDeferred("monitoring", false);
		attackArea.SetDeferred("monitoring", false);
		
		// Remove enemy after death animation
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 2f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
	
	private void Flip()
	{
		facingRight = !facingRight;
		sprite.Scale = new Vector2(-sprite.Scale.X, sprite.Scale.Y);
	}
	
	private void UpdateAnimation()
	{
		if (isDead) return;
		
		if (isAttacking)
		{
			// Attack animation is already playing
			return;
		}
		
		if (Math.Abs(Velocity.X) > 0.1f)
		{
			sprite.Play("walk");
		}
		else
		{
			sprite.Play("idle");
		}
	}
	
	public void SetPatrolDistance(float distance)
	{
		PatrolDistance = distance;
		patrolTarget = startPosition + Vector2.Right * PatrolDistance;
	}
}
