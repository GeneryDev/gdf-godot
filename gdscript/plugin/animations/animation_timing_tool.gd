@tool
@icon("res://addons/gdf-godot/assets/icons/tool_wrench")
extends Resource

const AnimationTransactionAPI : Script = preload(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_transaction_api.gd")
const Transactions := AnimationTransactionAPI.Transactions
const KeyState := AnimationTransactionAPI.KeyState
const TrackState := AnimationTransactionAPI.TrackState

const META_NAME_RETIMING := &"retiming"
const META_NAME_RETIMING_DATA := &"_retiming_data"
const TIME_TOLERANCE := 0.001

@export var edit_control_points: Script:
	get:
		return self.get_script()
	set(value):
		return

var _anim: Animation
var _mixer: AnimationMixer
var _anim_name: StringName

var _transactions: Transactions

func _init():
	resource_name = "Animation Timing Tool"

### CONTEXT METHODS

func set_context(mixer: AnimationMixer, anim: Animation, anim_name: StringName):
	_mixer = mixer
	_anim = anim
	_anim_name = anim_name
	return self

func set_context_from_editor():
	var context := get_context_from_editor()
	return set_context(context.mixer, context.anim, context.anim_name)

static func get_context_from_editor() -> Dictionary:
	var animation_player_editor = load(GdfEditorPlugin.PLUGIN_ROOT + "/gdscript/plugin/animations/animation_player_editor.gd")
	var mixer: AnimationMixer = animation_player_editor.current_animation_mixer
	var anim: Animation = null
	var anim_name = animation_player_editor.current_animation_name
	if mixer && anim_name:
		anim = mixer.get_animation(anim_name)
	return {
		"mixer": mixer,
		"anim": anim,
		"anim_name": anim_name
	}

func _find_edit_track_index() -> int:
	if !_anim: return -1
	for track_idx in range(_anim.get_track_count()):
		var path := _anim.track_get_path(track_idx)
		var prop_name : StringName = path.get_subname(path.get_subname_count() - 1)
		
		match prop_name:
			&"edit_control_points":
				return track_idx
	
	return -1

### GETTERS

func has_timing_tracks() -> bool:
	return _find_edit_track_index() != -1

func has_timing_data() -> bool:
	if !_anim: return false
	return _anim.has_meta(META_NAME_RETIMING_DATA) && _anim.get_meta(META_NAME_RETIMING_DATA) is Dictionary

func get_timing_data() -> Dictionary:
	if !has_timing_data(): return {}

	return _anim.get_meta(META_NAME_RETIMING_DATA) as Dictionary

func is_linked() -> bool:
	if !has_timing_data(): return false
	return get_timing_data().linked

func has_control_points() -> bool:
	if !has_timing_data(): return false
	var timing_data := get_timing_data()
	
	if !timing_data.original_control_points:
		return false
	
	return timing_data.original_control_points.size()

func get_control_point_count() -> int:
	if !has_timing_data(): return 0
	var timing_data := get_timing_data()
	return timing_data.original_control_points.size()

func has_applied_timing_changes() -> bool:
	if !has_timing_data(): return false
	if !has_control_points(): return false
	var timing_data := get_timing_data()

	var original_control_points : Array[float] = timing_data.original_control_points
	var current_control_points : Array[float] = timing_data.current_control_points
	var control_point_count := original_control_points.size()

	if original_control_points.size() != current_control_points.size():
		# Mismatching control point counts. Likely user error if not a bug. Assuming there has been a change.
		return true
	
	for key_index in range(control_point_count):
		if abs(original_control_points[key_index] - current_control_points[key_index]) > TIME_TOLERANCE:
			# Mismatching key between original and current; timing changes have definitely been applied
			return true
	
	# Original and current tracks match. Likely no timing changes have been applied, assuming no external changes or user error.
	return false

### ACTIONS

func prompt_initialize() -> void:
	if has_applied_timing_changes():
		var prompt := AcceptDialog.new()
		prompt.theme = EditorInterface.get_editor_theme()
		prompt.title = "Reinitialize Timing?"
		prompt.ok_button_text = "Revert and Reinitialize"
		prompt.add_button("Reinitialize as-is", true, "just_reinitialize")
		prompt.add_button("Cancel", true, "close")

		prompt.dialog_text = ("This animation (%s) already contains timing tracks, indicating that it has been retimed.\n" + \
				"You can either:\n" + \
				"Revert and Reinitialize: Revert all prior timing changes to the animation\n" + \
				"and redo the timing from its original state\n" + \
				"Reinitialize as-is: Keep the current animation timing\n" +
				"and redo the timing from the current state\n" + \
				"\n" + \
				"(Note that if this is an imported animation, reinitializing as-is\n" + \
				"and reimporting the animation may result in broken timings)") % [_anim_name]
		
		var body_label: Label = prompt.find_children("*","Label",false,false)[0]
		body_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER

		prompt.confirmed.connect(func ():
			revert()
			unlink()
			initialize()
		)
		prompt.custom_action.connect(func (action):
			prompt.hide()
			prompt.queue_free()
			if action == "just_reinitialize":
				initialize()
		)
		EditorInterface.popup_dialog_centered(prompt)
	else:
		initialize()

func prompt_unlink() -> void:
	if has_applied_timing_changes():
		var prompt := AcceptDialog.new()
		prompt.theme = EditorInterface.get_editor_theme()
		prompt.title = "Unlink"
		prompt.ok_button_text = "Revert and Unlink"
		prompt.add_button("Unlink as-is", true, "just_unlink")
		prompt.add_button("Cancel", true, "close")

		prompt.dialog_text = ("This animation (%s) has already been retimed. Unlinking will allow you to add,\n" +
				"remove or move control points freely.\n" + \
				"You can either:\n" + \
				"Revert and Unlink: Revert all prior timing changes to the animation\n" + \
				"and manually redo the timing from its original state\n" + \
				"Unlink as-is: Keep the current animation timing\n" +
				"\n" + \
				"(Note that if this is an imported animation, unlinking as-is\n" + \
				"and reimporting the animation may result in broken timings)") % [_anim_name]

		var body_label: Label = prompt.find_children("*","Label",false,false)[0]
		body_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER

		prompt.confirmed.connect(func ():
			revert()
			unlink()
		)
		prompt.custom_action.connect(func (action):
			prompt.hide()
			prompt.queue_free()
			if action == "just_unlink":
				unlink()
		)
		EditorInterface.popup_dialog_centered(prompt)
	else:
		unlink()

func initialize() -> void:
	if !_anim:
		_error(ErrorReason.INCOMPLETE_CONTEXT)
		return

	_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), _anim, "Animation Timing Tool: Initialize")

	var edit_track_index := _initialize_edit_track()

	_array_to_track_keys([0, _anim.length], edit_track_index)

	# Set timing data
	var new_timing_data := {
		"linked": false,
		"original_control_points": [] as Array[float],
		"current_control_points": [] as Array[float],
		"original_animation_length": -1,
		"current_animation_length": -1
	}
	_transactions.set_anim_metadata(META_NAME_RETIMING_DATA, new_timing_data)

