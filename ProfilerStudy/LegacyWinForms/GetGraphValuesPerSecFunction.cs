using System.Collections.Generic;

namespace ProfilerStudy;

internal delegate void GetGraphValuesPerSecFunction(long graph_name, long start_time, long end_time, List<PerSecValue> values, out long first_interval_time);
