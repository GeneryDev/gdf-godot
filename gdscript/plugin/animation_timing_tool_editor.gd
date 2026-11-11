@tool
extends RefCounted

const timing_tool_script = preload("res://addons/gdf-godot/gdscript/plugin/animation_timing_tool.gd")

var _editor_controls: EditorControlCache = EditorControlCache.new()
var _retiming_tracks_popup_item_index := -1
var _tracks_changed_queued := false

func enable() -> void:
	_editor_controls.fetch()
	_editor_controls.timeline_edit.track_added.connect(_add_track)
	EditorInterface.get_base_control().theme_changed.connect(_add_menu_items)
	_editor_controls.track_vbox.child_order_changed.connect(_on_tracks_changed_defer)
	# _editor_controls.animation_track_edit.move_selection_commit.connect(_on_move_selection_commit)
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
	_editor_controls.track_vbox.child_order_changed.disconnect(_on_tracks_changed_defer)
	# _editor_controls.animation_track_edit.move_selection_commit.disconnect(_on_move_selection_commit)

func _add_track(index: int) -> void:
	if index == _retiming_tracks_popup_item_index:
		_editor_controls.pick_track.hide()
		var tool = timing_tool_script.new()
		tool.initialize_tracks()

func _on_move_selection_commit() -> void:
	print("move selection commit")

func _on_tracks_changed_defer() -> void:
	print("tracks changed")
	if _editor_controls.track_vbox.get_child_count() == 0:
		print("EMPTY")
	
	if !_tracks_changed_queued:
		RenderingServer.frame_post_draw.connect(_on_tracks_changed_and_drawn, CONNECT_ONE_SHOT)
		_tracks_changed_queued = true

func _on_tracks_changed_and_drawn() -> void:
	print("deferred")
	_tracks_changed_queued = false
	for track_edit: Control in _editor_controls.track_vbox.find_children("*","AnimationTrackEdit",true,false):
		# print("Track edit: %s" % [track_edit])
		var path : String = track_edit.get_tooltip(Vector2(100, 13))
		if path.contains("metadata/retiming:") && !track_edit.has_meta("_timing_tool_initialized"):
			track_edit.set_meta("_timing_tool_initialized", true)
			_on_timing_track_edit_created(track_edit, path)

func _on_timing_track_edit_created(track_edit: Control, property_path: String) -> void:
	print("Track edit: %s" % [property_path])
	# var overlay := ColorRect.new()
	# overlay.color = Color(1,0,0,0.2)
	# overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	# overlay.anchor_top = 0
	# overlay.anchor_left = 0
	# overlay.anchor_bottom = 1
	# overlay.anchor_right = 1
	# track_edit.add_child(overlay)
	
	track_edit.remove_request.connect(_on_remove_request, CONNECT_DEFERRED)

func _on_remove_request(track_index: int) -> void:
	var tool = timing_tool_script.new()
	tool.bake()

class EditorControlCache extends RefCounted:
	var editor

	var editor_control: Control
	var main_vbox: VBoxContainer
	var top_hbox: HBoxContainer
	var tool_anim: MenuButton
	var animation_track_editor: Control
	var track_vbox: Control
	var timeline_edit: Control
	var timeline_edit_left: Container
	var pick_track: Window
	var add_track_button: MenuButton
	var add_track_popup: PopupMenu
	
	func fetch() -> void:
		editor = load("res://addons/gdf-godot/gdscript/plugin/animation_player_editor.gd")
		
		var editor_control: Control = editor.animation_editor_control
		
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
		
		if !track_vbox:
			for candidate: MarginContainer in animation_track_editor.find_children("*","MarginContainer", true, false):
				if candidate.theme_type_variation == &"AnimationTrackMargins":
					track_vbox = candidate.get_child(0)
					break

		print("track_vbox:")
		print(track_vbox)

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