func _get_or_add_edit_track() -> int:
	var edit_track_index := _find_edit_track_index()
	if edit_track_index != -1:
		return edit_track_index
	else:
		return _initialize_edit_track()

func _initialize_edit_track() -> int:
	var edit_track_index := _find_edit_track_index()

	if edit_track_index < 0:
		# Edit track is missing. Create one
		var new_index := _anim.get_track_count()
		_transactions.add_track(Animation.TrackType.TYPE_VALUE, new_index, ^".:metadata/retiming:edit_control_points")
		edit_track_index = new_index

	_transactions.update_track(edit_track_index, TrackState.blank(false, true))
	_transactions.clear_track(edit_track_index)
	
	if _mixer:
		_transaction_initialize_mixer_metadata(_mixer, self.get_script())
	
	return edit_track_index

func _transaction_initialize_mixer_metadata(mixer: AnimationMixer, tool_script: Script) -> void:
	if !mixer: return
	var anim_root_node := mixer.get_node(mixer.root_node)
	var meta_name := META_NAME_RETIMING

	if !anim_root_node.has_meta(meta_name) || anim_root_node.get_meta(meta_name) == null:
		var new_tool : Resource = tool_script.new()
		var undo_redo := EditorInterface.get_editor_undo_redo()
		undo_redo.create_action("Setup AnimationMixer for Timing Tool", UndoRedo.MERGE_ALL, anim_root_node)
		undo_redo.add_do_method(anim_root_node, &"set_meta", meta_name, new_tool)
		if !anim_root_node.has_meta(meta_name):
			undo_redo.add_undo_method(anim_root_node, &"remove_meta", meta_name)
		else:
			undo_redo.add_undo_method(anim_root_node, &"set_meta", meta_name, null)
		undo_redo.commit_action()

