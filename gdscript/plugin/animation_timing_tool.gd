@tool
@icon("res://addons/gdf-godot/assets/icons/tool_wrench")
extends Node

@export var original_control_points: String
@export var current_control_points: String
@export var desired_control_points: String

@export_tool_button("Reset")
var _button_reset := reset
@export_tool_button("Original Control Points Done")
var _button_original_control_points_done := original_control_points_done
@export_tool_button("Update Timing")
var _button_update_timing := update_timing

var _transactions: Transactions
var _undo_redo: EditorUndoRedoManager

func _get_animation() -> Dictionary:
	var animation_player_editor = load("res://addons/gdf-godot/gdscript/plugin/animation_player_editor.gd")
	var mixer: AnimationMixer = animation_player_editor.editing_animation_mixer
	var anim: Animation = null
	var anim_name = animation_player_editor.current_animation_name
	if mixer && anim_name:
		anim = mixer.get_animation(anim_name)
	if !mixer || !anim:
		return {}
	return {
		"mixer": mixer,
		"anim": anim
	}

func _find_track_indices(anim: Animation, anim_root_node: Node, all_required: bool = false) -> Dictionary:
	var track_index_a := -1
	var track_index_b := -1
	var track_index_c := -1
	
	for track_idx in range(anim.get_track_count()):
		var path := anim.track_get_path(track_idx)
		var animated_node := anim_root_node.get_node(path)
		if animated_node != self: continue
		var prop_name : StringName = path.get_subname(path.get_subname_count() - 1)
		
		match prop_name:
			&"original_control_points":
				track_index_a = track_idx
			&"current_control_points":
				track_index_b = track_idx
			&"desired_control_points":
				track_index_c = track_idx
	
	if all_required && (track_index_a == -1 || track_index_b == -1 || track_index_c == -1):
		return {}
	return {
		"a" = track_index_a,
		"b" = track_index_b,
		"c" = track_index_c
	}

func reset() -> void:
	var working_objects := _get_animation()
	if !working_objects: return
	var mixer: AnimationMixer = working_objects.mixer
	var anim: Animation = working_objects.anim
	
	_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), anim, "Animation Timing Tool: Reset")
	
	var anim_root_node := mixer.get_node(mixer.root_node)
	var track_indices := _find_track_indices(anim, anim_root_node)
	
	var path_to_this_node := anim_root_node.get_path_to(self)
	
	# Add any missing tracks
	_transactions.initialize_track(&"original_control_points", anim, track_indices.a, path_to_this_node, TrackState.blank(true))
	_transactions.initialize_track(&"desired_control_points", anim, track_indices.c, path_to_this_node, TrackState.blank(false))
	_transactions.initialize_track(&"current_control_points", anim, track_indices.b, path_to_this_node, TrackState.blank(false, true))

	# Find tracks again
	track_indices = _find_track_indices(anim, anim_root_node)

	_transactions.insert_key(track_indices.a, KeyState.time_value_pair(0, "Start"))
	_transactions.insert_key(track_indices.a, KeyState.time_value_pair(anim.length, "End"))

func original_control_points_done() -> void:
	var working_objects := _get_animation()
	if !working_objects: return
	var mixer: AnimationMixer = working_objects.mixer
	var anim: Animation = working_objects.anim

	_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), anim, "Animation Timing Tool: Original Control Points Done")

	var anim_root_node := mixer.get_node(mixer.root_node)
	var track_indices := _find_track_indices(anim, anim_root_node, true)
	if !track_indices:
		printerr("One or more control point tracks not found in current animation")
		return
	
	var control_point_count := anim.track_get_key_count(track_indices.a)
	if control_point_count < 2:
		printerr("Not enough control points in the 'original_control_points' track! At least 2 control points are needed")
		return

	if anim.track_get_key_count(track_indices.b) > 0:
		printerr("The 'current_control_points' track must be empty!")
		return
	
	if anim.track_get_key_count(track_indices.c) > 0:
		printerr("The 'desired_control_points' track must be empty!")
		return
	
	# Copy all control point keyframes from the original to both "current" and "desired" control point tracks
	for key_index in range(control_point_count):
		var state := KeyState.create_from(anim, track_indices.a, key_index)

		_transactions.insert_key(track_indices.b, state)
		_transactions.insert_key(track_indices.c, state)
	
	# Update checkboxes
	_transactions.update_track(track_indices.a, TrackState.blank(false))
	_transactions.update_track(track_indices.b, TrackState.blank(false, true))
	_transactions.update_track(track_indices.c, TrackState.blank(true))

