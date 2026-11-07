@tool
@abstract
extends ShaderMaterial
class_name GdfCustomMaterial

enum MaterialPropertyFlag {
	NONE,
	MUTATES_SHADER,
	UPDATES_SHADER_PARAMETERS
}

@export_tool_button("Refresh")
var _button_debug_refresh : Callable = _debug_refresh

@export_storage
var custom_properties : Dictionary[StringName, Variant]:
	get:
		return custom_properties
	set(value):
		custom_properties = value.duplicate()
		_refresh_shader()
		_update_all_shader_parameters()

var _custom_property_list : Array[Dictionary] = []
var _custom_property_map : Dictionary[StringName, Dictionary] = {}

var _is_shader_ready := false
var _pending_shader_parameters : Dictionary[StringName, Variant] = {}

# Map of material subclass identifiers to (map of shader keys to weak refs of shaders)
static var shader_map : Dictionary[StringName, Dictionary] = {}
static var shader_map_mutex : Mutex = Mutex.new()

@abstract func get_custom_material_subclass_identifier() -> StringName
@abstract func get_custom_shader_type() -> Shader.Mode
@abstract func get_custom_material_properties() -> Array[Dictionary]
@abstract func compute_material_defines() -> Dictionary[String, String]
@abstract func compute_material_render_modes() -> Array[String]
@abstract func compute_material_includes() -> Array[String]
@abstract func update_shader_parameter(property: StringName, value: Variant) -> void

func _ensure_custom_property_list_ready() -> void:
	if _custom_property_list: return
	_custom_property_list.clear()
	var bit_written_count := 0
	for property in get_custom_material_properties():
		if !_validate_custom_property(property):
			continue
		_custom_property_list.append(property)
		if (property.usage & PROPERTY_USAGE_STORAGE) != 0:
			_custom_property_map[property.name as StringName] = property

		if (property.material_flags & MaterialPropertyFlag.MUTATES_SHADER) != 0:
			var property_bit_count := _get_property_bit_count(property)
			property.shader_key_bit_count = property_bit_count;
			property.shader_key_bit_shift = bit_written_count;
			property.shader_key_bit_mask = ((1 << property_bit_count) - 1) << bit_written_count;
			
			bit_written_count += property_bit_count

func _get_property_bit_count(property: Dictionary) -> int:
	var property_bit_count := 0
	match property.type:
		TYPE_BOOL:
			property_bit_count = 1
		TYPE_INT:
			if (property.hint & PROPERTY_HINT_ENUM) != 0:
				var enum_value_count : int = property.hint_string.count(",")+1
				property_bit_count = ceili(log(enum_value_count) / log(2))

	if property_bit_count == 0:
		printerr("Property %s not properly configured to mutate shader: type must be either bool, or an int-backed enum")
		return 0
		
	return property_bit_count

func _validate_custom_property(property: Dictionary) -> bool:
	if !property: return false
	if !("name" in property):
		printerr("Missing 'name' in property: " + str(property))
		return false
	if !("type" in property):
		printerr("Missing 'type' in property: " + str(property))
		return false
	if !("usage" in property):
		printerr("Missing 'usage' in property: " + str(property))
		return false
	if !("hint" in property):
		printerr("Missing 'hint' in property: " + str(property))
		return false
	if !("hint_string" in property):
		printerr("Missing 'hint_string' in property: " + str(property))
		return false
	if !("default" in property):
		if property.type != TYPE_NIL:
			printerr("Missing 'default' in property: " + str(property))
			return false
		property.default = null
		if !is_instance_of(property.default, property.type):
			printerr("Mismatched types between property and default value: " + str(property))
			return false
	if !("material_flags" in property):
		property.material_flags = MaterialPropertyFlag.NONE
	return true

func _clear_property_list_cache() -> void:
	_custom_property_list.clear()
	_custom_property_map.clear()

func _get_property_list() -> Array[Dictionary]:
	_ensure_custom_property_list_ready()
	return _custom_property_list

func _get_shader_type_string() -> String:
	var mode := get_custom_shader_type()
	match(mode):
		Shader.MODE_SPATIAL: return "spatial"
		Shader.MODE_CANVAS_ITEM: return "canvas_item"
		Shader.MODE_FOG: return "fog"
		Shader.MODE_PARTICLES: return "particles"
		Shader.MODE_SKY: return "sky"
		Shader.MODE_TEXTURE_BLIT: return "texture_blit"
	printerr("No such shader type " + str(mode) + ", defaulting to spatial")
	return "spatial"

