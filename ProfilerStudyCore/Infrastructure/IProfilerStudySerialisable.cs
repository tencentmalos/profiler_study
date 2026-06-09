using System.IO;

namespace ProfilerStudy;

public interface IProfilerStudySerialisable
{
	void Read(BinaryReader binary_reader, int version);

	void Write(BinaryWriter binary_writer);
}
