@tool
extends RefCounted

const timing_tool_script = preload("res://addons/gdf-godot/gdscript/plugin/animation_timing_tool.gd")

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
	var overlay := TimingTrackOverlay.new(self)
	track_edit.add_child(overlay)
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
	
class TimingTrackOverlay extends Control:
	const ACTION_LINK := 1
	const ACTION_UNLINK := 2
	const ACTION_APPLY_TIMING_CHANGE := 3
	const ACTION_REVERT := 4
	const ACTION_REMOVE := 5
	
	var _top_level_script
	var _hovered := false
	
	var _link_button: Button

	var _revert_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"Reload", &"EditorIcons")
	var _remove_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"Remove", &"EditorIcons")
	var _linked_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"Instance", &"EditorIcons")
	var _unlinked_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"Unlinked", &"EditorIcons")
	var _dropdown_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"GuiDropdown", &"EditorIcons")

	var _menu: PopupMenu

	func _init(top_level_script: Object):
		_top_level_script = top_level_script

		mouse_filter = Control.MOUSE_FILTER_PASS
		anchor_top = 0
		anchor_left = 0
		anchor_bottom = 1
		anchor_right = 1
		
		_link_button = Button.new()
		_link_button.toggle_mode = true
		add_child(_link_button)
		_link_button.pressed.connect(_on_link_button_pressed)
		
		top_level_script.animation_data_updated.connect(_update_state)
	
	func _update_state() -> void:
		var tool := timing_tool_script.new()
		tool.set_context_from_editor()
		if tool.is_linked():
			_link_button.icon = _linked_icon
			_link_button.tooltip_text = "Control points linked to animation timing.\nPress to unlink."
			_link_button.set_pressed_no_signal(true)
		else:
			_link_button.icon = _unlinked_icon
			_link_button.tooltip_text = "Control points are free to edit.\nPress to link control points to animation timing."
			_link_button.set_pressed_no_signal(false)
	
	func _on_link_button_pressed() -> void:
		var tool := timing_tool_script.new()
		tool.set_context_from_editor()
		if !tool.is_linked():
			tool.link()
		else:
			tool.prompt_unlink()
		_update_state()
	
	func _ready() -> void:
		_update_state()

	func _notification(what: int) -> void:
		match what:
			NOTIFICATION_MOUSE_ENTER:
				_hovered = true
				queue_redraw()
			NOTIFICATION_MOUSE_EXIT:
				_hovered = false
				queue_redraw()
	
	func _gui_input(event: InputEvent) -> void:
		if event is InputEventMouseButton:
			if event.position.x > _top_level_script.get_name_limit(): return
			if event.is_pressed() && event.button_index == MOUSE_BUTTON_LEFT || event.button_index == MOUSE_BUTTON_RIGHT:
				var popup_pos := get_screen_position() + Vector2(0, size.y)
				_popup_context_menu.call_deferred(popup_pos)
				accept_event()
	
	func _popup_context_menu(position: Vector2) -> void:
		if !_menu:
			_menu = PopupMenu.new()
			add_child(_menu)
			_menu.id_pressed.connect(_popup_menu_action_pressed)

		var tool := timing_tool_script.new()
		tool.set_context_from_editor()
			
		_menu.clear()
		if !tool.is_linked():
			_menu.add_icon_item(_linked_icon, "Link", ACTION_LINK)
			_menu.set_item_tooltip(_menu.item_count-1, "Linking the control points to the animation allows to retime the entire animation by dragging the keyframes on this track.\nYou may not add or remove control points after linking.")
		else:
			_menu.add_icon_item(_unlinked_icon, "Unlink", ACTION_UNLINK)
			_menu.set_item_tooltip(_menu.item_count-1, "Unlinking will allow you to add, remove, or move control points freely without affecting the rest of the animation.")
		if tool.is_linked():
			_menu.add_separator()
			_menu.add_item("Apply Timing Changes", ACTION_APPLY_TIMING_CHANGE)
			_menu.set_item_tooltip(_menu.item_count-1, "Apply the current state of the keyframes to the timing of the animation.\nThis is done automatically when moving keyframes in this track while linked.")
			_menu.add_icon_item(_revert_icon, "Revert", ACTION_REVERT)
			_menu.set_item_tooltip(_menu.item_count-1, "Revert the timing of the animation to how it was when this track was last linked.")

		_menu.add_separator()
		_menu.add_icon_item(_remove_icon, "Remove Timing Track", ACTION_REMOVE)
		_menu.set_item_tooltip(_menu.item_count-1, "Remove timing track entirely, keeping the current state of the animation.\nDon't do this if the animation is imported! Keeping this track will allow you to quickly apply the same timing changes next time it is reimported.")
		
		_menu.reset_size()
		_menu.position = position
		_menu.popup()
	
	func _popup_menu_action_pressed(id: int) -> void:
		var tool := timing_tool_script.new()
		tool.set_context_from_editor()
		
		match id:
			ACTION_LINK:
				tool.link()
			ACTION_UNLINK:
				tool.prompt_unlink()
			ACTION_APPLY_TIMING_CHANGE:
				tool.apply_timing_change()
			ACTION_REVERT:
				tool.revert()
			ACTION_REMOVE:
				tool.uninitialize()
	
	func _draw() -> void:
		var height := size.y
		var parent := get_parent_control()
		var editor_scale := EditorInterface.get_editor_scale()
		var grouped := parent.get_parent().get_child(0).is_class(&"AnimationTrackEditGroup")
		
		var limit: int = _top_level_script.get_name_limit()
		var bg_rect := Rect2(1,0,limit - 4 - 1,size.y).grow(-2)
		if grouped:
			# bg_rect = bg_rect.grow_side(SIDE_LEFT, -44 * editor_scale)
			pass
		else:
			# bg_rect = bg_rect.grow_side(SIDE_LEFT, -16 * editor_scale)
			bg_rect = bg_rect.grow_side(SIDE_LEFT, 2 * editor_scale)

		var stylebox_odd := parent.get_theme_stylebox(&"odd",&"AnimationTrackEdit")
		var stylebox_focus := parent.get_theme_stylebox(&"focus",&"AnimationTrackEdit")
		var stylebox_hover := parent.get_theme_stylebox(&"hover",&"AnimationTrackEdit")

		draw_style_box(stylebox_odd, bg_rect)
		if _hovered:
			draw_style_box(stylebox_hover, bg_rect)

		var ofs : float = 0
		if grouped:
			ofs += 44 * editor_scale
		else:
			ofs += 38 * editor_scale

		var link_button_rect := Rect2(6 if grouped else 0, 0, 24, height)
		_link_button.position = link_button_rect.position
		_link_button.size = link_button_rect.size
		
		var icon := get_theme_icon(&"Time",&"EditorIcons")
		draw_texture_rect(icon, Rect2(ofs,height / 2 - icon.get_height()*editor_scale / 2,icon.get_width()*editor_scale,icon.get_height()*editor_scale), false)
		ofs += icon.get_width()*editor_scale

		ofs += 4*editor_scale
		
		var text_color := parent.get_theme_color(&"font_color", &"Label")
		if grouped:
			text_color.a *= 0.7
		
		var font_to_use := parent.get_theme_font(&"source", &"EditorFonts")
		var font_size_to_use := parent.get_theme_font_size(&"source_size", &"EditorFonts")
		var text_pos := Vector2(ofs, (height - font_to_use.get_height(font_size_to_use)) / 2 + font_to_use.get_ascent(font_size_to_use))
		var text := "Timing"
		draw_string(font_to_use, text_pos, text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size_to_use, text_color)
		
		draw_texture_rect(_dropdown_icon, Rect2(limit - _dropdown_icon.get_width()*editor_scale - 12*editor_scale, height / 2 - _dropdown_icon.get_height()*editor_scale / 2, _dropdown_icon.get_width()*editor_scale, _dropdown_icon.get_height()*editor_scale), false)
		
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
		
		add_track_hb = timeline_edit.get_child(1)
		
		add_track_button = add_track_hb.find_children("*","MenuButton", false, false)[0]
		add_track_popup = add_track_button.get_popup()
		filter_track = add_track_hb.find_children("*","LineEdit", false, false)[0]
		
		len_hb = timeline_edit.get_child(2)
		print("len_hb:")
		print(len_hb)

		length_slider = len_hb.find_children("*","EditorSpinSlider", true, false)[0]
		print("length_slider:")
		print(length_slider)
