extends PanelContainer

const COLUMN_NAME := 0
const COLUMN_STORAGE := 1
const MAX_COLUMN := COLUMN_STORAGE

const ACTION_EDIT_RESOURCE := 0
const ACTION_REMOVE_ANIM := 1
const ACTION_MAKE_UNIQUE := 2
const ACTION_SHOW_IN_FILESYSTEM := 3
const ACTION_REIMPORT := 10

static var editor_scale: float:
	get:
		return EditorInterface.get_editor_scale()

var _library: AnimationLibraryImporter
var _search_bar: LineEdit
var _tree: Tree

var _detail_main_container: Control
var _detail_unselected_container: Control
var _detail_name_label: Label
var _detail_storage_button: MenuButton
var _detail_inherit_button: OptionButton
var _detail_reimport_button: Button

var _current_animation_name_in_detail: StringName
var _current_animation_in_detail: Animation
var _detail_refresh_queued := false

var icons: Dictionary = {}

func _init(library: AnimationLibraryImporter):
	_library = library
	
	theme = EditorInterface.get_editor_theme()
	icons.Animation = get_theme_icon(&"Animation",&"EditorIcons")
	icons.AnimationLibrary = get_theme_icon(&"AnimationLibrary",&"EditorIcons")
	icons.Edit = get_theme_icon(&"Edit",&"EditorIcons")
	icons.Duplicate = get_theme_icon(&"Duplicate",&"EditorIcons")
	icons.Remove = get_theme_icon(&"Remove",&"EditorIcons")
	icons.ShowInFileSystem = get_theme_icon(&"ShowInFileSystem",&"EditorIcons")
	icons.File = get_theme_icon(&"File",&"EditorIcons")
	icons.GuiTabMenuHl = get_theme_icon(&"GuiTabMenuHl",&"EditorIcons")
	icons.Load = get_theme_icon(&"Load",&"EditorIcons")
	icons.Import = get_theme_icon(&"AssetStore",&"EditorIcons")
	
	var vbox := VBoxContainer.new()
	add_child(vbox)

	vbox.add_child(HSeparator.new())
	vbox.add_child(_build_header())
	vbox.add_child(_build_tree())
	vbox.add_child(HSeparator.new())
	vbox.add_child(_build_detail_panel())
	
	_populate_tree()
	_show_detail(&"")

func _build_header() -> Control:
	var library := _library
	var header := HBoxContainer.new()

	var search_bar := LineEdit.new()
	_search_bar = search_bar
	header.add_child(search_bar)
	search_bar.size_flags_horizontal = SIZE_EXPAND_FILL
	search_bar.placeholder_text = "Search Animations (%s)" % library.get_animation_list_size()
	search_bar.text_changed.connect(_populate_tree)

	var new_animation_button := Button.new()
	header.add_child(new_animation_button)
	new_animation_button.text = "New Animation"
	new_animation_button.icon = theme.get_icon(&"Add",&"EditorIcons")
	new_animation_button.theme_type_variation = &"InspectorActionButton"
	new_animation_button.pressed.connect(library.dialog_new_animation)
	return header

func _build_tree() -> Control:
	var library := _library
	var tree := Tree.new()
	_tree = tree
	tree.custom_minimum_size = Vector2(40,300)
	tree.item_selected.connect(_on_tree_item_selected)
	tree.nothing_selected.connect(_on_tree_nothing_selected)
	tree.button_clicked.connect(_on_tree_button_clicked)
	tree.gui_input.connect(_on_tree_gui_input)
	return tree

