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
	
	if !EditorInterface.get_resource_filesystem().filesystem_changed.is_connected(_on_filesystem_changed):
		EditorInterface.get_resource_filesystem().filesystem_changed.connect(_on_filesystem_changed)
		
	for child in instance.get_children():
		if child is AnimationPlayer:
			_source_cache.animation_player = child
			_collect_source_animations(child)
	
	print("Cache: %s" % [_source_cache])

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

func _error(message: String) -> void:
	printerr("[Animation Library Importer | Error] " + message)

func new_animation() -> void:
	_read_source()

func _on_filesystem_changed() -> void:
	_clear_source_cache()

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
enum Case {
	KEEP,
	SNAKE_CASE,
	PASCAL_CASE,
	CAMEL_CASE,
	KEBAB_CASE,
	CAPITALIZE
}
