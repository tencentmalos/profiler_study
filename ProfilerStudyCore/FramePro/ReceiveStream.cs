using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FramePro;

internal class ReceiveStream
{
	private class ReceiveBuffer
	{
		public byte[] m_Buffer;

		public int m_BufferLength;
	}

	private MemoryStream m_MemoryStream = new MemoryStream();

	private BinaryReader m_BinaryReader;

	private Queue<ReceiveBuffer> m_Buffers = new Queue<ReceiveBuffer>();

	private AutoResetEvent m_ReceivedBufferEvent = new AutoResetEvent(initialState: false);

	private volatile bool m_Finished;

	private long m_BytesReceived;

	private long m_BytesAvailable;

	private bool m_FinishedReceiving;

	public bool FinishedReceiving => m_FinishedReceiving;

	private long BytesAvailable => m_BytesAvailable - m_BytesReceived;

	private int BufferCount
	{
		get
		{
			lock (m_Buffers)
			{
				return m_Buffers.Count;
			}
		}
	}

	public long BytesReceived => m_BytesReceived;

	public ReceiveStream()
	{
		m_BinaryReader = new BinaryReader(m_MemoryStream);
	}

	public void EnqueueBuffer(byte[] buffer, int buffer_length)
	{
		ReceiveBuffer receiveBuffer = new ReceiveBuffer();
		receiveBuffer.m_Buffer = buffer;
		receiveBuffer.m_BufferLength = buffer_length;
		lock (m_Buffers)
		{
			m_Buffers.Enqueue(receiveBuffer);
		}
		m_ReceivedBufferEvent.Set();
	}

	public void SetFinished()
	{
		m_Finished = true;
		m_ReceivedBufferEvent.Set();
	}

	public int ReadInt32()
	{
		while (BytesAvailable < 4 && !m_FinishedReceiving)
		{
			WaitForReceiveBuffer();
		}
		m_BytesReceived += 4L;
		return m_BinaryReader.ReadInt32();
	}

	public uint ReadUInt32()
	{
		while (BytesAvailable < 4 && !m_FinishedReceiving)
		{
			WaitForReceiveBuffer();
		}
		m_BytesReceived += 4L;
		return m_BinaryReader.ReadUInt32();
	}

	public long ReadInt64()
	{
		while (BytesAvailable < 8 && !m_FinishedReceiving)
		{
			WaitForReceiveBuffer();
		}
		m_BytesReceived += 8L;
		return m_BinaryReader.ReadInt64();
	}

	public ulong ReadUInt64()
	{
		while (BytesAvailable < 8 && !m_FinishedReceiving)
		{
			WaitForReceiveBuffer();
		}
		m_BytesReceived += 8L;
		return m_BinaryReader.ReadUInt64();
	}

	public double ReadDouble()
	{
		while (BytesAvailable < 8 && !m_FinishedReceiving)
		{
			WaitForReceiveBuffer();
		}
		m_BytesReceived += 8L;
		return m_BinaryReader.ReadDouble();
	}

	public int Read(byte[] buffer, int offset, int size)
	{
		while (BytesAvailable == 0L && !m_FinishedReceiving)
		{
			WaitForReceiveBuffer();
		}
		if (BytesAvailable == 0L)
		{
			return 0;
		}
		int count = (int)Math.Min(BytesAvailable, size);
		int num = m_BinaryReader.Read(buffer, offset, count);
		m_BytesReceived += num;
		return num;
	}

	private void WaitForReceiveBuffer()
	{
		if (BufferCount == 0 && !m_Finished)
		{
			m_ReceivedBufferEvent.WaitOne();
		}
		if (BufferCount != 0)
		{
			int num = (int)BytesAvailable;
			byte[] buffer = null;
			if (num != 0)
			{
				buffer = new byte[num];
				for (int num2 = num; num2 != 0; num2 -= m_MemoryStream.Read(buffer, num - num2, num2))
				{
				}
			}
			m_MemoryStream.Seek(0L, SeekOrigin.Begin);
			if (num != 0)
			{
				m_MemoryStream.Write(buffer, 0, num);
			}
			ReceiveBuffer receiveBuffer = null;
			lock (m_Buffers)
			{
				receiveBuffer = m_Buffers.Dequeue();
			}
			m_MemoryStream.Write(receiveBuffer.m_Buffer, 0, receiveBuffer.m_BufferLength);
			m_MemoryStream.Seek(0L, SeekOrigin.Begin);
			m_BytesAvailable += receiveBuffer.m_BufferLength;
		}
		else if (m_Finished)
		{
			m_FinishedReceiving = true;
		}
	}
}
