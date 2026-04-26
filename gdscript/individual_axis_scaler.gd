@tool
extends Node
class_name IndividualAxisScaler;

signal scale_changed()

@export
var target : Node;

@export
var scale_x : float = 1:
	get:
		if is_instance_valid(target):
			return target.scale.x;
		else:
			return 1.0;
	set(value):
		if !is_instance_valid(target):
			return;
		var scale = target.scale;
		scale.x = value;
		target.scale = scale;
		scale_changed.emit();
@export
var scale_y : float = 1:
	get:
		if is_instance_valid(target):
			return target.scale.y;
		else:
			return 1.0;
	set(value):
		if !is_instance_valid(target):
			return;
		var scale = target.scale;
		scale.y = value;
		target.scale = scale;
		scale_changed.emit();
@export
var scale_z : float = 1:
	get:
		if is_instance_valid(target) && target is Node3D:
			return target.scale.z;
		else:
			return 1.0;
	set(value):
		if !is_instance_valid(target):
			return;
		if target is not Node3D:
			return;
		var scale = target.scale;
		scale.z = value;
		target.scale = scale;
		scale_changed.emit();
