extends Control

const timing_tool_script = preload(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_timing_tool.gd")

const ACTION_INITIALIZE := 0
const ACTION_LINK := 1
const ACTION_UNLINK := 2
const ACTION_APPLY_TIMING_CHANGE := 3
const ACTION_REVERT := 4
const ACTION_REMOVE := 5

var name_limit_function: Callable

var _hovered := false

var _link_button: Button

var _revert_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"Reload", &"EditorIcons")
var _remove_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"Remove", &"EditorIcons")
var _linked_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"Instance", &"EditorIcons")
var _unlinked_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"Unlinked", &"EditorIcons")
var _dropdown_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"GuiDropdown", &"EditorIcons")
var _broken_icon : Texture2D = EditorInterface.get_editor_theme().get_icon(&"FileBroken", &"EditorIcons")

var _menu: PopupMenu

func _init():
	mouse_filter = Control.MOUSE_FILTER_PASS
	anchor_top = 0
	anchor_left = 0
	anchor_bottom = 1
	anchor_right = 1

	_link_button = Button.new()
	_link_button.toggle_mode = true
	add_child(_link_button)
	_link_button.pressed.connect(_on_link_button_pressed)

func update_state() -> void:
	var tool := timing_tool_script.new()
	tool.set_context_from_editor()

	if tool.has_timing_data():
		_link_button.disabled = false
		if tool.is_linked():
			_link_button.icon = _linked_icon
			_link_button.tooltip_text = "Control points linked to animation timing.\nPress to unlink."
			_link_button.set_pressed_no_signal(true)
		else:
			_link_button.icon = _unlinked_icon
			_link_button.tooltip_text = "Control points are free to edit.\nPress to link control points to animation timing."
			_link_button.set_pressed_no_signal(false)
	else:
		_link_button.disabled = true
		_link_button.icon = _broken_icon
		_link_button.tooltip_text = "This timing track has not been initialized correctly."
		_link_button.set_pressed_no_signal(false)

func _on_link_button_pressed() -> void:
	var tool := timing_tool_script.new()
	tool.set_context_from_editor()
	if tool.has_timing_data():
		if !tool.is_linked():
			tool.link()
		else:
			tool.prompt_unlink()
	update_state()

func _ready() -> void:
	update_state()

func _notification(what: int) -> void:
	match what:
		NOTIFICATION_MOUSE_ENTER:
			_hovered = true
			queue_redraw()
		NOTIFICATION_MOUSE_EXIT:
			_hovered = false
			queue_redraw()

func _get_name_limit() -> int:
	if name_limit_function:
		return name_limit_function.call()
	else:
		return 0

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.position.x > _get_name_limit(): return
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
	if tool.has_timing_data():
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
			_menu.set_item_disabled(_menu.item_count-1, !tool.has_applied_timing_changes())
		_menu.add_separator()
		_menu.add_icon_item(_remove_icon, "Remove Timing Track", ACTION_REMOVE)
		_menu.set_item_tooltip(_menu.item_count-1, "Remove timing track entirely, keeping the current state of the animation.\nDon't do this if the animation is imported! Keeping this track will allow you to quickly apply the same timing changes next time it is reimported.")
	else:
		_menu.add_item("Initialize", ACTION_INITIALIZE)

	_menu.reset_size()
	_menu.position = position
	_menu.popup()

func _popup_menu_action_pressed(id: int) -> void:
	var tool := timing_tool_script.new()
	tool.set_context_from_editor()

	match id:
		ACTION_INITIALIZE:
			tool.prompt_initialize()
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

	var limit: int = _get_name_limit()
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