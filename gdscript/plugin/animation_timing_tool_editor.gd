@tool
extends RefCounted

var _editor_controls: EditorControlCache = EditorControlCache.new()
var _retiming_tracks_popup_item_index := -1

func enable() -> void:
	_editor_controls.fetch()
	_editor_controls.timeline_edit.track_added.connect(_add_track)
	EditorInterface.get_base_control().theme_changed.connect(_add_menu_items)
	_add_menu_items()

func _add_menu_items() -> void:
	print("Add menu items")
	var editor_theme := EditorInterface.get_editor_theme()
	_editor_controls.add_track_popup.add_icon_item(editor_theme.get_icon(&"Time", &"EditorIcons"), "Retiming Tracks...")
	_retiming_tracks_popup_item_index = _editor_controls.add_track_popup.item_count-1

func _remove_menu_items() -> void:
	if _retiming_tracks_popup_item_index != -1:
		_editor_controls.add_track_popup.remove_item(_retiming_tracks_popup_item_index)
		_retiming_tracks_popup_item_index = -1
	
func disable() -> void:
	_remove_menu_items()
	_editor_controls.timeline_edit.track_added.disconnect(_add_track)
	EditorInterface.get_base_control().theme_changed.disconnect(_add_menu_items)

func _add_track(index: int) -> void:
	if index == _retiming_tracks_popup_item_index:
		_editor_controls.pick_track.hide()
		var tool = load("res://addons/gdf-godot/gdscript/plugin/animation_timing_tool.gd").new()
		tool.initialize_tracks()

class EditorControlCache extends RefCounted:
	var editor

	var editor_control: Control
	var main_vbox: VBoxContainer
	var top_hbox: HBoxContainer
	var tool_anim: MenuButton
	var animation_track_editor: Control
	var timeline_edit: Control
	var timeline_edit_left: Container
	var pick_track: Window
	var add_track_button: MenuButton
	var add_track_popup: PopupMenu
	
	func fetch() -> void:
		editor = load("res://addons/gdf-godot/gdscript/plugin/animation_player_editor.gd")
		
		var editor_control: Control = editor.animation_editor_control
		# editor_control.print_tree_pretty()
		
		main_vbox = editor_control.find_children("*","VBoxContainer", true, false)[0]
		print("main_vbox:")
		print(main_vbox)
		
		top_hbox = main_vbox.find_children("*","HBoxContainer", true, false)[0]
		print("top_hbox:")
		print(top_hbox)
		
		tool_anim = top_hbox.find_children("*","MenuButton", true, false)[0]
		print("tool_anim:")
		print(tool_anim)
		
		animation_track_editor = main_vbox.find_children("*","AnimationTrackEditor", true, false)[0]
		print("animation_track_editor:")
		print(animation_track_editor)

		pick_track = animation_track_editor.find_children("*","SceneTreeDialog", true, false)[0]
		print("pick_track:")
		print(pick_track)

		timeline_edit = animation_track_editor.find_children("*","AnimationTimelineEdit", true, false)[0]
		print("timeline_edit:")
		print(timeline_edit)
		
		timeline_edit_left = timeline_edit.get_child(1)
		
		timeline_edit_left.print_tree_pretty()
		add_track_button = timeline_edit_left.find_children("*","MenuButton", true, false)[0]
		
		add_track_popup = add_track_button.get_popup()