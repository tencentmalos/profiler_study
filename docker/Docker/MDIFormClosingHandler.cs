using System.Collections.Generic;
using System.Windows.Forms;

namespace Docker;

public delegate void MDIFormClosingHandler(ICollection<Control> controls, ref bool cancel);
