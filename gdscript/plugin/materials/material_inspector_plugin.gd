@tool
extends EditorInspectorPlugin

var _started_shader_parameters := false

func _can_handle(object: Object) -> bool:
	if object is GdfCustomMaterial:
		return true
	return false

func _parse_begin(object: Object) -> void:
	_started_shader_parameters = false

func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
	if name.begins_with("shader_parameter/"):
		_started_shader_parameters = true
		return true
	return false
