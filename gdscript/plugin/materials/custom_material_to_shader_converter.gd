extends EditorResourceConversionPlugin

func _handles(resource: Resource) -> bool:
	return resource is GdfCustomMaterial

func _converts_to() -> String:
	return "ShaderMaterial"

func _convert(resource: Resource) -> Resource:
	var custom := resource as GdfCustomMaterial
	var shader_mat := ShaderMaterial.new()
	shader_mat.shader = custom.shader.duplicate()
	for property in custom.get_property_list():
		var property_name := property.name as StringName
		if property_name.begins_with("shader_parameter/"):
			shader_mat.set(property_name, custom.get(property_name))
	return shader_mat