func _build_detail_panel() -> Control:
	var library := _library
	var detail_panel := PanelContainer.new()
	detail_panel.add_theme_stylebox_override(&"panel", get_theme_stylebox(&"panel",&"Panel"))
	detail_panel.custom_minimum_size = Vector2(40, 200)

	if true:
		# Unselected panel
		var vbox := VBoxContainer.new()
		_detail_unselected_container = vbox
		detail_panel.add_child(vbox)
		
		var unselected_label := Label.new()
		vbox.add_child(unselected_label)
		unselected_label.text = "Select an animation above for more details"
		unselected_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		unselected_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		unselected_label.size_flags_vertical = SIZE_EXPAND_FILL

		var action_container := HFlowContainer.new()
		vbox.add_child(action_container)
		action_container.alignment = FlowContainer.ALIGNMENT_CENTER

		var reimport_all_button := Button.new()
		action_container.add_child(reimport_all_button)
		reimport_all_button.icon = icons.Import
		reimport_all_button.text = "Reimport All"
		reimport_all_button.theme_type_variation = &"InspectorActionButton"
		reimport_all_button.pressed.connect(library.reimport_all, CONNECT_DEFERRED)
	if true:
		# Selected panel
		var vbox := VBoxContainer.new()
		_detail_main_container = vbox
		detail_panel.add_child(vbox)
	
		var name_panel_hbox := HBoxContainer.new()
		vbox.add_child(name_panel_hbox)
		
		var name_panel := PanelContainer.new()
		name_panel.size_flags_horizontal = SIZE_EXPAND_FILL
		name_panel_hbox.add_child(name_panel)
		name_panel.add_theme_stylebox_override("panel", get_theme_stylebox(&"bg",&"EditorInspectorCategory"))
	
		var name_margin_container := MarginContainer.new()
		name_panel.add_child(name_margin_container)
		name_margin_container.add_theme_constant_override("margin_top", -4)
		name_margin_container.add_theme_constant_override("margin_bottom", -4)
		
		var name_hbox := HBoxContainer.new()
		name_margin_container.add_child(name_hbox)
		name_hbox.alignment = BoxContainer.ALIGNMENT_CENTER
		name_hbox.add_theme_constant_override("separation", 0)
		
		var anim_icon_rect := TextureRect.new()
		anim_icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		name_hbox.add_child(anim_icon_rect)
		anim_icon_rect.texture = icons.Animation
		anim_icon_rect.offset_transform_enabled = true
		anim_icon_rect.offset_transform_position = Vector2(0,0.5)
		# Offset the icon by half a pixel, because the texture rect typically ends up at an odd number height,
		# and the centering from the stretch mode ends up blurring the texture vertically otherwise
		
		var name_label := Label.new()
		_detail_name_label = name_label
		name_label.text = "animation_name"
		name_hbox.add_child(name_label)
		name_label.add_theme_font_override("font", get_theme_font("bold",&"EditorFonts"))
		
		var resource_button := MenuButton.new()
		name_panel_hbox.add_child(resource_button)
		resource_button.icon = icons.GuiTabMenuHl
		resource_button.get_popup().id_pressed.connect(func(id): _perform_action(id, _current_animation_name_in_detail), CONNECT_DEFERRED)
		_add_resource_context_menu_actions(resource_button.get_popup())
		resource_button.get_popup().add_separator()
		resource_button.get_popup().add_icon_item(icons.Remove, tr("Remove from Library"), ACTION_REMOVE_ANIM)
	
		var storage_row := HBoxContainer.new()
		vbox.add_child(storage_row)
		storage_row.alignment = BoxContainer.ALIGNMENT_CENTER
		
		var storage_label := Label.new()
		storage_row.add_child(storage_label)
		storage_label.text = "Storage"
		storage_label.size_flags_horizontal = SIZE_EXPAND_FILL
		
		var storage_button := MenuButton.new()
		_detail_storage_button = storage_button
		storage_row.add_child(storage_button)
		storage_button.text = "storage"
		storage_button.flat = false
		storage_button.size_flags_horizontal = SIZE_EXPAND_FILL
		_add_resource_context_menu_actions(storage_button.get_popup())
		storage_button.get_popup().id_pressed.connect(func(id: int): _perform_action(id, _current_animation_name_in_detail), CONNECT_DEFERRED)
	
		var inherit_row := HBoxContainer.new()
		vbox.add_child(inherit_row)
		inherit_row.alignment = BoxContainer.ALIGNMENT_CENTER
	
		var inherit_label := Label.new()
		inherit_row.add_child(inherit_label)
		inherit_label.text = "Inherits from"
		inherit_label.size_flags_horizontal = SIZE_EXPAND_FILL
	
		var inherit_button := OptionButton.new()
		_detail_inherit_button = inherit_button
		inherit_row.add_child(inherit_button)
		inherit_button.size_flags_horizontal = SIZE_EXPAND_FILL
		
		inherit_button.clear()
		inherit_button.add_item("[none]", 0)
		inherit_button.set_item_metadata(0, &"")
		for anim_name in library.get_source_animation_list():
			inherit_button.add_item(anim_name)
			inherit_button.set_item_metadata(inherit_button.item_count-1, anim_name)
		inherit_button.item_selected.connect(_on_inherit_selected, CONNECT_DEFERRED)
			
		var padding := Control.new()
		vbox.add_child(padding)
		padding.size_flags_vertical = Control.SIZE_EXPAND_FILL
		padding.mouse_filter = MOUSE_FILTER_IGNORE
		
		var action_container := HFlowContainer.new()
		vbox.add_child(action_container)
		action_container.alignment = FlowContainer.ALIGNMENT_CENTER

		var edit_button := Button.new()
		action_container.add_child(edit_button)
		edit_button.icon = icons.Edit
		edit_button.text = "Edit"
		edit_button.theme_type_variation = &"InspectorActionButton"
		edit_button.pressed.connect(func(): _perform_action(ACTION_EDIT_RESOURCE, _current_animation_name_in_detail), CONNECT_DEFERRED)

		var reimport_button := Button.new()
		action_container.add_child(reimport_button)
		_detail_reimport_button = reimport_button
		reimport_button.icon = icons.Import
		reimport_button.text = "Reimport"
		reimport_button.theme_type_variation = &"InspectorActionButton"
		reimport_button.pressed.connect(func(): _perform_action(ACTION_REIMPORT, _current_animation_name_in_detail), CONNECT_DEFERRED)
	
	return detail_panel