func uninitialize() -> void:
	if !_anim:
		_error(ErrorReason.INCOMPLETE_CONTEXT)
		return
	if !has_timing_data() && !has_timing_tracks():
		return

	_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), _anim, "Animation Timing Tool: Uninitialize")
	
	_transactions.remove_track(_find_edit_track_index())
	_transactions.set_anim_metadata(META_NAME_RETIMING_DATA, null)

func revert() -> void:
	if !_anim:
		_error(ErrorReason.INCOMPLETE_CONTEXT)
		return
	if !has_timing_data():
		_error(ErrorReason.NOT_INITIALIZED)
		return
	var timing_data := get_timing_data()
	if !is_linked(): return

	_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), _anim, "Animation Timing Tool: Revert")

	var edit_track_index := _get_or_add_edit_track()
	
	var original_control_points : Array[float] = timing_data.original_control_points

	# Copy all control point keyframes from the original to the "desired" control point track
	_array_to_track_keys(original_control_points, edit_track_index)

	apply_timing_change(false)

func unlink() -> void:
	if !_anim:
		_error(ErrorReason.INCOMPLETE_CONTEXT)
		return
	if !has_timing_data():
		_error(ErrorReason.NOT_INITIALIZED)
		return
	if !is_linked(): return
	
	_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), _anim, "Animation Timing Tool: Unlink")

	var edit_track_index := _find_edit_track_index()
	if edit_track_index == -1:
		_error(ErrorReason.MISSING_TIMING_TRACK)
		return
	
	var new_timing_data := {
		"linked": false,
		"original_control_points": [] as Array[float],
		"current_control_points": [] as Array[float],
		"original_animation_length": -1,
		"current_animation_length": -1
	}
	var key_times := _track_keys_to_array(edit_track_index)
	new_timing_data.original_control_points = key_times.size()
	new_timing_data.original_animation_length = _anim.length

	_transactions.set_anim_metadata(META_NAME_RETIMING_DATA, new_timing_data)

func link(start_new_transaction: bool = true) -> void:
	if !_anim:
		_error(ErrorReason.INCOMPLETE_CONTEXT)
		return
	if !has_timing_data():
		_error(ErrorReason.NOT_INITIALIZED)
		return
	var timing_data := get_timing_data()
	if is_linked():
		return

	if start_new_transaction:
		_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), _anim, "Animation Timing Tool: Link")

	var edit_track_index := _find_edit_track_index()
	if edit_track_index == -1:
		_error(ErrorReason.MISSING_TIMING_TRACK)
		return
	
	var control_point_count := _anim.track_get_key_count(edit_track_index)
	if control_point_count < 2:
		_error(ErrorReason.INVALID_KEY_COUNT, "Not enough control points! At least 2 control points are needed")
		return

	var new_timing_data := {
		"linked": true,
		"original_control_points": [] as Array[float],
		"current_control_points": [] as Array[float],
		"original_animation_length": -1,
		"current_animation_length": -1
	}
	
	# Copy all control point keyframes from the original to both "current" and "desired" control point tracks
	var key_times := _track_keys_to_array(edit_track_index)
	new_timing_data.original_control_points = key_times.duplicate()
	new_timing_data.current_control_points = key_times.duplicate()
	new_timing_data.original_animation_length = _anim.length
	new_timing_data.current_animation_length = _anim.length

	_transactions.set_anim_metadata(META_NAME_RETIMING_DATA, new_timing_data)

