@tool
extends EditorInspectorPlugin

func _can_handle(object: Object) -> bool:
	if object.is_class(&"AnimationTrackKeyEdit"):
		return true
	return false

func _parse_begin(object: Object) -> void:
	print("obj: " + str(object))
	# print(object.get_property_list())
	# print(object.get_method_list())
	# print(object.get_meta_list())
	print(object.get_root_path())

# func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
# 	if name.begins_with("shader_parameter/"):
# 		_started_shader_parameters = true
# 		return true
# 	return false

