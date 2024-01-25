using System.Collections.Generic;

namespace SCL;

public delegate void RowMoveRequestHandler(List<Row> rows, Row target_row, EDropMode drop_mode, ref bool cancel);
