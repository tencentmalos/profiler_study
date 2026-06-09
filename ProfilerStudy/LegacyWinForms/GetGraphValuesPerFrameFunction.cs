using System.Collections.Generic;

namespace ProfilerStudy;

internal delegate void GetGraphValuesPerFrameFunction(int start_frame_index, int end_frame_index, long graph_name, List<FrameValue> values);
