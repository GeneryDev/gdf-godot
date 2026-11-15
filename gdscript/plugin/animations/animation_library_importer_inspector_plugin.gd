@tool
extends EditorInspectorPlugin

func _can_handle(object: Object) -> bool:
	if object is AnimationLibraryImporter:
		return true
	return false

func _parse_property(object: Object, type: int, name: String, hint_type: int, hint_string: String, usage_flags: int, wide: bool) -> bool:
	match name:
		&"_inspector_view":
			_insert_view(object)
			return true
	return false

func _insert_view(library: AnimationLibraryImporter) -> void:
	var view = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_library_importer_view_panel.gd").new(library)
	add_custom_control(view)
