using System.IO;

namespace FramePro;

public interface IFrameProSerialisable
{
	void Read(BinaryReader binary_reader, int version);

	void Write(BinaryWriter binary_writer);
}
