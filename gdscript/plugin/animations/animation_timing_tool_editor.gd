@tool
extends RefCounted

const timing_tool_script = preload(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_timing_tool.gd")
const timing_track_overlay = preload(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_timing_track_overlay.gd")

signal animation_data_updated()

var _editor_controls: EditorControlCache = EditorControlCache.new()
var _retiming_tracks_popup_item_index := -1
var _tracks_changed_queued := false

func enable() -> void:
	_editor_controls.fetch()
	_editor_controls.timeline_edit.track_added.connect(_add_track)
	EditorInterface.get_base_control().theme_changed.connect(_add_menu_items)
	_editor_controls.track_vbox.child_order_changed.connect(_on_tracks_changed_defer)
	_editor_controls.animation_track_editor.animation_step_changed.connect(_on_animation_changed.unbind(1))
	_add_menu_items()

func disable() -> void:
	_remove_menu_items()
	_editor_controls.timeline_edit.track_added.disconnect(_add_track)
	EditorInterface.get_base_control().theme_changed.disconnect(_add_menu_items)
	_editor_controls.track_vbox.child_order_changed.disconnect(_on_tracks_changed_defer)
	_editor_controls.animation_track_editor.animation_step_changed.disconnect(_on_animation_changed.unbind(1))

func _add_menu_items() -> void:
	var editor_theme := EditorInterface.get_editor_theme()
	_editor_controls.add_track_popup.add_icon_item(editor_theme.get_icon(&"Time", &"EditorIcons"), "Timing Track...")
	_retiming_tracks_popup_item_index = _editor_controls.add_track_popup.item_count-1

func _remove_menu_items() -> void:
	if _retiming_tracks_popup_item_index != -1:
		_editor_controls.add_track_popup.remove_item(_retiming_tracks_popup_item_index)
		_retiming_tracks_popup_item_index = -1

func _add_track(index: int) -> void:
	if index == _retiming_tracks_popup_item_index:
		_editor_controls.pick_track.hide()
		var tool = timing_tool_script.new()
		tool.set_context_from_editor()
		if !tool.has_timing_data():
			tool.initialize()

func _on_tracks_changed_defer() -> void:
	if !_tracks_changed_queued:
		if _editor_controls.track_vbox.is_visible_in_tree():
			# If editor is visible, we can just wait until the frame is next drawn
			if !RenderingServer.frame_post_draw.is_connected(_on_tracks_changed_and_drawn):
				RenderingServer.frame_post_draw.connect(_on_tracks_changed_and_drawn, CONNECT_ONE_SHOT)
		else:
			# Editor not visible, so wait until it becomes visible
			if !_editor_controls.track_vbox.visibility_changed.is_connected(_on_tracks_changed_defer):
				_editor_controls.track_vbox.visibility_changed.connect(_on_tracks_changed_defer)
			
		_tracks_changed_queued = true
		set_deferred(&"_tracks_changed_queued", false)

func _on_tracks_changed_and_drawn() -> void:
	_tracks_changed_queued = false
	for track_edit: Control in _editor_controls.track_vbox.find_children("*","AnimationTrackEdit",true,false):
		var path : String = track_edit.get_tooltip(Vector2(100, 13))
		if path.contains("metadata/retiming:") && !track_edit.has_meta("_timing_tool_initialized"):
			# print("Track edit: %s" % [path])
			track_edit.set_meta("_timing_tool_initialized", true)
			_on_timing_track_edit_created(track_edit, path)

func _on_timing_track_edit_created(track_edit: Control, property_path: String) -> void:
	var tool = timing_tool_script.new()
	tool.set_context_from_editor()
	tool.force_fix_track_imported()
	
	var overlay := timing_track_overlay.new()
	overlay.name_limit_function = get_name_limit
	overlay.set_meta("_owner", self) # keep self alive while overlay is in memory
	track_edit.add_child(overlay)
	animation_data_updated.connect(overlay.update_state)
	_editor_controls.timeline_edit.name_limit_changed.connect(overlay.queue_redraw)

	track_edit.remove_request.connect(_on_remove_request, CONNECT_DEFERRED)
	track_edit.move_selection_commit.connect(_on_move_selection_commit, CONNECT_DEFERRED)

func _on_animation_changed() -> void:
	animation_data_updated.emit()

func get_name_limit() -> int:
	var timeline := _editor_controls.timeline_edit
	var hsize_icon: Texture2D = _editor_controls.timeline_edit.get_theme_icon(&"Hsize", &"EditorIcons")
	
	return _editor_controls.add_track_hb.size.x + ((hsize_icon.get_width() + 16) * EditorInterface.get_editor_scale())

func _on_move_selection_commit() -> void:
	var tool = timing_tool_script.new()
	tool.set_context_from_editor()
	if tool.is_linked():
		tool.apply_timing_change()

func _on_remove_request(track_index: int) -> void:
	var tool = timing_tool_script.new()
	tool.set_context_from_editor().uninitialize()
		
class EditorControlCache extends RefCounted:
	var editor

	var editor_control: Control
	var main_vbox: VBoxContainer
	var top_hbox: HBoxContainer
	var tool_anim: MenuButton
	var animation_track_editor: Control
	var track_vbox: Control
	var timeline_edit: Control
	var add_track_hb: Container
	var pick_track: Window
	var add_track_button: MenuButton
	var add_track_popup: PopupMenu
	var filter_track: LineEdit
	var len_hb: Container
	var length_slider: EditorSpinSlider
	
	func fetch() -> void:
		editor = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_player_editor.gd")
		
		var editor_control: Control = editor.animation_editor_control
		# editor_control.print_tree_pretty()
		
		main_vbox = editor_control.find_children("*","VBoxContainer", true, false)[0]
		top_hbox = main_vbox.find_children("*","HBoxContainer", true, false)[0]
		tool_anim = top_hbox.find_children("*","MenuButton", true, false)[0]

		animation_track_editor = main_vbox.find_children("*","AnimationTrackEditor", true, false)[0]
		
		if !track_vbox:
			for candidate: MarginContainer in animation_track_editor.find_children("*","MarginContainer", true, false):
				if candidate.theme_type_variation == &"AnimationTrackMargins":
					track_vbox = candidate.get_child(0)
					break

		pick_track = animation_track_editor.find_children("*","SceneTreeDialog", true, false)[0]

		timeline_edit = animation_track_editor.find_children("*","AnimationTimelineEdit", true, false)[0]
		
		add_track_hb = timeline_edit.get_child(1)
		
		add_track_button = add_track_hb.find_children("*","MenuButton", false, false)[0]
		add_track_popup = add_track_button.get_popup()
		filter_track = add_track_hb.find_children("*","LineEdit", false, false)[0]
		
		len_hb = timeline_edit.get_child(2)

		length_slider = len_hb.find_children("*","EditorSpinSlider", true, false)[0]