func update_timing() -> void:
	var working_objects := _get_animation()
	if !working_objects: return
	var mixer: AnimationMixer = working_objects.mixer
	var anim: Animation = working_objects.anim
	
	_update_timing(anim, mixer, true)

func _update_timing(anim: Animation, mixer: AnimationMixer, error_if_missing_tracks: bool = false) -> void:
	if !anim: return

	_transactions = Transactions.new(EditorInterface.get_editor_undo_redo(), anim, "Animation Timing Tool: Update Animation Timing")

	var anim_root_node := mixer.get_node(mixer.root_node)
	var track_indices := _find_track_indices(anim, anim_root_node)
	if track_indices.a == -1 || track_indices.c == -1:
		if error_if_missing_tracks:
			printerr("One or more control point tracks not found in current animation. In animation: '%s'" % [anim.resource_name])
		return
	
	var control_point_count := anim.track_get_key_count(track_indices.a)
	var reimported := false
	
	if control_point_count < 0:
		printerr("Not enough control points in the 'original_control_points' track! At least 2 control points are needed. In animation: %s" % [anim.resource_name])
		return
	
	if track_indices.b == -1:
		_recover_current_control_point_track(anim, anim_root_node)
		reimported = true
		
		# Find tracks again
		track_indices = _find_track_indices(anim, anim_root_node)
	
	if anim.track_get_key_count(track_indices.b) != control_point_count:
		printerr("The 'current_control_points' track must have exactly %s keyframes! If you need a different number of control points, press Reset and redesign the original control point track. In animation: '%s'" % [control_point_count, anim.resource_name])
		return

	if anim.track_get_key_count(track_indices.c) != control_point_count:
		printerr("The 'desired_control_points' track must have exactly %s keyframes! If you need a different number of control points, press Reset and redesign the original control point track. In animation: '%s'" % [control_point_count, anim.resource_name])
		return

	var old_times: Array[float] = []
	var new_times: Array[float] = []
	old_times.resize(control_point_count)
	new_times.resize(control_point_count)
	
	var any_timing_changes := false
	for key_index in range(control_point_count):
		var old_time := anim.track_get_key_time(track_indices.b, key_index)
		var new_time := anim.track_get_key_time(track_indices.c, key_index)
		old_times[key_index] = old_time
		new_times[key_index] = new_time
		
		if old_time != new_time: any_timing_changes = true
	
	if !any_timing_changes:
		return
	
	for track_index in range(anim.get_track_count()):
		if track_index == track_indices.a || track_index == track_indices.c:
			# Skip over Original Control Points track and Desired Control Points track
			continue
		if reimported && !anim.track_is_imported(track_index):
			# If the animation was re-imported, only re-time tracks that were imported.
			continue
		
		_transactions.retime_track(track_index, old_times, new_times)
	
	_transactions.retime_length(old_times, new_times)
	
	# Update checkboxes
	_transactions.update_track(track_indices.a, TrackState.blank(false))
	_transactions.update_track(track_indices.b, TrackState.blank(false, true))
	_transactions.update_track(track_indices.c, TrackState.blank(true))

