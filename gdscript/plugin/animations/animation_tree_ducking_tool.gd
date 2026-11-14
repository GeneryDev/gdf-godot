@tool
extends Resource

var currently_ducking: bool
var ducking_tree: AnimationTree
var ducking_by_player: AnimationPlayer
var tree_active_before_ducking: bool

func _init():
	resource_name = "Ducking in Editor"

func _validate_property(property: Dictionary) -> void:
	match property.name:
		&"currently_ducking":
			property.usage &= ~PROPERTY_USAGE_STORAGE
		&"ducking_tree":
			property.usage &= ~PROPERTY_USAGE_STORAGE
		&"ducking_by_player":
			property.usage &= ~PROPERTY_USAGE_STORAGE
		&"tree_active_before_ducking":
			property.usage &= ~PROPERTY_USAGE_STORAGE

func start_ducking(tree: AnimationTree, player: AnimationPlayer) -> void:
	if currently_ducking:
		printerr("Already ducking")
		return
	
	currently_ducking = true
	ducking_tree = tree
	ducking_by_player = player
	tree_active_before_ducking = tree.active
	
	_add_sensor(ducking_tree)
	_resume_ducking()

func stop_ducking() -> void:
	if !currently_ducking: return
	_pause_ducking()
	if ducking_tree && is_instance_valid(ducking_tree):
		_remove_sensor(ducking_tree)
	ducking_tree = null
	ducking_by_player = null
	currently_ducking = false

func _add_sensor(tree: Node) -> void:
	var save_sensor_exists := tree.get_children().any(func(n): n is DuckingSaveSensorNode)
	if !save_sensor_exists:
		var sensor := DuckingSaveSensorNode.new()
		sensor.pre_save.connect(_on_pre_save)
		sensor.post_save.connect(_on_post_save)
		tree.add_child(sensor)

func _resume_ducking() -> void:
	if !currently_ducking: return
	if ducking_tree && is_instance_valid(ducking_tree):
		ducking_tree.active = false
		
func _pause_ducking() -> void:
	if !currently_ducking: return
	if ducking_tree && is_instance_valid(ducking_tree):
		ducking_tree.active = tree_active_before_ducking

func _remove_sensor(tree: Node) -> void:
	for child in tree.get_children():
		if child is DuckingSaveSensorNode:
			tree.remove_child(child)
			child.queue_free()

func _on_pre_save() -> void:
	_pause_ducking()

func _on_post_save() -> void:
	_resume_ducking()

class DuckingSaveSensorNode extends Node:
	signal pre_save()
	signal post_save()
	
	func _init():
		name = "_ducking_save_sensor"
	
	func _notification(what: int) -> void:
		match what:
			Node.NOTIFICATION_EDITOR_PRE_SAVE:
				pre_save.emit()
			Node.NOTIFICATION_EDITOR_POST_SAVE:
				post_save.emit()