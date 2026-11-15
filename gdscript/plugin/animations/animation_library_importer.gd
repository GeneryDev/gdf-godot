@tool
class_name AnimationLibraryImporter
extends AnimationLibrary

const AnimationTransactionAPI : Script = preload(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_transaction_api.gd")
const Transactions := AnimationTransactionAPI.Transactions
const KeyState := AnimationTransactionAPI.KeyState
const TrackState := AnimationTransactionAPI.TrackState

const META_NAME_INHERITING_FROM_ANIMATION := &"inheriting_from_animation"

@export_file("*.gltf","*.glb","*.fbx","*.blend")
var source_scene: String:
	get: return source_scene
	set(value):
		if source_scene == value: return
		source_scene = value
		_clear_source_cache()
		
@export_group("Import Settings")
@export_subgroup("Animation Names", "animation_name_")
@export var animation_name_transform_case: Case = Case.KEEP
@export var animation_name_format: String = "%s"

@export_group("Export Settings")
@export_subgroup("Export Animation Files", "animation_")
@export_custom(PROPERTY_HINT_GROUP_ENABLE, "") var animation_export_files: bool
@export var animation_save_directory: String = "animations/"
@export var animation_filename_transform_case: Case = Case.KEEP
@export var animation_filename_format: String = "%s.tres"

@export_group("")
@export_custom(PROPERTY_HINT_NONE, "", PROPERTY_USAGE_EDITOR | PROPERTY_USAGE_READ_ONLY)
var _inspector_view: bool

var _source_cache: Dictionary = {}

var _transactions: Transactions

### Getters

func get_source_animation_list() -> Array[StringName]:
	_read_source()
	var list : Array[StringName] = []
	for anim_name in _source_cache.animations:
		list.append(anim_name)
	return list

func get_inheriting_from_animation(anim: Animation) -> StringName:
	if anim.has_meta(META_NAME_INHERITING_FROM_ANIMATION):
		return anim.get_meta(META_NAME_INHERITING_FROM_ANIMATION)
	return &""

func transform_animation_name(name: String) -> String:
	return animation_name_format % convert_case(name, animation_name_transform_case)

func create_filepath_for_animation_name(name: String) -> String:
	var base_path: String
	var dir_path: String
	if animation_save_directory.begins_with("res://"):
		dir_path = animation_save_directory
	elif resource_path:
		base_path = resource_path.get_slice("::",0).get_base_dir()
		dir_path = base_path.path_join(animation_save_directory) if animation_save_directory else base_path
	else:
		return ""
	
	var filename := animation_filename_format % convert_case(name, animation_filename_transform_case)
	return dir_path.path_join(filename)

### Actions

func dialog_new_animation() -> void:
	_read_source()
	
	var dialog := NewAnimationDialog.new()
	dialog.name_transform_function = transform_animation_name
	dialog.name_validate_function = func(n): return !has_animation(n)
	dialog.populate_inherit_options(_source_cache.animations.keys())
	dialog.set_default_name(transform_animation_name("animation_name"))
	
	dialog.confirmed.connect(func():
		var new_animation_name := dialog.get_new_animation_name()
		var inherited_animation_name := dialog.get_inherited_animation_name()
		new_animation(new_animation_name, inherited_animation_name)
	)
	
	EditorInterface.popup_dialog_centered(dialog)

func new_animation(new_anim_name: StringName, inherited_anim_name: StringName) -> void:
	_read_source()
	var inherited_anim: Animation = _source_cache.animations[inherited_anim_name] if inherited_anim_name else null
	print("New animation: %s extending %s" % [new_anim_name, inherited_anim_name])
	
	var new_anim := Animation.new()
	if inherited_anim_name: new_anim.set_meta(META_NAME_INHERITING_FROM_ANIMATION, inherited_anim_name)
	
	var undo_redo := EditorInterface.get_editor_undo_redo()
	var action_name := "Create animation '%s'" % [new_anim_name]
	undo_redo.create_action(action_name, UndoRedo.MERGE_ALL, new_anim, true)
	undo_redo.add_do_method(self, &"add_animation", new_anim_name, new_anim)
	undo_redo.add_undo_method(self, &"remove_animation", new_anim_name)
	undo_redo.commit_action()
	
	import_animation(new_anim, new_anim_name, inherited_anim, action_name)
	
	_edit_animation(new_anim_name)
	
	if animation_export_files:
		var save_path := create_filepath_for_animation_name(new_anim_name)
		if save_path:
			DirAccess.make_dir_absolute(save_path.get_base_dir())
		new_anim.take_over_path(save_path)

func reimport_animation(anim_name: StringName, action_name: String = "") -> void:
	var anim := get_animation(anim_name)
	if !anim: return
	var inheriting_from_animation_name := get_inheriting_from_animation(anim)
	if !inheriting_from_animation_name: return
	_read_source()
	
	if !_source_cache.animations.has(inheriting_from_animation_name):
		_error("Failed to reimport animation '%s'; it's configured to inherit from source animation '%s', but no such animation exists in the source." % [anim_name, inheriting_from_animation_name])
		return
	
	var source_anim: Animation = _source_cache.animations[inheriting_from_animation_name]
	import_animation(anim, anim_name, source_anim, action_name)

func reimport_all() -> void:
	_read_source()
	for anim_name in get_animation_list():
		reimport_animation(anim_name)

func import_animation(anim: Animation, anim_name: StringName, source_anim: Animation, action_name: String = "") -> void:
	if anim == source_anim:
		_error("Animation cannot inherit from itself.")
		return
	
	var undo_redo := EditorInterface.get_editor_undo_redo()
	
	var timing_tool = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_timing_tool.gd").new()
	timing_tool.set_context(null, anim, anim_name)
	if timing_tool.has_timing_data():
		var original_animation_length: float = timing_tool.get_timing_data().original_animation_length
		if abs(original_animation_length - source_anim.length) > 0.01:
			_warn("Animation '%s' was expecting an animation of length %s, but the new source animation is now length %s. Please check the newly imported animation." % [anim_name, original_animation_length, source_anim.length])
	
	action_name = action_name if action_name else "Reimport animation '%s'" % [anim_name]
	_transactions = Transactions.new(undo_redo, anim, action_name)
	
	# Remove imported tracks
	for track_index in range(anim.get_track_count()-1, -1, -1):
		if !anim.track_is_imported(track_index): continue
		_transactions.remove_track(track_index)
	
	# Add all tracks from parent animation
	for source_track_index in range(source_anim.get_track_count()-1, -1, -1):
		var track_state := TrackState.create_from(source_anim, source_track_index)
		var track_path := source_anim.track_get_path(source_track_index)
		
		if anim.find_track(track_path, track_state.track_type) != -1:
			continue # There already is a non-imported track for this track
		
		_transactions.add_track(track_state.track_type, 0, track_path)
		_transactions.update_track(0, track_state)
		
		_transactions.copy_track_keys(0, source_anim, source_track_index)
	
	# Other animation data
	undo_redo.create_action(action_name, UndoRedo.MERGE_ALL, anim, true)
	undo_redo.add_do_property(anim, &"length", source_anim.length)
	undo_redo.add_do_property(anim, &"loop_mode", source_anim.loop_mode)
	undo_redo.add_undo_property(anim, &"length", anim.length)
	undo_redo.add_undo_property(anim, &"loop_mode", anim.loop_mode)
	undo_redo.commit_action()
	
	if timing_tool.has_timing_data():
		timing_tool.apply_timing_change()

func set_animation_inheritance(anim_name: StringName, new_source_anim_name: StringName, reimport: bool = false, action_name: String = "") -> void:
	var anim := get_animation(anim_name)
	if !anim: return
	
	var old_meta = anim.get_meta(META_NAME_INHERITING_FROM_ANIMATION)

	var undo_redo := EditorInterface.get_editor_undo_redo()
	action_name = action_name if action_name else "Set animation inheritance for '%s'" % [anim_name]
	undo_redo.create_action(action_name, UndoRedo.MERGE_ALL, anim, true)

	undo_redo.add_undo_method(anim, &"emit_changed")
	
	if new_source_anim_name:
		undo_redo.add_do_method(anim, &"set_meta", META_NAME_INHERITING_FROM_ANIMATION, new_source_anim_name)
	else:
		undo_redo.add_do_method(anim, &"remove_meta", META_NAME_INHERITING_FROM_ANIMATION)

	if old_meta:
		undo_redo.add_undo_method(anim, &"set_meta", META_NAME_INHERITING_FROM_ANIMATION, old_meta)
	else:
		undo_redo.add_undo_method(anim, &"remove_meta", META_NAME_INHERITING_FROM_ANIMATION)

	undo_redo.add_do_method(anim, &"emit_changed")
	
	undo_redo.commit_action()
	
	if reimport && new_source_anim_name:
		reimport_animation(anim_name, action_name)

### Private Utilities

func _read_source() -> void:
	if _source_cache: return
	_source_cache = {
		"valid": false,
		"scene_instance": null,
		"animation_player": null,
		"animations": {}
	}
	
	if !source_scene: return
	
	if !ResourceLoader.exists(source_scene, "PackedScene"):
		_error("Source scene does not exist: %s" % [source_scene])
		return
	var loaded_resource := ResourceLoader.load(source_scene, "ResourceScene", ResourceLoader.CACHE_MODE_REUSE)
	if !loaded_resource:
		_error("Failed to load source scene at %s" % [source_scene])
		return
	if !loaded_resource is PackedScene:
		_error("Source does not point to a PackedScene. Found %s at path: %s" % [loaded_resource.get_class(), source_scene])
		return
	
	var packed_scene := loaded_resource as PackedScene
	var instance := packed_scene.instantiate()
	if !instance:
		_error("Failed to instantiate scene at %s" % [source_scene])
		return
	
	_source_cache.valid = true
	_source_cache.scene_instance = instance
	
	if !EditorInterface.get_resource_filesystem().filesystem_changed.is_connected(_clear_source_cache):
		EditorInterface.get_resource_filesystem().filesystem_changed.connect(_clear_source_cache)
		
	for child in instance.get_children():
		if child is AnimationPlayer:
			_source_cache.animation_player = child
			_collect_source_animations(child)

func _collect_source_animations(mixer: AnimationMixer):
	var output: Array[Dictionary] = []
	for library_name: StringName in mixer.get_animation_library_list():
		var library := mixer.get_animation_library(library_name)
		for anim_name: StringName in library.get_animation_list():
			var anim := library.get_animation(anim_name)
			var full_anim_name := StringName("%s/%s" % [library_name, anim_name]) if library_name else anim_name
			_source_cache.animations[full_anim_name] = anim
	return output

func _clear_source_cache() -> void:
	if !_source_cache: return
	print("Clearing source cache")
	if "scene_instance" in _source_cache && is_instance_valid(_source_cache.scene_instance):
		_source_cache.scene_instance.free()
	_source_cache = {}

func _edit_animation(anim_name: StringName) -> void:
	if !anim_name: return
	var anim := get_animation(anim_name) if anim_name else null
	if !anim: return

	var editor = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_player_editor.gd")
	var mixer: AnimationMixer = editor.current_animation_mixer
	if mixer:
		for library_name in mixer.get_animation_library_list():
			if mixer.get_animation_library(library_name) == self:
				var full_anim_name = library_name + "/" + anim_name if library_name else anim_name
				editor.make_visible()
				editor.set_deferred(&"current_animation_name", full_anim_name)
				return

func _error(message: String) -> void:
	EditorInterface.get_editor_toaster().push_toast(message, EditorToaster.SEVERITY_ERROR)
	printerr("[Animation Library Importer | Error] " + message)

func _warn(message: String) -> void:
	EditorInterface.get_editor_toaster().push_toast(message, EditorToaster.SEVERITY_WARNING)
	push_warning("[Animation Library Importer | Warning] " + message)

### Engine callbacks

func _validate_property(property: Dictionary) -> void:
	if str(property.name).ends_with("_case"):
		var new_hint := ""
		for key in Case:
			var value = Case[key]
			var display_name : String = CASE_DISPLAY_NAMES[value]
			if !new_hint.is_empty():
				new_hint += ","
			new_hint += "%s:%s" % [display_name, value]
		property.hint_string = new_hint

func _notification(what: int) -> void:
	match what:
		NOTIFICATION_PREDELETE:
			print("Animation Library Importer predelete")
			# _clear_source_cache()
			# except we can't call own functions from NOTIFICATION_PREDELETE yay...
			if _source_cache && "scene_instance" in _source_cache && is_instance_valid(_source_cache.scene_instance):
				print("Freeing animation library source")
				_source_cache.scene_instance.free()

const CASE_DISPLAY_NAMES := {
	Case.KEEP: "(keep)",
	Case.SNAKE_CASE: "snake_case",
	Case.PASCAL_CASE: "PascalCase",
	Case.CAMEL_CASE: "camelCase",
	Case.KEBAB_CASE: "kebab-case",
	Case.CAPITALIZE: "Title Case",
}

static func convert_case(str: String, case: Case) -> String:
	match case:
		Case.KEEP:
			return str
		Case.SNAKE_CASE:
			return str.to_snake_case()
		Case.PASCAL_CASE:
			return str.to_pascal_case()
		Case.CAMEL_CASE:
			return str.to_camel_case()
		Case.KEBAB_CASE:
			return str.to_kebab_case()
		Case.CAPITALIZE:
			return str.capitalize()
	return str

enum Case {
	KEEP,
	SNAKE_CASE,
	PASCAL_CASE,
	CAMEL_CASE,
	KEBAB_CASE,
	CAPITALIZE
}

class NewAnimationDialog extends ConfirmationDialog:
	var name_transform_function: Callable
	var name_validate_function: Callable
	
	var _name_field: LineEdit
	var _inherit_options: OptionButton

	var _name_manually_changed := false

	var _default_name : String
	
	func _init():
		title = "New Animation"
		borderless = false
		size = Vector2i(350, 160)
		theme = EditorInterface.get_editor_theme()
		transient = true
		exclusive = true
		close_requested.connect(queue_free)
		confirmed.connect(queue_free)
		
		var content := VBoxContainer.new()
		add_child(content)
		
		var name_label := Label.new()
		content.add_child(name_label)
		name_label.text = "New Animation Name:"
		
		var name_field := LineEdit.new()
		_name_field = name_field
		content.add_child(name_field)
		name_field.theme_type_variation = &"CodeEdit"
		
		var inherit_row := HBoxContainer.new()
		content.add_child(inherit_row)
		inherit_row.custom_minimum_size = Vector2(1, 29)
		
		var inherit_label := Label.new()
		inherit_row.add_child(inherit_label)
		inherit_label.text = "Inherit from:"
		inherit_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		
		var inherit_options := OptionButton.new()
		_inherit_options = inherit_options
		inherit_row.add_child(inherit_options)
		inherit_options.size_flags_horizontal = Control.SIZE_EXPAND_FILL

		inherit_options.item_selected.connect(_on_item_selected)
		name_field.text_changed.connect(_on_text_changed)
	
	func set_default_name(default_name: String) -> void:
		_default_name = default_name
		_name_field.text = default_name
		
	func _ready() -> void:
		_name_field.text = _default_name
		_name_manually_changed = false
		_name_field.grab_focus.call_deferred()
		_name_field.select_all()
		validate_ok_button()
	
	func populate_inherit_options(anim_names: Array) -> void:
		_inherit_options.clear()
		_inherit_options.add_item("[none]", -1)
		_inherit_options.set_item_metadata(_inherit_options.item_count-1, &"")
		for name: StringName in anim_names:
			_inherit_options.add_item(name)
			_inherit_options.set_item_metadata(_inherit_options.item_count-1, name)
		
	func _on_item_selected(index: int) -> void:
		var anim_name = _inherit_options.get_item_metadata(index)
		if anim_name && (!_name_field.text.strip_edges() || _name_field.text == _default_name || !_name_manually_changed):
			var new_name: String = anim_name
			if name_transform_function:
				new_name = name_transform_function.call(new_name)
			_name_field.text = new_name
			_name_manually_changed = false
			validate_ok_button()
	
	func _on_text_changed(text: String) -> void:
		validate_ok_button()
		_name_manually_changed = true
	
	func validate_ok_button() -> void:
		get_ok_button().disabled = !_name_field.text.strip_edges() || (name_validate_function && !name_validate_function.call(_name_field.text))
	
	func get_new_animation_name() -> StringName:
		return _name_field.text as StringName
	
	func get_inherited_animation_name() -> StringName:
		return _inherit_options.get_selected_metadata()