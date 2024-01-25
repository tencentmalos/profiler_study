using System.Collections.Generic;

namespace SCL;

public delegate void DeletingRowHandler(ICollection<Row> sel_rows, ref bool cancel);
