namespace SCL;

public interface IDataGridEditControl
{
	object Value { get; set; }

	event EditControlValueChangedHandler EditControlValueChanged;
}
