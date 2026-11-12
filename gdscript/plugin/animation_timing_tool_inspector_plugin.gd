@tool
extends EditorInspectorPlugin

var timing_tool_script: Script = load("res://addons/gdf-godot/gdscript/plugin/animation_timing_tool.gd")

var _tool: Object

func _can_handle(object: Object) -> bool:
	if object.is_class(&"AnimationTrackKeyEdit") || object.is_class(&"AnimationMultiTrackKeyEdit"):
		return &"value" in object && object.value is Script && object.value == timing_tool_script
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
	_add_separator("AnimationMixer", EditorInterface.get_editor_theme().get_icon(&"AnimationMixer", &"EditorIcons"))
	_add_button("Retime Reimported Animations", EditorInterface.get_editor_theme().get_icon(&"Load", &"EditorIcons"), func(): _tool.set_context_from_editor().retime_reimported_animations())
	_add_padding(4)
	_add_separator("Animation", EditorInterface.get_editor_theme().get_icon(&"Animation", &"EditorIcons"))
	_add_button("Initialize", load("res://addons/gdf-godot/assets/icons/timing_tool_control_points.svg"), func(): _tool.set_context_from_editor().prompt_initialize())
	_add_button("Apply Timing Change", load("res://addons/gdf-godot/assets/icons/timing_tool_update_timing.svg"), func(): _tool.set_context_from_editor().apply_timing_change())
	_add_button("Uninitialize", EditorInterface.get_editor_theme().get_icon(&"Remove", &"EditorIcons"), func(): _tool.set_context_from_editor().uninitialize())

func _add_button(text: String, icon: Texture2D, callback: Callable) -> void:
	var btn := Button.new()
	btn.text = text
	btn.icon = icon
	btn.pressed.connect(callback, CONNECT_DEFERRED)
	add_custom_control(btn)

func _add_separator(text: String, icon: Texture2D) -> void:
	var hbox := HBoxContainer.new()
	
	var sep_l := HSeparator.new()
	var label_hbox := HBoxContainer.new()
	var icon_rect := TextureRect.new()
	var label := Label.new()
	var sep_r := HSeparator.new()

	var sep_stylebox := EditorInterface.get_editor_theme().get_stylebox(&"separator", &"PopupMenu")
	sep_l.add_theme_stylebox_override("separator", sep_stylebox)
	sep_r.add_theme_stylebox_override("separator", sep_stylebox)
	sep_l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	sep_r.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	icon_rect.texture = icon
	icon_rect.expand_mode = TextureRect.EXPAND_KEEP_SIZE
	icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	label_hbox.add_theme_constant_override(&"separation",0)
	label.text = text
	hbox.add_child(sep_l)
	label_hbox.add_child(icon_rect)
	label_hbox.add_child(label)
	hbox.add_child(label_hbox)
	hbox.add_child(sep_r)
	add_custom_control(hbox)

func _add_padding(size: int) -> void:
	var padding := Control.new()
	padding.mouse_filter = Control.MOUSE_FILTER_IGNORE
	padding.custom_minimum_size = Vector2(size, size)
	add_custom_control(padding)

func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
	match name:
		&"value":
			return true
		&"edit_control_points":
			return true
	return false

