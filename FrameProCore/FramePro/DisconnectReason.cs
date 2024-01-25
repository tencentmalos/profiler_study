namespace FramePro;

public enum DisconnectReason
{
	None,
	Requested,
	LostConnection,
	Errors,
	BadVersion,
	UnexpectedPacket,
	NoData
}
