@tool
class_name GdfEditorPlugin
extends EditorPlugin

static var instance: EditorPlugin
const PLUGIN_ROOT: String = "res://addons/gdf-godot"

var animation_editor
var _inspector_plugins : Array[EditorInspectorPlugin]
var _resource_conversion_plugins : Array[EditorResourceConversionPlugin]

var _animation_player_editor: Script = load(PLUGIN_ROOT + "/gdscript/plugin/animations/animation_player_editor.gd")
var _animation_timing_tool_editor
var _animation_track_browser_editor

func _enter_tree() -> void:
	instance = self
	
	# C#
	_inspector_plugins.append(load(PLUGIN_ROOT + "/GDF/Editor/ToolInspectorPlugin.cs").new())
	# Animations
	_inspector_plugins.append(load(PLUGIN_ROOT + "/gdscript/plugin/animations/animation_node_inspector_plugin.gd").new())
	_inspector_plugins.append(load(PLUGIN_ROOT + "/gdscript/plugin/animations/animation_timing_tool_inspector_plugin.gd").new())
	_animation_timing_tool_editor = load(PLUGIN_ROOT + "/gdscript/plugin/animations/animation_timing_tool_editor.gd").new()
	if _animation_timing_tool_editor:
		_animation_timing_tool_editor.enable()
	_animation_track_browser_editor = load(PLUGIN_ROOT + "/gdscript/plugin/animations/animation_track_browser_editor.gd").new()
	if _animation_track_browser_editor:
		_animation_track_browser_editor.enable()
	_inspector_plugins.append(load(PLUGIN_ROOT + "/gdscript/plugin/animations/animation_root_motion_track_inspector_plugin.gd").new())
	# Materials
	_inspector_plugins.append(load(PLUGIN_ROOT + "/gdscript/plugin/materials/material_inspector_plugin.gd").new())
	_resource_conversion_plugins.append(load(PLUGIN_ROOT + "/gdscript/plugin/materials/custom_material_to_shader_converter.gd").new())
	
	_add_plugins(_inspector_plugins, add_inspector_plugin)
	_add_plugins(_resource_conversion_plugins, add_resource_conversion_plugin)
	
	print("Initialized Genery Development Framework Editor Plugin (gdscript)")
	SceneManager.AddProjectSettings()

func _exit_tree() -> void:
	if instance == self:
		instance = null

	if _animation_timing_tool_editor:
		_animation_timing_tool_editor.disable()
	if _animation_track_browser_editor:
		_animation_track_browser_editor.disable()

	_remove_plugins(_inspector_plugins, remove_inspector_plugin)
	_remove_plugins(_resource_conversion_plugins, remove_resource_conversion_plugin)

func _add_plugins(list, callable) -> void:
	for plugin in list:
		if plugin:
			callable.call(plugin)

func _remove_plugins(list, callable) -> void:
	for plugin in list:
		if plugin:
			callable.call(plugin)
	list.clear()

func _handles(object: Object) -> bool:
	if object is AnimationMixer: return true
	return false

func _edit(object: Object) -> void:
	_animation_player_editor._update_editing_mixer(object)