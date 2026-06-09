namespace ProfilerStudy;

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