func _set(property: StringName, value: Variant) -> bool:
	_ensure_custom_property_list_ready()
	if property in _custom_property_map:
		custom_properties[property] = value
		var flags : MaterialPropertyFlag = _custom_property_map[property].material_flags as MaterialPropertyFlag
		if (flags & MaterialPropertyFlag.MUTATES_SHADER) != 0:
			_refresh_shader()
		if (flags & MaterialPropertyFlag.UPDATES_SHADER_PARAMETERS) != 0:
			_update_shader_parameter(property, value)
		return true
	return false

func _get(property: StringName) -> Variant:
	_ensure_custom_property_list_ready()
	if !(property in _custom_property_map):
		return null
	if property in custom_properties:
		return custom_properties[property]
	return _custom_property_map[property].default

func _property_get_revert(property: StringName) -> Variant:
	if !(property in _custom_property_map):
		return null
	return _custom_property_map[property].default

func _property_can_revert(property: StringName) -> bool:
	if property in _custom_property_map:
		return true
	return false

func _validate_property(property: Dictionary) -> void:
	var prop_name := property["name"] as StringName
	
	# Disable storage of custom properties (done through the custom_properties dictionary instead)
	if _custom_property_map.has(prop_name):
		property["usage"] &= ~PROPERTY_USAGE_STORAGE
	
	if prop_name.begins_with("shader_parameter/"):
		property["usage"] &= ~PROPERTY_USAGE_EDITOR
		property["usage"] &= ~PROPERTY_USAGE_STORAGE
		property["usage"] |= PROPERTY_USAGE_READ_ONLY
	
	match prop_name:
		&"shader":
			property["usage"] &= ~PROPERTY_USAGE_EDITOR
			property["usage"] &= ~PROPERTY_USAGE_STORAGE

func _refresh_shader() -> void:
	_ensure_custom_property_list_ready()

	var shader_key := _compute_shader_key()
	
	var subclass_identifier := get_custom_material_subclass_identifier()
	shader_map_mutex.lock()
	var shader_map_for_subclass := shader_map.get_or_add(subclass_identifier, {})
	shader = _get_or_create_shader(shader_key, shader_map_for_subclass)
	shader_map_mutex.unlock()
	
	_is_shader_ready = true
	
	for key in _pending_shader_parameters:
		_update_shader_parameter(key, _pending_shader_parameters[key])
	_pending_shader_parameters.clear()

func _get_or_create_shader(shader_key: int, shader_map_for_subclass: Dictionary) -> Shader:
	if shader_map_for_subclass.has(shader_key):
		var ref : WeakRef = shader_map_for_subclass[shader_key]
		var referenced_shader = ref.get_ref()
		if referenced_shader is Shader:
			return referenced_shader
	
	# Couldn't find it, create anew
	var new_shader := _create_shader()
	shader_map_for_subclass[shader_key] = weakref(new_shader)
	return new_shader

func _create_shader() -> Shader:
	var define_mappings := compute_material_defines()
	var defines := []
	for key in define_mappings:
		var value := define_mappings[key]
		if !value.is_empty():
			defines.append("#define %s %s" % [key, value])
		else:
			defines.append("#define %s" % [key])
	var render_modes := compute_material_render_modes()
	var includes : Array = compute_material_includes()
	includes = includes.map(func(path: String) -> String:
		return "#include \"%s\"" % path.c_escape()
	)

	var new_shader := Shader.new()
	new_shader.code = \
		"shader_type %s;\n" % [_get_shader_type_string()] + \
				"render_mode %s;\n" % [", ".join(render_modes)] + \
				"\n" + \
				"\n".join(defines) + \
				"\n" + \
				"\n".join(includes)

	return new_shader

func _update_shader_parameter(property_name: StringName, value: Variant) -> void:
	if !_is_shader_ready:
		_pending_shader_parameters[property_name] = value
		return
	
	update_shader_parameter(property_name, value)

func _update_all_shader_parameters() -> void:
	_ensure_custom_property_list_ready()
	for property_name in _custom_property_map:
		var property := _custom_property_map[property_name];
		var flags : MaterialPropertyFlag = property.material_flags as MaterialPropertyFlag
		if (flags & MaterialPropertyFlag.UPDATES_SHADER_PARAMETERS) != 0:
			_update_shader_parameter(property_name, get(property_name))

func _compute_shader_key() -> int:
	_ensure_custom_property_list_ready()
	var key := 0
	for property in _custom_property_list:
		if (property.usage & PROPERTY_USAGE_STORAGE) == 0:
			continue
		if property.material_flags != MaterialPropertyFlag.MUTATES_SHADER:
			continue
		
		var value := int(get(property.name))
		key |= (value << property.shader_key_bit_shift) & property.shader_key_bit_mask
	return key

func _debug_refresh():
	_clear_property_list_cache()
	shader_map_mutex.lock()
	shader_map.get_or_add(get_custom_material_subclass_identifier(),{}).clear()
	shader_map_mutex.unlock()
	_refresh_shader()
