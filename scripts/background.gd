extends ColorRect

var color_speed: float = 0.0005
var star_scene: PackedScene = preload("res://scenes/Star.tscn")
var spawn_timer: float = 0.0

func _process(delta):
	var y_position = get_global_mouse_position().y / 2
	var hue = fmod(y_position * color_speed, 1.0)
	var new_color = Color.from_ok_hsl(hue, 1.0, 0.5)
	self.color = new_color

	spawn_timer -= delta
	if spawn_timer <= 0.0:
		_spawn_star()
		spawn_timer = randf_range(0.5, 2.5)  

func _spawn_star():
	var star = star_scene.instantiate()
	add_child(star)
	
	star.position = Vector2(
		randf() * size.x,
		randf() * size.y
	)