func _recover_current_control_point_track(anim: Animation, anim_root_node: Node) -> void:
	var track_indices := _find_track_indices(anim, anim_root_node)

	var path_to_this_node := anim_root_node.get_path_to(self)
	_transactions.initialize_track(&"current_control_points", anim, track_indices.b, path_to_this_node, TrackState.blank(false, true))
	
	track_indices = _find_track_indices(anim, anim_root_node)

	var control_point_count := anim.track_get_key_count(track_indices.a)
	# Copy all control point keyframes from the original to the "current" track
	for key_index in range(control_point_count):
		var state := KeyState.create_from(anim, track_indices.a, key_index)
		_transactions.insert_key(track_indices.b, state)

	track_indices = _find_track_indices(anim, anim_root_node)
	# Disabled due to undo-redo inconsistencies
	# _transactions.move_track(track_indices.b, track_indices.a+1)

func get_original_retimed_animation_length(anim: Animation, mixer: AnimationMixer) -> Variant:
	if !anim: return null
	
	var anim_root_node := mixer.get_node(mixer.root_node)
	var track_indices := _find_track_indices(anim, anim_root_node)
	
	if track_indices.a == -1: return null

	var control_point_count := anim.track_get_key_count(track_indices.a)
	
	if control_point_count < 2: return null
	
	return anim.track_get_key_time(track_indices.a, control_point_count - 1)
		

func _validate_property(property: Dictionary) -> void:
	var property_name : StringName = property.name
	if [&"original_control_points", &"current_control_points", &"desired_control_points"].has(property_name):
		property.usage &= ~PROPERTY_USAGE_STORAGE

