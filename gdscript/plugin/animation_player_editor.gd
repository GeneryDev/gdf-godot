@tool
extends RefCounted

static var _animation_editor: Control
static var _animation_picker: OptionButton
static var _time_control: SpinBox

static var _editing_animation_mixer: AnimationMixer
static var _editing_mixer_unselected_but_still_in_tree := false

static var current_time: float:
	get:
		_prepare_to_access_editor()
		if !_time_control: return 0
		return _time_control.value
	set(value):
		_prepare_to_access_editor()
		if !_time_control: return
		_time_control.value = value

static var current_animation_name: StringName:
	get:
		_prepare_to_access_editor()
		if !_animation_picker: return &""
		if _animation_picker.selected == -1: return &""
		return _animation_picker.get_item_text(_animation_picker.selected)
	set(value):
		_prepare_to_access_editor()
		if !_animation_picker: return
		for i in range(_animation_picker.item_count):
			if _animation_picker.get_item_text(i) == value:
				_animation_picker.selected = i
				return

static var editing_animation_mixer: AnimationMixer:
	get:
		_sanitize_editing_mixer()
		return _editing_animation_mixer

static var is_open: bool:
	get:
		_prepare_to_access_editor()
		return _animation_editor && _animation_editor.visible

static func _prepare_to_access_editor():
	if _animation_editor && _animation_picker && _time_control: return
	
	_animation_editor = EditorInterface.get_base_control().find_children("Animation","AnimationPlayerEditor", true, false)[0]
	_animation_picker = _animation_editor.find_children("*","OptionButton", true, false)[0]
	_time_control = _animation_editor.find_children("*","SpinBox", true, false)[0]

static func _update_editing_mixer(mixer: AnimationMixer) -> void:
	if mixer:
		_editing_animation_mixer = mixer
		print("Editing animation mixer: %s" % [_editing_animation_mixer])
		_editing_mixer_unselected_but_still_in_tree = false
	elif _editing_animation_mixer && _editing_animation_mixer.is_inside_tree():
		_editing_mixer_unselected_but_still_in_tree = true
		print("Deselected mixer, but still in tree")
	else:
		_editing_animation_mixer = null
		print("No animation mixer active")
		_editing_mixer_unselected_but_still_in_tree = false

static func _sanitize_editing_mixer() -> void:
	if !_editing_mixer_unselected_but_still_in_tree: return
	if is_instance_valid(_editing_animation_mixer) && _editing_animation_mixer.is_inside_tree(): return
	
	_editing_animation_mixer = null
	_editing_mixer_unselected_but_still_in_tree = false
	print("Deselected mixer removed from tree")
