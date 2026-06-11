using System;

namespace ProfilerStudy.Tracy;

public sealed class TracyFileFormatException : InvalidOperationException
{
	public TracyFileFormatException(string errorCode, string message)
		: base(message)
	{
		ErrorCode = errorCode;
	}

	public string ErrorCode { get; }
}
