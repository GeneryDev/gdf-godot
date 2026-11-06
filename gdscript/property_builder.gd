class_name PropertyBuilder
extends RefCounted

var _dict := {
	"name": &"",
	"type": TYPE_NIL,
	"usage": PROPERTY_USAGE_NONE,
	"hint": PROPERTY_HINT_NONE,
	"hint_string": ""
}

func _init(name: StringName, type: Variant.Type = TYPE_NIL):
	_dict.name = name
	_dict.type = type
	
func as_dictionary() -> Dictionary:
	return _dict

func with_usage(usage: PropertyUsageFlags) -> PropertyBuilder:
	_dict.usage |= usage
	return self

func without_usage(usage: PropertyUsageFlags) -> PropertyBuilder:
	_dict.usage &= ~usage
	return self

func with_hint(hint: PropertyHint, hint_string: String = "") -> PropertyBuilder:
	_dict.hint = hint
	_dict.hint_string = hint_string
	return self

func with_custom(key: String, value: Variant) -> PropertyBuilder:
	_dict[key] = value
	return self

static func property(name: StringName, type: Variant.Type) -> PropertyBuilder:
	return PropertyBuilder.new(name, type).with_usage(PROPERTY_USAGE_DEFAULT)

static func enum_property(name: StringName, enum_entries: Dictionary) -> PropertyBuilder:
	var hint_string := ""
	for key in enum_entries:
		if !hint_string.is_empty():
			hint_string += ","
		hint_string += str(key).capitalize()
		hint_string += ":"
		hint_string += str(enum_entries[key])
	return PropertyBuilder.new(name, TYPE_INT).with_usage(PROPERTY_USAGE_DEFAULT).with_hint(PROPERTY_HINT_ENUM, hint_string)

static func group(name: StringName = &"", prefix: String = "") -> PropertyBuilder:
	return PropertyBuilder.new(name).with_usage(PROPERTY_USAGE_GROUP).with_hint(PROPERTY_HINT_NONE,prefix)

static func subgroup(name: StringName = &"", prefix: String = "") -> PropertyBuilder:
	return PropertyBuilder.new(name).with_usage(PROPERTY_USAGE_SUBGROUP).with_hint(PROPERTY_HINT_NONE,prefix)

static func category(name: StringName = &"") -> PropertyBuilder:
	return PropertyBuilder.new(name).with_usage(PROPERTY_USAGE_CATEGORY)