using Godot;
using System;

public partial class HealthBar : Control
{
	private ProgressBar healthBar;
	private Label healthLabel;
	private int maxHealth = 100;
	private int currentHealth = 100;
	
	public int MaxHealth 
	{ 
		get => maxHealth; 
		set 
		{ 
			maxHealth = value; 
			UpdateHealthBar(); 
		} 
	}
	
	public int CurrentHealth 
	{ 
		get => currentHealth; 
		set 
		{ 
			currentHealth = Math.Max(0, Math.Min(value, maxHealth)); 
			UpdateHealthBar(); 
		} 
	}
	
	public override void _Ready()
	{
		healthBar = GetNode<ProgressBar>("HealthProgressBar");
		healthLabel = GetNode<Label>("HealthLabel");
		
		// Configure the progress bar
		healthBar.MinValue = 0;
		healthBar.MaxValue = maxHealth;
		healthBar.Value = currentHealth;
		
		// Style the health bar
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = Colors.Red;
		styleBox.CornerRadiusTopLeft = 5;
		styleBox.CornerRadiusTopRight = 5;
		styleBox.CornerRadiusBottomLeft = 5;
		styleBox.CornerRadiusBottomRight = 5;
		
		var backgroundStyle = new StyleBoxFlat();
		backgroundStyle.BgColor = Colors.DarkRed;
		backgroundStyle.CornerRadiusTopLeft = 5;
		backgroundStyle.CornerRadiusTopRight = 5;
		backgroundStyle.CornerRadiusBottomLeft = 5;
		backgroundStyle.CornerRadiusBottomRight = 5;
		
		healthBar.AddThemeStyleboxOverride("fill", styleBox);
		healthBar.AddThemeStyleboxOverride("background", backgroundStyle);
		
		UpdateHealthBar();
	}
	
	private void UpdateHealthBar()
	{
		if (healthBar != null)
		{
			healthBar.MaxValue = maxHealth;
			healthBar.Value = currentHealth;
			
			// Change color based on health percentage
			var healthPercentage = (float)currentHealth / maxHealth;
			var styleBox = new StyleBoxFlat();
			styleBox.CornerRadiusTopLeft = 5;
			styleBox.CornerRadiusTopRight = 5;
			styleBox.CornerRadiusBottomLeft = 5;
			styleBox.CornerRadiusBottomRight = 5;
			
			if (healthPercentage > 0.6f)
				styleBox.BgColor = Colors.Green;
			else if (healthPercentage > 0.3f)
				styleBox.BgColor = Colors.Yellow;
			else
				styleBox.BgColor = Colors.Red;
				
			healthBar.AddThemeStyleboxOverride("fill", styleBox);
		}
		
		if (healthLabel != null)
		{
			healthLabel.Text = $"{currentHealth}/{maxHealth}";
		}
	}
	
	public void TakeDamage(int damage)
	{
		CurrentHealth -= damage;
		
		// Create a damage animation
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate", Colors.Red, 0.1f);
		tween.TweenProperty(this, "modulate", Colors.White, 0.1f);
		
		if (currentHealth <= 0)
		{
			EmitSignal(SignalName.HealthDepleted);
		}
	}
	
	public void Heal(int healAmount)
	{
		CurrentHealth += healAmount;
		
		// Create a heal animation
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate", Colors.Green, 0.1f);
		tween.TweenProperty(this, "modulate", Colors.White, 0.1f);
	}
	
	public void SetHealth(int health)
	{
		CurrentHealth = health;
	}
	
	public void SetMaxHealth(int maxHealth)
	{
		MaxHealth = maxHealth;
	}
	
	public bool IsAlive()
	{
		return currentHealth > 0;
	}
	
	[Signal]
	public delegate void HealthDepletedEventHandler();
}