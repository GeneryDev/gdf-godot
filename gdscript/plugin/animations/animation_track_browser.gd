extends AcceptDialog

const COLUMN_INDEX := 0
const COLUMN_PATH := 1
const COLUMN_ENABLED := 2
const COLUMN_IMPORTED := 3
const COLUMN_COMPRESSED := 4
const MAX_COLUMN := 4

var animation : Animation:
	get:
		return animation
	set(value):
		if animation:
			animation.changed.disconnect(_on_animation_changed)
		animation = value
		if animation:
			animation.changed.connect(_on_animation_changed)

var _search_bar: LineEdit
var _track_tree: Tree

func _init():
	title = "Animation Track Browser"
	borderless = false
	size = Vector2i(720, 400)
	theme = theme
	transient = true
	exclusive = false
	ok_button_text = tr("Close")

	close_requested.connect(queue_free)

	var vbox := VBoxContainer.new()
	add_child(vbox)

	var content := VBoxContainer.new()
	content.size_flags_vertical = Control.SIZE_EXPAND_FILL
	vbox.add_child(content)
	var search_bar := LineEdit.new()
	_search_bar = search_bar
	search_bar.placeholder_text = "Search"
	content.add_child(search_bar)
	var tree := Tree.new()
	_track_tree = tree
	tree.size_flags_vertical = Control.SIZE_EXPAND_FILL
	content.add_child(tree)

	_search_bar.text_changed.connect(_populate_tree)
	_track_tree.item_edited.connect(_on_tree_item_edited)
	about_to_popup.connect(_on_about_to_popup)

func _on_about_to_popup() -> void:
	_search_bar.clear()
	_populate_tree()

func _ready() -> void:
	_search_bar.grab_focus()

func _populate_tree(filter: String = "") -> void:
	var anim := animation
	if !anim: return
	var tree := _track_tree
	var selected_index := tree.get_selected().get_metadata(0) as int if tree.get_selected() else -1

	tree.clear()
	tree.columns = MAX_COLUMN+1
	tree.hide_root = true
	tree.hide_folding = true
	tree.column_titles_visible = true
	tree.set_column_title(COLUMN_INDEX, "Index")
	tree.set_column_title(COLUMN_PATH, "Path")
	tree.set_column_title(COLUMN_ENABLED, "Enabled")
	tree.set_column_title(COLUMN_IMPORTED, "Imported")
	tree.set_column_title(COLUMN_COMPRESSED, "Compressed")
	tree.set_column_expand(COLUMN_INDEX, false)
	tree.set_column_expand(COLUMN_ENABLED, false)
	tree.set_column_expand(COLUMN_IMPORTED, false)
	tree.set_column_expand(COLUMN_COMPRESSED, false)
	tree.select_mode = Tree.SELECT_ROW

	var root := tree.create_item()

	for track_index in range(anim.get_track_count()):
		var path := anim.track_get_path(track_index)

		if filter && !(path as String).containsn(filter):
			continue

		var item := tree.create_item(root)
		item.set_text(COLUMN_INDEX, str(track_index))
		item.set_text_alignment(COLUMN_INDEX, HORIZONTAL_ALIGNMENT_RIGHT)
		item.set_icon(COLUMN_PATH, _get_icon_for_track_type(anim.track_get_type(track_index)))
		item.set_text(COLUMN_PATH, path)
		item.set_metadata(COLUMN_INDEX, track_index)
		item.set_metadata(COLUMN_PATH, path)

		item.set_cell_mode(COLUMN_ENABLED, TreeItem.CELL_MODE_CHECK)
		item.set_editable(COLUMN_ENABLED, true)
		item.set_cell_mode(COLUMN_IMPORTED, TreeItem.CELL_MODE_CHECK)
		item.set_editable(COLUMN_IMPORTED, true)

		_refresh_item(item)

		if selected_index == track_index:
			item.select(0)

func _refresh_item(item: TreeItem) -> void:
	var anim := animation
	if !anim: return
	var track_index: int = item.get_metadata(COLUMN_INDEX)

	item.set_checked(COLUMN_ENABLED, anim.track_is_enabled(track_index))
	item.set_checked(COLUMN_IMPORTED, anim.track_is_imported(track_index))
	item.set_text(COLUMN_COMPRESSED, "Yes" if anim.track_is_compressed(track_index) else "No")

func _refresh_tree() -> void:
	for item in _track_tree.get_root().get_children():
		_refresh_item(item)

func _on_animation_changed() -> void:
	_populate_tree(_search_bar.text)

func _on_tree_item_edited() -> void:
	var tree := _track_tree
	var edited_item := tree.get_edited()
	var edited_column := tree.get_edited_column()

	match edited_column:
		COLUMN_ENABLED:
			_edit_track_enabled.call_deferred(edited_item)
		COLUMN_IMPORTED:
			_edit_track_imported.call_deferred(edited_item)

func _edit_track_enabled(edited_item: TreeItem) -> void:
	var anim := animation
	if !anim: return
	var track_index: int = edited_item.get_metadata(COLUMN_INDEX)
	if track_index < 0 or track_index >= anim.get_track_count(): return
	
	var undo_redo := EditorInterface.get_editor_undo_redo()

	var old_value := anim.track_is_enabled(track_index)
	var new_value := edited_item.is_checked(COLUMN_ENABLED)
	if old_value == new_value: return
	undo_redo.create_action("Toggle Track Enabled", UndoRedo.MERGE_ALL, anim, false)
	undo_redo.add_do_method(anim, &"track_set_enabled", track_index, new_value)
	undo_redo.add_undo_method(anim, &"track_set_enabled", track_index, old_value)
	undo_redo.commit_action()

func _edit_track_imported(edited_item: TreeItem) -> void:
	var anim := animation
	if !anim: return
	var track_index: int = edited_item.get_metadata(COLUMN_INDEX)
	if track_index < 0 or track_index >= anim.get_track_count(): return
	
	var undo_redo := EditorInterface.get_editor_undo_redo()

	var old_value := anim.track_is_imported(track_index)
	var new_value := edited_item.is_checked(COLUMN_IMPORTED)
	if old_value == new_value: return
	undo_redo.create_action("Toggle Track Imported", UndoRedo.MERGE_ALL, anim, false)
	undo_redo.add_do_method(anim, &"track_set_imported", track_index, new_value)
	undo_redo.add_undo_method(anim, &"track_set_imported", track_index, old_value)
	undo_redo.commit_action()

func _get_icon_for_track_type(track_type: Animation.TrackType):
	var theme := EditorInterface.get_editor_theme()
	var icon_name: StringName = &"KeyValue"
	match track_type:
		Animation.TrackType.TYPE_VALUE:
			icon_name = &"KeyValue"
		Animation.TrackType.TYPE_POSITION_3D:
			icon_name = &"KeyTrackPosition"
		Animation.TrackType.TYPE_ROTATION_3D:
			icon_name = &"KeyTrackRotation"
		Animation.TrackType.TYPE_SCALE_3D:
			icon_name = &"KeyTrackScale"
		Animation.TrackType.TYPE_BLEND_SHAPE:
			icon_name = &"KeyTrackBlendShape"
		Animation.TrackType.TYPE_METHOD:
			icon_name = &"KeyCall"
		Animation.TrackType.TYPE_BEZIER:
			icon_name = &"KeyBezier"
		Animation.TrackType.TYPE_AUDIO:
			icon_name = &"KeyAudio"
		Animation.TrackType.TYPE_ANIMATION:
			icon_name = &"KeyAnimation"
	return theme.get_icon(icon_name, &"EditorIcons")
