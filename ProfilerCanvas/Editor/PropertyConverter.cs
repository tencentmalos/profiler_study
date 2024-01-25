using SCL;

namespace Editor;

public interface PropertyConverter
{
	Row CreateRow(object parent, PropertyName property_name, object value, bool read_only);

	void UpdateRow(Row row, object value);

	object GetNewValue(PropertyName property_name, ColRow colrow, object old_rect);
}
