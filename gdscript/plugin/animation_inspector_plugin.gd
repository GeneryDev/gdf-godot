@tool
extends EditorInspectorPlugin

const META_NAME_METADATA: StringName = &"gdf_metadata"

func _can_handle(object: Object) -> bool:
	if GdfAnimationNodeMetadata.Supports(object):
		return true
	if GdfAnimationTransitionMetadata.Supports(object):
		return true
	return false

func _create_button_for_method(label: String, callable: Callable, wide: bool = false) -> Control:
	var button := Button.new()
	button.text = label
	button.pressed.connect(callable)
	return button

func _unfold(button: Button) -> void:
	if button && !button.button_pressed:
		button.pressed.emit()

func _parse_begin(object: Object) -> void:
	if GdfAnimationNodeMetadata.Supports(object):
		if !object.has_meta(META_NAME_METADATA):
			add_custom_control(_create_button_for_method("Add GDF Metadata", func() -> void: _add_metadata(object, GdfAnimationNodeMetadata.new), true))
		else:
			_add_metadata_inspector(object, &"GdfAnimationNodeMetadata")

			add_custom_control(_create_button_for_method("Remove GDF Metadata", func() -> void: _remove_metadata(object), true))

	if GdfAnimationTransitionMetadata.Supports(object):
		if !object.has_meta(META_NAME_METADATA):
			add_custom_control(_create_button_for_method("Add GDF Metadata", func() -> void: _add_metadata(object, GdfAnimationTransitionMetadata.new), true))
		else:
			_add_metadata_inspector(object, &"GdfAnimationTransitionMetadata")

			add_custom_control(_create_button_for_method("Remove GDF Metadata", func() -> void: _remove_metadata(object), true))

func _add_metadata_inspector(object: Object, meta_class_name: StringName) -> void:
	var property_path: StringName = "metadata/" + META_NAME_METADATA
	var editor := EditorInspector.instantiate_property_editor(object, TYPE_OBJECT, property_path, PROPERTY_HINT_RESOURCE_TYPE, meta_class_name, PROPERTY_USAGE_DEFAULT, true)
	editor.use_folding = true
	editor.draw_background = false
	add_property_editor(property_path, editor, false, "GDF Metadata")
	var editor_resource_picker: Control
	var button_index: int
	if Engine.get_version_info().hex >= 0x040600:
		# Godot 4.6 or greater
		for child in editor.get_children():
			if child.is_class(&"EditorResourcePicker"):
				editor_resource_picker = child
				button_index = 1
				break
	else:
		# Before Godot 4.6
		editor_resource_picker = editor.get_child(0)
		button_index = 0
	
	if editor_resource_picker:
		editor_resource_picker.modulate = Color(1,1,1,0)
		var button := editor_resource_picker.get_child(button_index)
		if button is Button:
			_unfold.call_deferred(button)

func _add_metadata(object : Object, constructor: Callable) -> void:
	var undo_redo := EditorInterface.get_editor_undo_redo()
	undo_redo.create_action("Attach GDF Metadata to AnimationNode", UndoRedo.MERGE_ALL, object)
	var new_meta: Object = constructor.call() as Object
	if new_meta.is_class(&"GdfAnimationNodeMetadata"):
		new_meta.AnimationNodeClassName = object.get_class()
	undo_redo.add_do_method(object, &"set_meta", META_NAME_METADATA, new_meta)
	undo_redo.add_undo_method(object, &"remove_meta", META_NAME_METADATA)
	undo_redo.add_do_reference(new_meta)
	undo_redo.commit_action()

func _remove_metadata(object : Object) -> void:
	var undo_redo := EditorInterface.get_editor_undo_redo()
	undo_redo.create_action("Remove GDF Metadata from AnimationNode", UndoRedo.MERGE_ALL, object)
	var old_meta: Object = object.get_meta(META_NAME_METADATA) as Object
	undo_redo.add_do_method(object, &"remove_meta", META_NAME_METADATA)
	undo_redo.add_undo_method(object, &"set_meta", META_NAME_METADATA, old_meta)
	undo_redo.add_undo_reference(old_meta)
	undo_redo.commit_action()
	