@tool
extends EditorInspectorPlugin

var timing_tool_script: Script = load("res://addons/gdf-godot/gdscript/plugin/animation_timing_tool.gd")

var _tool: Object

func _can_handle(object: Object) -> bool:
	if object.is_class(&"AnimationTrackKeyEdit") || object.is_class(&"AnimationMultiTrackKeyEdit"):
		return &"value" in object && object.value == timing_tool_script
	if is_instance_of(object, timing_tool_script):
		return true
	return false

func _parse_begin(object: Object) -> void:
	if !_tool:
		_tool = timing_tool_script.new()

func _parse_category(object: Object, category: String) -> void:
	if is_instance_of(object, timing_tool_script) && category == "animation_timing_tool.gd":
		var label := Label.new()
		label.text = "You shouldn't be here."
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		add_custom_control(label)

func _parse_end(object: Object) -> void:
	_add_buttons()
	
	# print(object.get_property_list())
	# print(object.get_method_list())
	# print(object.get_meta_list())

func _add_buttons() -> void:
	_add_button("Revert Timing Changes", EditorInterface.get_editor_theme().get_icon(&"Reload", &"EditorIcons"), func(): _tool.revert_timing())
	_add_button("Initialize Timing Tracks", load("res://addons/gdf-godot/assets/icons/timing_tool_control_points.svg"), func(): _tool.initialize_tracks())
	_add_button("Original Control Points Done", load("res://addons/gdf-godot/assets/icons/timing_tool_control_points_done.svg"), func(): _tool.original_control_points_done())
	_add_button("Update Timings", load("res://addons/gdf-godot/assets/icons/timing_tool_update_timing.svg"), func(): _tool.update_timing())

func _add_button(text: String, icon: Texture2D, callback: Callable) -> void:
	var btn := Button.new()
	btn.text = text
	btn.icon = icon
	btn.pressed.connect(callback, CONNECT_DEFERRED)
	add_custom_control(btn)

func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
	match name:
		&"value":
			return true
		&"original_control_points":
			return true
		&"desired_control_points":
			return true
		&"current_control_points":
			return true
	return false
	# if name.begins_with("shader_parameter/"):
	# 	_started_shader_parameters = true
	# 	return true
	# return false