class Transactions extends RefCounted:
	var _undo_redo: EditorUndoRedoManager
	var _anim: Animation
	var _action_name: String
	
	func _init(undo_redo: EditorUndoRedoManager, anim: Animation, action_name: String):
		_undo_redo = undo_redo
		_anim = anim
		_action_name = action_name
	
	func create_action(backward_undo_ops: bool = false) -> void:
		_undo_redo.create_action(_action_name if _action_name else "Animation Timing Tool", UndoRedo.MERGE_ALL, _anim, backward_undo_ops)
	
	func initialize_track(property_name: StringName, anim: Animation, index: int, path_to_this_node: NodePath, state: TrackState) -> int:
		if index < 0:
			# Add track
			var new_index := anim.get_track_count()
			add_track(Animation.TrackType.TYPE_VALUE, new_index, "%s:%s" % [path_to_this_node, property_name])
			index = new_index
		
		update_track(index, state)
		clear_track(index)
		
		return index
	
	func move_track(from_index: int, to_index: int) -> void:
		create_action(true)
		_undo_redo.add_do_method(_anim, &"track_move_to", from_index, to_index)
		_undo_redo.add_undo_method(_anim, &"track_move_to", to_index, from_index)
		_undo_redo.commit_action()
	
	func add_track(type: Animation.TrackType, at_index: int, path: NodePath) -> void:
		create_action(true)
		_undo_redo.add_do_method(_anim, &"add_track", type, at_index)
		_undo_redo.add_do_method(_anim, &"track_set_path", at_index, path)
		_undo_redo.add_undo_method(_anim, &"remove_track", at_index)
		_undo_redo.commit_action()
	
	func update_track(track_index: int, state: TrackState) -> void:
		create_action(true)
		state.write(_undo_redo, _anim, track_index)
		_undo_redo.commit_action()
	
	func clear_track(track_index: int) -> void:
		create_action(true)
		for key_idx in range(_anim.track_get_key_count(track_index) - 1, -1, -1):
			KeyTransaction.write_removal(_undo_redo, _anim, track_index, key_idx)
		_undo_redo.commit_action()
	
	func remove_track(track_index: int) -> void:
		var track_path := _anim.track_get_path(track_index)
		var track_type := _anim.track_get_type(track_index)
		var track_state := TrackState.create_from(_anim, track_index)
		
		clear_track(track_index)
		
	func insert_key(track_index: int, state: KeyState) -> void:
		create_action(true)
		KeyTransaction.write_insertion(_undo_redo, _anim, track_index, state)
		_undo_redo.commit_action()
	
	func copy_track_keys(track_index: int, source_animation: Animation, source_track_index: int) -> void:
		create_action(true)
		var key_count := source_animation.track_get_key_count(source_track_index)
		for key_index in range(key_count):
			var key_state = KeyState.create_from(source_animation, source_track_index, key_index)
			KeyTransaction.write_insertion(_undo_redo, _anim, track_index, key_state)
		
		_undo_redo.commit_action()
	
	func retime_length(old_times: Array[float], new_times: Array[float]) -> void:
		var old_length := _anim.length
		var new_length := _remap_time(old_length, old_times, new_times)
		create_action(true)
		_undo_redo.add_do_property(_anim, &"length", new_length)
		_undo_redo.add_undo_property(_anim, &"length", old_length)
		_undo_redo.commit_action()
	
	func retime_track(track_index: int, old_times: Array[float], new_times: Array[float]) -> void:
		# Godot doesn't allow manipulating method keyframes any other way than moving them
		var move_only := _anim.track_get_type(track_index) == Animation.TrackType.TYPE_METHOD
		
		create_action(true)
		var track_key_count := _anim.track_get_key_count(track_index)
		var key_transactions : Array[KeyTransaction] = []
		key_transactions.resize(track_key_count)
		
		# Move to a temporary time, past the last keyframe
		# This is needed to avoid keyframes "merging" as we're moving them around one by one
		for key_index in range(track_key_count - 1, -1, -1):
			var transaction := KeyTransaction.create_from(_anim, track_index, key_index)
			key_transactions[key_index] = transaction
			
			var original_time := transaction.state.time
			
			if move_only:
				# Move the keyframe to a temporary time after the end of the animation, keeping the same order
				var temp_time = _anim.length + 10 + key_index

				_undo_redo.add_do_method(transaction, &"move", temp_time)
				_undo_redo.add_undo_method(transaction, &"move", original_time)
			else:
				# Remove the keyframe instead
				_undo_redo.add_do_method(transaction, &"remove")
				_undo_redo.add_undo_method(transaction, &"insert_at_time", original_time)
		
		for key_index in range(track_key_count):
			var transaction := key_transactions[key_index]

			var original_time := transaction.state.time
			var remapped_time := _remap_time(original_time, old_times, new_times)
			
			if move_only:
				# Move the keyframe back from the temporary time to the new remapped time
				var temp_time = _anim.length + 10 + key_index
				_undo_redo.add_do_method(transaction, &"move", remapped_time)
				_undo_redo.add_undo_method(transaction, &"move", temp_time)
			else:
				# Re-insert the keyframe instead
				_undo_redo.add_do_method(transaction, &"insert_at_time", remapped_time)
				_undo_redo.add_undo_method(transaction, &"remove")
		
		_undo_redo.commit_action()
	
	func _remap_time(time: float, old_times: Array[float], new_times: Array[float]) -> float:
		if time <= old_times[0]:
			return new_times[0] - (old_times[0] - time)
		if time >= old_times[old_times.size()-1]:
			return new_times[new_times.size()-1] + (time - old_times[old_times.size()-1])
		for i in range(old_times.size()-1):
			if time <= old_times[i + 1]:
				var old_from := old_times[i]
				var old_to := old_times[i+1]
				var new_from := new_times[i]
				var new_to := new_times[i+1]
				return remap(time, old_from, old_to, new_from, new_to)
		printerr("Should have been impossible to get here")
		return 0

