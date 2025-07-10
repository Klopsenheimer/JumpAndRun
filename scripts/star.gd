extends Node2D

var lifetime: float = randf_range(1.5, 3.0)
var star_size: float = randf_range(1.0, 3.0)  # Renamed from 'size'
var twinkle_speed: float = randf_range(2.0, 4.0)
var alpha: float = 0.0
var growing: bool = true

func _process(delta):
	lifetime -= delta
	if lifetime <= 0:
		queue_free()
		return
	
	# Twinkle effect
	alpha += (twinkle_speed * delta) * (1.0 if growing else -1.0)
	
	if alpha >= 1.0:
		alpha = 1.0
		growing = false
	elif alpha <= 0.3:
		alpha = 0.3
		growing = true
	
	queue_redraw()

func _draw():
	# Main star
	draw_circle(Vector2.ZERO, star_size, Color(1, 1, 1, alpha))
	
	# Glow effect
	var glow_size = star_size * 1.5
	for i in 3:
		draw_circle(Vector2.ZERO, glow_size, Color(1, 1, 1, alpha * 0.1 * (3 - i)))
	
	# Star points
	var points = PackedVector2Array()
	for i in 4:
		var angle = i * PI / 2
		points.push_back(Vector2.ZERO)
		points.push_back(Vector2(cos(angle), sin(angle)) * star_size * 2)
	draw_multiline(points, Color(1, 1, 1, alpha * 0.5), 1.0)