func apply_timing_change(start_new_transaction: bool = true) -> void:
	if !_anim:
		_error(ErrorReason.INCOMPLETE_CONTEXT)
		return
	if !has_timing_data():
		_error(ErrorReason.NOT_INITIALIZED)
		return
	if !is_linked():
		_error(ErrorReason.INVALID_LINK_STATE, "Cannot apply timing changes; timing track must be linked.")
		return

	if start_new_transaction:
		_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), _anim, "Animation Timing Tool: Apply Timing Change")
	
	var reimported := false
	
	if !has_timing_tracks():
		# Missing timing tracks! Assuming reimported
		reimported = true
		_restore_timing_after_reimport()

	var edit_track_index := _find_edit_track_index()
	if edit_track_index == -1:
		_error(ErrorReason.MISSING_TIMING_TRACK)
		return

	var timing_data := get_timing_data()
	var control_point_count := get_control_point_count()

	var original_control_points : Array[float] = timing_data.original_control_points
	var current_control_points : Array[float] = timing_data.current_control_points
	
	if _anim.track_get_key_count(edit_track_index) != control_point_count:
		_error(ErrorReason.INVALID_KEY_COUNT, "The control points track must have exactly %s keyframes! If you need a different number of control points, press Initialize Tracks and redesign the original control point track" % [control_point_count])
		return
	
	if current_control_points.size() != control_point_count:
		_error(ErrorReason.OTHER, "Mismatching array sizes between current_control_points and original_control_points. Likely a bug. Cannot apply timing changes")
		return

	var old_times: Array[float] = current_control_points.duplicate()
	var new_times: Array[float] = _track_keys_to_array(edit_track_index)
	old_times.resize(control_point_count)
	new_times.resize(control_point_count)
	
	var any_timing_changes := old_times != new_times
	
	if !any_timing_changes:
		return
	
	for track_index in range(_anim.get_track_count()):
		if track_index == edit_track_index:
			# Skip over Control Points track
			continue
		if reimported && !_anim.track_is_imported(track_index):
			# If the animation was re-imported, only re-time tracks that were imported.
			continue
		
		_transactions.retime_track(track_index, old_times, new_times)
	
	_transactions.retime_length(old_times, new_times)

	var new_timing_data := timing_data.duplicate(true)
	new_timing_data.current_control_points = new_times.duplicate()
	new_timing_data.current_animation_length = _anim.length
	_transactions.set_anim_metadata(META_NAME_RETIMING_DATA, new_timing_data)

func _restore_timing_after_reimport() -> void:
	if !has_timing_data():
		_error(ErrorReason.NOT_INITIALIZED)
		return
	var edit_track_index := _initialize_edit_track()
	var timing_data := get_timing_data()

	var current_control_points : Array[float] = timing_data.current_control_points
	var control_point_count := current_control_points.size()
	
	# Copy all control point keyframes from the original to the "current" track

	_array_to_track_keys(current_control_points, edit_track_index)
	var new_timing_data := timing_data.duplicate(true)
	new_timing_data.current_control_points = new_timing_data.original_control_points
	new_timing_data.current_animation_length = new_timing_data.original_animation_length
	_transactions.set_anim_metadata(META_NAME_RETIMING_DATA, new_timing_data)

func retime_reimported_animations() -> void:
	if !_mixer:
		_error(ErrorReason.INCOMPLETE_CONTEXT)
		return

	var retimed_count := 0
	for entry in _get_all_animations(_mixer):
		var anim: Animation = entry.anim
		var anim_name: StringName = entry.anim_name
		set_context(_mixer, anim, anim_name)
		if !has_timing_data(): continue
		if !is_linked(): continue

		_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), _anim, "Animation Timing Tool: Retime Animation '%s'" % [anim_name])
		apply_timing_change(false)
		retimed_count += 1
	_info("Retimed %s animations" % [retimed_count])