class KeyTransaction extends RefCounted:
	var state: KeyState
	
	var _anim: Animation
	var _track_index: int
	var _key_index: int
	
	static func create_from(anim: Animation, track_index: int, key_index: int) -> KeyTransaction:
		var state := KeyState.create_from(anim, track_index, key_index)
		var transaction = KeyTransaction.new()
		transaction._anim = anim
		transaction._track_index = track_index
		transaction._key_index = key_index
		transaction.state = state
		return transaction

	func insert_at_time(time: float) -> void:
		state.time = time
		insert()
	
	func insert() -> void:
		_key_index = state.insert_to(_anim, _track_index)
	
	func remove() -> void:
		_anim.track_remove_key(_track_index, _key_index)
		_key_index = -1
	
	func move(time: float) -> void:
		var key_index := _anim.track_find_key(_track_index, state.time, Animation.FindMode.FIND_MODE_EXACT)
		if key_index == -1:
			var closest_time : Variant = null
			for i in range(_anim.track_get_key_count(_track_index)):
				var key_time := _anim.track_get_key_time(_track_index, i)
				if (closest_time == null || abs(key_time - time) < abs(closest_time - time)):
					closest_time = key_time
			
			printerr("Failed to move key in track %s at time %s to time %s; couldn't find the keyframe! Closest was at %s" % [_anim.track_get_path(_track_index), state.time, time, closest_time])
			return
		
		_anim.track_set_key_time(_track_index, key_index, time)
		_key_index = _anim.track_find_key(_track_index, state.time, Animation.FindMode.FIND_MODE_EXACT)
		state.time = time
	
	static func write_move(undo_redo: EditorUndoRedoManager, anim: Animation, track_index: int, key_index: int, to_time: float) -> void:
		var transaction := create_from(anim, track_index, key_index)
		var old_time := transaction.state.time

		undo_redo.add_do_method(transaction, &"move", to_time)
		undo_redo.add_undo_method(transaction, &"move", old_time)

	static func write_removal(undo_redo: EditorUndoRedoManager, anim: Animation, track_index: int, key_index: int) -> void:
		var transaction := create_from(anim, track_index, key_index)

		undo_redo.add_do_method(transaction, &"remove")
		undo_redo.add_undo_method(transaction, &"insert")

	static func write_insertion(undo_redo: EditorUndoRedoManager, anim: Animation, track_index: int, state: KeyState) -> void:
		var transaction := KeyTransaction.new()
		transaction._anim = anim
		transaction._track_index = track_index
		transaction._key_index = -1
		transaction.state = state

		undo_redo.add_do_method(transaction, &"insert")
		undo_redo.add_undo_method(transaction, &"remove")
	
class KeyState extends RefCounted:
	var time: float
	var value: Variant
	var transition: float
	
	var animation: StringName
	
	var bezier_value: float
	var bezier_in_handle: Vector2
	var bezier_out_handle: Vector2
	
	var audio_stream: Resource
	var audio_start_offset: float
	var audio_end_offset: float
	
	var method: StringName
	var method_params: Array

	static func time_value_pair(time: float, value: Variant) -> KeyState:
		var state := KeyState.new()
		state.time = time
		state.value = value
		return state

	static func create_from(anim: Animation, track_index: int, key_index: int) -> KeyState:
		var state := KeyState.new()
		state.time = anim.track_get_key_time(track_index, key_index)
		state.value = anim.track_get_key_value(track_index, key_index)
		state.transition = anim.track_get_key_transition(track_index, key_index)

		match anim.track_get_type(track_index):
			Animation.TrackType.TYPE_METHOD:
				# This crashes Godot, and the results can't be written to a keyframe anyway, so skip them.
				# state.method = anim.method_track_get_name(track_index, key_index)
				# state.method_params = anim.method_track_get_params(track_index, key_index)
				pass
			Animation.TrackType.TYPE_BEZIER:
				state.bezier_value = anim.bezier_track_get_key_value(track_index, key_index)
				state.bezier_in_handle = anim.bezier_track_get_key_in_handle(track_index, key_index)
				state.bezier_out_handle = anim.bezier_track_get_key_out_handle(track_index, key_index)
			Animation.TrackType.TYPE_AUDIO:
				state.audio_stream = anim.audio_track_get_key_stream(track_index, key_index)
				state.audio_start_offset = anim.audio_track_get_key_start_offset(track_index, key_index)
				state.audio_end_offset = anim.audio_track_get_key_end_offset(track_index, key_index)
			Animation.TrackType.TYPE_ANIMATION:
				state.animation = anim.animation_track_get_key_animation(track_index, key_index)

		return state

	func insert_to(anim: Animation, track_index: int) -> int:
		match anim.track_get_type(track_index):
			Animation.TrackType.TYPE_METHOD:
				pass
			Animation.TrackType.TYPE_BEZIER:
				return anim.bezier_track_insert_key(track_index, time, bezier_value, bezier_in_handle, bezier_out_handle)
			Animation.TrackType.TYPE_AUDIO:
				return anim.audio_track_insert_key(track_index, time, audio_stream, audio_start_offset, audio_end_offset)
			Animation.TrackType.TYPE_ANIMATION:
				return anim.animation_track_insert_key(track_index, time, animation)
				
		return anim.track_insert_key(track_index, time, value, transition)

