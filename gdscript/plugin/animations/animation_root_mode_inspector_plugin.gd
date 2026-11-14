@tool
extends EditorInspectorPlugin

const META_NAME_CACHED_VALUE := &"_cached_value"

func _can_handle(object: Object) -> bool:
	if object is AnimationMixer:
		return true
	return false

func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
	match name:
		&"root_motion_track":
			var property_editor := EditorInspector.instantiate_property_editor(object, type, "", hint_type, hint_string, usage_flags)
		
			property_editor.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			property_editor.draw_label = true
			property_editor.set_object_and_property(object, name)
			property_editor.selectable = false
			property_editor.update_property()
			property_editor.set_meta(META_NAME_CACHED_VALUE, object.get(name))
			add_property_editor(name, property_editor, false, "Track (unrestricted)")
			property_editor.property_changed.connect(_on_property_changed.bind(object))
			return false
	return false

func _on_property_changed(property: StringName, value: Variant, field: StringName, changing: bool, obj: Object) -> void:
	var undo_redo := EditorInterface.get_editor_undo_redo()
	undo_redo.create_action("Set " + property, UndoRedo.MERGE_ALL, obj, true)
	undo_redo.add_do_property(obj, property, value)
	undo_redo.add_undo_property(obj, property, obj.get(property))
	undo_redo.commit_action(true)