@tool
extends Node
class_name AutoMinimumSize;

const ROUNDING_MODE_NONE : int = 0
const ROUNDING_MODE_ROUND_UP : int = 1
const ROUNDING_MODE_ROUND_DOWN : int = 2
const ROUNDING_MODE_ROUND : int = 3

@export
var size_source_control : Control:
	set(value):
		if size_source_control == value:
			return;
		if size_source_control != null:
			size_source_control.resized.disconnect(update);
			size_source_control.minimum_size_changed.disconnect(update);
		size_source_control = value;
		if size_source_control != null:
			size_source_control.resized.connect(update);
			size_source_control.minimum_size_changed.connect(update);
		update();

@export
var size_target_control : Control:
	set(value):
		if size_target_control == value:
			return;
		if size_target_control != null:
			size_target_control.resized.disconnect(update);
			size_target_control.minimum_size_changed.disconnect(update);
		size_target_control = value;
		if size_target_control != null:
			size_target_control.resized.connect(update);
			size_target_control.minimum_size_changed.connect(update);
		update();

@export_enum("Combined Minimum Size","Actual Size")
var mode : int:
	set(value):
		if mode == value:
			return;
		mode = value;
		update();

@export
var size_scale : Vector2 = Vector2.ONE:
	set(value):
		if size_scale == value:
			return;
		size_scale = value;
		update();

@export
var maximum_size : Vector2 = Vector2.ZERO:
	set(value):
		if maximum_size == value:
			return;
		maximum_size = value;
		update();

@export_enum("No Rounding","Round Up","Round Down","Closest Integer")
var rounding_mode : int = ROUNDING_MODE_NONE:
	set(value):
		if rounding_mode == value:
			return;
		rounding_mode = value;
		update();

@export_group("Set Aspect Ratio")
@export_custom(PROPERTY_HINT_GROUP_ENABLE, "")
var set_aspect_ratio : bool:
	set(value):
		if set_aspect_ratio == value:
			return;
		set_aspect_ratio = value;
		update();

@export_enum("Width Controls Height", "Height Controls Width", "Fit", "Cover")
var stretch_mode : int:
	set(value):
		if stretch_mode == value:
			return;
		stretch_mode = value;
		if set_aspect_ratio:
			update();

@export_range(0.001, 10, 0.001, "or_greater")
var aspect_ratio : float = 1.0:
	set(value):
		if aspect_ratio == value:
			return;
		aspect_ratio = value;
		if set_aspect_ratio:
			update();

func _ready():
	update();

func update() -> void:
	if size_source_control == null || size_target_control == null:
		return;
	
	var new_size: Vector2 = size_source_control.get_combined_minimum_size() if mode == 0 else size_source_control.size;
	if set_aspect_ratio:
		var adjusted_stretch_mode : int = stretch_mode;
		if adjusted_stretch_mode == AspectRatioContainer.STRETCH_FIT || adjusted_stretch_mode == AspectRatioContainer.STRETCH_COVER:
			adjusted_stretch_mode = AspectRatioContainer.STRETCH_HEIGHT_CONTROLS_WIDTH if bool(int(aspect_ratio > 1) ^ int(new_size.x > new_size.y)) == (stretch_mode != AspectRatioContainer.STRETCH_FIT) else AspectRatioContainer.STRETCH_WIDTH_CONTROLS_HEIGHT;
		match adjusted_stretch_mode:
			AspectRatioContainer.STRETCH_WIDTH_CONTROLS_HEIGHT:
				new_size.y = new_size.x * (1.0 / aspect_ratio);
			AspectRatioContainer.STRETCH_HEIGHT_CONTROLS_WIDTH:
				new_size.x = new_size.y * aspect_ratio;
	
	new_size *= size_scale;
	if maximum_size.x != 0:
		new_size.x = min(new_size.x, maximum_size.x);
	if maximum_size.y != 0:
		new_size.y = min(new_size.y, maximum_size.y);
		
	match rounding_mode:
		ROUNDING_MODE_NONE:
			pass;
		ROUNDING_MODE_ROUND_UP:
			new_size = new_size.ceil();
		ROUNDING_MODE_ROUND_DOWN:
			new_size = new_size.floor();
		ROUNDING_MODE_ROUND:
			new_size = new_size.round();
	
	size_target_control.custom_minimum_size = new_size;

