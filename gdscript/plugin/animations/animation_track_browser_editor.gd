@tool
extends RefCounted

const ID_SEPARATOR := 2604
const ACTION_SHOW_TRACK_BROWSER := 2605

var _editor_controls: EditorControlCache = EditorControlCache.new()

var _current_animation : Animation

func enable() -> void:
	_editor_controls.fetch()
	_add_menu_items()

func disable() -> void:
	_remove_menu_items()

func _add_menu_items() -> void:
	var editor_theme := EditorInterface.get_editor_theme()
	_editor_controls.edit_menu_popup.add_separator("", ID_SEPARATOR)
	_editor_controls.edit_menu_popup.add_icon_item(editor_theme.get_icon(&"AnimationTrackList", &"EditorIcons"), "Track Browser...", ACTION_SHOW_TRACK_BROWSER)
	_editor_controls.edit_menu_popup.id_pressed.connect(_on_menu_item_pressed)

func _remove_menu_items() -> void:
	_editor_controls.edit_menu_popup.remove_item(_editor_controls.edit_menu_popup.get_item_index(ID_SEPARATOR))
	_editor_controls.edit_menu_popup.remove_item(_editor_controls.edit_menu_popup.get_item_index(ACTION_SHOW_TRACK_BROWSER))
	_editor_controls.edit_menu_popup.id_pressed.disconnect(_on_menu_item_pressed)

func _on_menu_item_pressed(id: int) -> void:
	match id:
		ACTION_SHOW_TRACK_BROWSER:
			var context := _get_context_from_editor()
			if context:
				_show_track_browser(context.anim, context.anim_name)
			else:
				EditorInterface.get_editor_toaster().push_toast("No animation selected", EditorToaster.SEVERITY_ERROR)

func _get_context_from_editor() -> Dictionary:
	var animation_player_editor = _editor_controls.editor
	var mixer: AnimationMixer = animation_player_editor.current_animation_mixer
	var anim: Animation = null
	var anim_name = animation_player_editor.current_animation_name
	if mixer && anim_name:
		anim = mixer.get_animation(anim_name)
	if !anim: return {}
	return {
		"mixer": mixer,
		"anim": anim,
		"anim_name": anim_name
	}

func _show_track_browser(anim: Animation, anim_name: StringName) -> void:
	var track_browser = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_track_browser.gd").new()
	track_browser.animation = anim
	track_browser.title = "Animation Track Browser (%s)" % anim_name
	
	EditorInterface.popup_dialog_centered(track_browser)

class EditorControlCache extends RefCounted:
	var editor

	var editor_control: Control
	var main_vbox: VBoxContainer
	var top_hbox: HBoxContainer
	var edit_menu_button: MenuButton
	var edit_menu_popup: PopupMenu

	func fetch() -> void:
		editor = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_player_editor.gd")

		editor_control = editor.animation_editor_control
		# editor_control.print_tree_pretty()

		main_vbox = editor_control.find_children("*","VBoxContainer", true, false)[0]
		top_hbox = main_vbox.find_children("*","HBoxContainer", true, false)[0]
		edit_menu_button = top_hbox.find_children("*","MenuButton", true, false)[1]
		edit_menu_popup = edit_menu_button.get_popup()