func force_fix_track_imported() -> void:
	if !_anim:
		_error(ErrorReason.INCOMPLETE_CONTEXT)
		return
	if !has_timing_tracks():
		return
	# Forces the timing edit track to be imported, without undo-redo,
	# to workaround a Godot bug where removing an imported track via the animation editor, and then undoing,
	# would "forget" the imported state of the track and wrongly initialize it as false.
	var edit_track_index := _find_edit_track_index()
	if edit_track_index != -1 && !_anim.track_is_imported(edit_track_index):
		# Bug was triggered
		_anim.track_set_imported(edit_track_index, true)
		_anim.emit_changed()

### Private Utility

func _get_all_animations(mixer: AnimationMixer) -> Array[Dictionary]:
	var output: Array[Dictionary] = []
	for library_name: StringName in mixer.get_animation_library_list():
		var library := mixer.get_animation_library(library_name)
		for anim_name: StringName in library.get_animation_list():
			var anim := library.get_animation(anim_name)
			var full_anim_name := StringName("%s/%s" % [library_name, anim_name]) if library_name else anim_name
			output.append({
				"anim": anim,
				"anim_name": full_anim_name
			})
	return output

func _track_keys_to_array(track_index: int) -> Array[float]:
	var times : Array[float] = []
	for key_index in range(_anim.track_get_key_count(track_index)):
		times.append(_anim.track_get_key_time(track_index, key_index))
	return times

func _array_to_track_keys(times: Array[float], track_index: int) -> void:
	_transactions.clear_track(track_index)
	for key_index in range(times.size()):
		var state = KeyState.time_value_pair(times[key_index], self.get_script())
		_transactions.insert_key(track_index, state)

func _error(reason: ErrorReason, message: String = "") -> void:
	if !message:
		match reason:
			ErrorReason.INCOMPLETE_CONTEXT:
				message = "No animation has been assigned to this Animation Timing Tool"
			ErrorReason.NOT_INITIALIZED:
				message = "The Animation Timing Tool hasn't been initialized for this animation; i.e. there is no timing track for it"
			ErrorReason.INVALID_LINK_STATE:
				message = "Invalid link state"
			ErrorReason.MISSING_TIMING_TRACK:
				message = "No timing control point track found in current animation"
			ErrorReason.INVALID_KEY_COUNT:
				message = "Invalid key count"
			ErrorReason.OTHER:
				pass
	EditorInterface.get_editor_toaster().push_toast(message, EditorToaster.SEVERITY_ERROR)
	printerr("[Animation Timing Tool | %s] %s (In animation: %s)" % [ErrorReason.find_key(reason), message, _anim_name])

func _info(message: String = "") -> void:
	EditorInterface.get_editor_toaster().push_toast(message, EditorToaster.SEVERITY_INFO)
	print("[Animation Timing Tool | INFO] %s" % [message])

### Engine callbacks

func _validate_property(property: Dictionary) -> void:
	var property_name : StringName = property.name
	if [&"original_control_points", &"current_control_points", &"desired_control_points"].has(property_name):
		property.usage &= ~PROPERTY_USAGE_STORAGE
		property.usage |= PROPERTY_USAGE_READ_ONLY
	if [&"resource_name", &"resource_path", &"resource_local_to_scene"].has(property_name):
		property.usage &= ~PROPERTY_USAGE_EDITOR

func _hide_script_from_inspector() -> bool:
	return true

func _hide_metadata_from_inspector() -> bool:
	return true

enum ErrorReason {
	INCOMPLETE_CONTEXT,
	NOT_INITIALIZED,
	INVALID_LINK_STATE,
	MISSING_TIMING_TRACK,
	INVALID_KEY_COUNT,
	OTHER
}
