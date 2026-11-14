@tool
extends EditorInspectorPlugin

const META_NAME_DUCKING := &"_ducking_in_editor"

var ducking_tool_script: Script = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_tree_ducking_tool.gd")

var ducking_tools: Array

func _can_handle(object: Object) -> bool:
	if object is AnimationTree:
		return true
	return false

func _parse_end(object: Object) -> void:
	var hflow := HFlowContainer.new()
	hflow.alignment = FlowContainer.ALIGNMENT_CENTER
	var btn := Button.new()
	hflow.add_child(btn)
	btn.text = "Ducking in Editor"
	btn.toggle_mode = true
	btn.toggled.connect(_on_ducking_toggled.bind(object))
	btn.toggled.connect(_update_button_state.bind(btn, object).unbind(1))
	btn.tooltip_text = "When on, deactivates this AnimationTree whilst the corresponding AnimationPlayer is selected.\nWorkaround for Godot issue #73559 and many duplicates."
	_update_button_state(btn, object)
	add_custom_control(hflow)

func _update_button_state(btn: Button, object: Object) -> void:
	var theme := EditorInterface.get_editor_theme()
	if !_is_ducking_enabled(object):
		btn.text = "Ducking in Editor: Off"
		btn.icon = theme.get_icon(&"GuiVisibilityVisible",&"EditorIcons")
		btn.set_pressed_no_signal(false)
	else:
		btn.text = "Ducking in Editor: On"
		btn.icon = theme.get_icon(&"GuiVisibilityXray",&"EditorIcons")
		btn.set_pressed_no_signal(true)

func _on_ducking_toggled(toggled: bool, object: Object) -> void:
	var undo_redo := EditorInterface.get_editor_undo_redo()
	if toggled:
		undo_redo.create_action("Enable AnimationTree Ducking", UndoRedo.MERGE_ALL, object, true)
		undo_redo.add_do_method(object, &"set_meta", META_NAME_DUCKING, ducking_tool_script.new())
		undo_redo.add_undo_method(object, &"remove_meta", META_NAME_DUCKING)
		undo_redo.commit_action(true)
	else:
		undo_redo.create_action("Disable AnimationTree Ducking", UndoRedo.MERGE_ALL, object, true)
		undo_redo.add_do_method(object, &"remove_meta", META_NAME_DUCKING)
		undo_redo.add_undo_method(object, &"set_meta", META_NAME_DUCKING, object.get_meta(META_NAME_DUCKING))
		undo_redo.commit_action(true)

func _is_ducking_enabled(tree: Object) -> bool:
	return _get_ducking_tool(tree) != null

func _get_ducking_tool(tree: Object) -> Resource:
	if !tree.has_meta(META_NAME_DUCKING): return null
	return tree.get_meta(META_NAME_DUCKING) as Resource

func start_ducking(tree: AnimationTree, player: AnimationPlayer) -> void:
	var tool := _get_ducking_tool(tree)
	tool.start_ducking(tree, player)
	ducking_tools.append(tool)

func stop_ducking() -> void:
	for tool in ducking_tools:
		tool.stop_ducking()
	ducking_tools.clear()

func _find_animation_trees_pointing_to_player(player: AnimationPlayer) -> Array[AnimationTree]:
	var found_trees : Array[AnimationTree] = []
	var edited_scene := EditorInterface.get_edited_scene_root()
	if !edited_scene: return found_trees
	if !player.is_part_of_edited_scene(): return found_trees
	
	for tree: AnimationTree in edited_scene.find_children("*","AnimationTree",true,true):
		if !tree.anim_player: continue
		if tree.get_node(tree.anim_player) == player:
			found_trees.append(tree)
			
	return found_trees

func update_editing_mixer(object: Object) -> void:
	stop_ducking()
	var mixer := object as AnimationMixer if object is AnimationMixer else null
	if object is not AnimationPlayer: return
	for tree: AnimationTree in _find_animation_trees_pointing_to_player(object):
		if _is_ducking_enabled(tree):
			start_ducking(tree, object)