func _populate_tree(filter: String = "") -> void:
	var library := _library
	var tree := _tree
	var _prev_selected_anim_name := tree.get_selected().get_metadata(COLUMN_NAME) if tree.get_selected() else _current_animation_name_in_detail
	var _prev_selected_column := tree.get_selected_column()
	
	tree.clear()
	tree.hide_root = true
	tree.hide_folding = true
	tree.column_titles_visible = true
	tree.columns = MAX_COLUMN+1
	tree.select_mode = Tree.SELECT_ROW
	tree.set_column_title(COLUMN_NAME, "Name")
	tree.set_column_title(COLUMN_STORAGE, "Storage")
	tree.set_column_expand_ratio(COLUMN_NAME, 2)
	var root_item := tree.create_item()
	
	for anim_name in library.get_animation_list():
		
		var anim := library.get_animation(anim_name)
		
		var storage_label: String = _get_item_storage_display(anim).label

		if filter:
			if !anim_name.containsn(filter) && !storage_label.containsn(filter):
				continue

		var anim_item := tree.create_item(root_item)
		anim_item.set_text(COLUMN_NAME, anim_name)
		anim_item.set_icon(COLUMN_NAME, icons.Animation)
		anim_item.set_metadata(COLUMN_NAME, anim_name)
		anim_item.set_text(COLUMN_STORAGE, storage_label)
		anim_item.set_tooltip_text(COLUMN_STORAGE, anim.resource_path)
		
		anim_item.add_button(COLUMN_STORAGE, icons.Remove, ACTION_REMOVE_ANIM, false, tr("Remove from Library"), "Remove the animation from this library")
		
		if anim_name == _prev_selected_anim_name:
			anim_item.select(_prev_selected_column)

func _get_item_storage_display(anim: Animation) -> Dictionary:
	var library := _library
	var storage_label: String = ""
	var saved_path := anim.resource_path
	var icon: Texture2D = null
	if !saved_path:
		storage_label = "[unsaved]"
	elif saved_path.contains("::"):
		if saved_path.begins_with(library.resource_path):
			storage_label = "[built-in]"
		else:
			storage_label = "[foreign]"
	else:
		storage_label = saved_path.get_file()
		icon = icons.File
	
	return {
		"label": storage_label,
		"icon": icon
	}

func _on_tree_nothing_selected() -> void:
	_tree.deselect_all()
	_show_detail(&"")

func _on_tree_item_selected() -> void:
	var library := _library
	var tree := _tree
	var selected := tree.get_selected()
	
	if selected:
		var selected_anim_name: StringName = selected.get_metadata(COLUMN_NAME)
		if selected_anim_name: _show_detail(selected_anim_name)

func _on_tree_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.pressed && event.button_index == MOUSE_BUTTON_RIGHT:
			var item_at_pos := _tree.get_item_at_position(event.position)
			item_at_pos.select(0)
			_show_tree_item_context_menu(item_at_pos, _get_input_popup_position(event))

func _get_input_popup_position(event: InputEventMouse) -> Vector2:
	var pos := event.global_position

	if EditorInterface.is_multi_window_enabled():
		pos += Vector2(DisplayServer.window_get_position(event.window_id))

	return pos

func _on_tree_button_clicked(item: TreeItem, column: int, id: int, mouse_button_index: int) -> void:
	var library := _library
	var tree := _tree
	var anim_name := item.get_metadata(COLUMN_NAME)
	
	if !anim_name: return
	_perform_action(id, anim_name)

func _show_tree_item_context_menu(item: TreeItem, global_position: Vector2) -> void:
	var library := _library
	var tree := _tree
	var anim_name := item.get_metadata(COLUMN_NAME)
	if !anim_name: return
	
	var menu := PopupMenu.new()
	_tree.add_child(menu)
	menu.close_requested.connect(menu.queue_free)
	
	_add_animation_context_menu_actions(menu, anim_name)
	menu.add_separator()
	_add_resource_context_menu_actions(menu)
	menu.add_separator()
	menu.add_icon_item(icons.Remove, tr("Remove from Library"), ACTION_REMOVE_ANIM)
	
	menu.id_pressed.connect(_perform_action.bind(anim_name), CONNECT_DEFERRED)
	
	menu.position = global_position
	
	menu.popup()