class TrackState extends RefCounted:
	var track_type := Animation.TrackType.TYPE_VALUE
	
	var enabled := true
	var imported := false
	var update_mode := Animation.UpdateMode.UPDATE_DISCRETE
	var interpolation_type := Animation.InterpolationType.INTERPOLATION_NEAREST
	var interpolation_loop_wrap := false

	static func blank(enabled: bool, imported: bool = false) -> TrackState:
		var state := TrackState.new()
		state.enabled = enabled
		state.imported = imported
		return state
	
	static func create_from(anim: Animation, track_index: int) -> TrackState:
		var track_type := anim.track_get_type(track_index)
		var state := TrackState.new()
		state.track_type = track_type
		state.enabled = anim.track_is_enabled(track_index)
		state.imported = anim.track_is_imported(track_index)
		state.update_mode = anim.value_track_get_update_mode(track_index) if track_type == Animation.TrackType.TYPE_VALUE else 0
		state.interpolation_type = anim.track_get_interpolation_type(track_index)
		state.interpolation_loop_wrap = anim.track_get_interpolation_loop_wrap(track_index)
		return state

	func write_do(undo_redo: EditorUndoRedoManager, anim: Animation, track_index: int) -> void:
		undo_redo.add_do_method(anim, &"track_set_enabled", track_index, enabled)
		undo_redo.add_do_method(anim, &"track_set_imported", track_index, imported)
		if track_type == Animation.TrackType.TYPE_VALUE:
			undo_redo.add_do_method(anim, &"value_track_set_update_mode", track_index, update_mode)
		undo_redo.add_do_method(anim, &"track_set_interpolation_type", track_index, interpolation_type)
		undo_redo.add_do_method(anim, &"track_set_interpolation_loop_wrap", track_index, interpolation_loop_wrap)

	func write_undo(undo_redo: EditorUndoRedoManager, anim: Animation, track_index: int) -> void:
		undo_redo.add_undo_method(anim, &"track_set_enabled", track_index, enabled)
		undo_redo.add_undo_method(anim, &"track_set_imported", track_index, imported)
		if track_type == Animation.TrackType.TYPE_VALUE:
			undo_redo.add_undo_method(anim, &"value_track_set_update_mode", track_index, update_mode)
		undo_redo.add_undo_method(anim, &"track_set_interpolation_type", track_index, interpolation_type)
		undo_redo.add_undo_method(anim, &"track_set_interpolation_loop_wrap", track_index, interpolation_loop_wrap)

	func write(undo_redo: EditorUndoRedoManager, anim: Animation, track_index: int) -> void:
		var prev_state := create_from(anim, track_index)
		write_do(undo_redo, anim, track_index)
		prev_state.write_undo(undo_redo, anim, track_index)