namespace Editor;

public delegate void PropertyChangingHandler(object obj, string property_name, object old_value, object new_value, CmdGroup property_group_cmd);