func _on_inherit_selected(index: int) -> void:
	if !_current_animation_name_in_detail: return
	var inherit_anim_name := _detail_inherit_button.get_item_metadata(index)
	_library.set_animation_inheritance(_current_animation_name_in_detail, inherit_anim_name, true)
	
	_edit_animation(_current_animation_name_in_detail)

func _add_animation_context_menu_actions(menu: PopupMenu, anim_name: StringName) -> void:
	menu.add_icon_item(icons.Import, tr("Reimport"), ACTION_REIMPORT)
	menu.set_item_disabled(menu.item_count-1, _library.get_inheriting_from_animation(_library.get_animation(anim_name)) == &"")

func _add_resource_context_menu_actions(menu: PopupMenu) -> void:
	menu.add_icon_item(icons.Edit, tr("Edit"), ACTION_EDIT_RESOURCE)
	menu.add_icon_item(icons.Duplicate, tr("Make Unique"), ACTION_MAKE_UNIQUE)
	menu.add_separator()
	menu.add_icon_item(icons.ShowInFileSystem, tr("Show in FileSystem"), ACTION_SHOW_IN_FILESYSTEM)

func _perform_action(id: int, anim_name: StringName) -> void:
	if !anim_name: return
	var library := _library
	var anim := library.get_animation(anim_name) if anim_name else null
	if !anim: return
	
	var undo_redo := EditorInterface.get_editor_undo_redo()
	
	match id:
		ACTION_EDIT_RESOURCE:
			EditorInterface.edit_resource.call_deferred(anim)
			_edit_animation(anim_name)
		ACTION_REMOVE_ANIM:
			undo_redo.create_action("Remove Animation from Library", UndoRedo.MERGE_DISABLE, library, false, true)
			undo_redo.add_do_method(library, &"remove_animation", anim_name)
			undo_redo.add_undo_method(library, &"add_animation", anim_name, anim)
			undo_redo.commit_action()
		ACTION_MAKE_UNIQUE:
			undo_redo.create_action("Make Animation Unique: " + anim_name, UndoRedo.MERGE_DISABLE, library, false, true)
			var copy = anim.duplicate()
			undo_redo.add_do_method(library, &"remove_animation", anim_name)
			undo_redo.add_do_method(library, &"add_animation", anim_name, copy)
			undo_redo.add_undo_method(library, &"remove_animation", anim_name)
			undo_redo.add_undo_method(library, &"add_animation", anim_name, anim)
			undo_redo.commit_action()
		ACTION_SHOW_IN_FILESYSTEM:
			EditorInterface.select_file(anim.resource_path.get_slice("::",0))
			EditorInterface.get_file_system_dock().make_visible()
		ACTION_REIMPORT:
			library.reimport_animation(anim_name)

func _edit_animation(anim_name: StringName) -> void:
	if !anim_name: return
	var library := _library
	var anim := library.get_animation(anim_name) if anim_name else null
	if !anim: return
	
	var editor = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_player_editor.gd")
	var mixer: AnimationMixer = editor.current_animation_mixer
	if mixer:
		for library_name in mixer.get_animation_library_list():
			if mixer.get_animation_library(library_name) == library:
				var full_anim_name = library_name + "/" + anim_name if library_name else anim_name
				editor.make_visible()
				editor.set_deferred(&"current_animation_name", full_anim_name)
				return

func _show_detail(anim_name: StringName) -> void:
	var library := _library
	var anim := library.get_animation(anim_name) if anim_name else null

	_current_animation_name_in_detail = anim_name
	if _current_animation_in_detail:
		_current_animation_in_detail.changed.disconnect(_refresh_detail_deferred)
	_current_animation_in_detail = anim
	if _current_animation_in_detail:
		_current_animation_in_detail.changed.connect(_refresh_detail_deferred)
	
	if !anim:
		_detail_main_container.visible = false
		_detail_unselected_container.visible = true
		return
	
	_detail_main_container.visible = true
	_detail_unselected_container.visible = false
	_detail_name_label.text = anim_name
	
	var storage_display := _get_item_storage_display(anim)
	_detail_storage_button.text = storage_display.label
	_detail_storage_button.icon = storage_display.icon
	_detail_storage_button.tooltip_text = anim.resource_path
	
	var inheriting_from_animation: StringName = library.get_inheriting_from_animation(anim)
	for item_index in range(_detail_inherit_button.item_count):
		if _detail_inherit_button.get_item_metadata(item_index) == inheriting_from_animation:
			_detail_inherit_button.select(item_index)
	
	_detail_reimport_button.disabled = inheriting_from_animation.is_empty()

func _refresh_detail_deferred() -> void:
	if _detail_refresh_queued: return
	_detail_refresh_queued = true
	set_deferred(&"_detail_refresh_queued", false)
	_refresh_detail.call_deferred()

func _refresh_detail() -> void:
	_show_detail(_current_animation_name_in_detail)