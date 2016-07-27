using System;
using System.Text;
using System.IO;

namespace Disa.Framework
{
	internal static class BubbleGroupDatabasePrimitives
	{
		public static string AsciiBytesToString(byte[] buffer, int startIndex, int endIndex)
		{
			return Encoding.ASCII.GetString(buffer, startIndex, endIndex - startIndex);
		}

	    private static int ReadInt32(Stream stream)
		{
		    var buffer = new byte[4];
		    stream.Read(buffer, 0, buffer.Length);
		    return BitConverter.ToInt32(buffer, 0);
		}

		public static void JumpBubbleHeader(Stream stream)
		{
			JumpBubbleBytes(stream);
		}

		public static void ReadBubbleHeader(Stream stream, out byte[] bytes, out int bytesLength)
		{
			bytes = ReadBubbleBytes(stream, out bytesLength);
		}

		public static string ReadBubbleHeaderType(Stream stream, out byte[] bytes, out int bytesLength, out int endPosition)
		{
			bytes = ReadBubbleBytes(stream, out bytesLength);
			return ReadBubbleHeaderStruct(bytes, bytesLength, 0, out endPosition);
		}

		public static void FindBubbleHeaderDelimiter(byte[] bytes, int bytesLength, int beginSearch, out int foundPosition)
		{
			foundPosition = -1;
			for (var i = beginSearch; i < bytesLength; i++)
			{
				if (bytes[i] != ':') continue;
				foundPosition = i;
				break;
			}
		}

		public static string ReadBubbleHeaderStruct(byte[] bytes, int bytesLength, int beginPosition, out int endPosition)
		{
			FindBubbleHeaderDelimiter(bytes, bytesLength, beginPosition, out endPosition);
			return AsciiBytesToString(bytes, beginPosition, endPosition);
		}

		public static byte[] ReadBubbleData(Stream stream, out int bytesLength)
		{
			return ReadBubbleBytes(stream, out bytesLength);
		}

		public static byte[] ReadBubbleData(Stream stream)
		{
			int bytesLength;
			return ReadBubbleBytes(stream, out bytesLength);
		}

		public static int JumpBubbleData(Stream stream)
		{
			return JumpBubbleBytes(stream);
		}

		private static int JumpBubbleBytes(Stream stream)
		{
			var lengthPosition = stream.Position - 4;
			stream.Position = lengthPosition;
			var length = ReadInt32(stream);
			stream.Position = lengthPosition;

			var stringPosition = stream.Position - length;
			stream.Position = stringPosition;
			return length;
		}

		private static byte[] ReadBubbleBytes(Stream stream, out int bytesLength)
		{
			var lengthPosition = stream.Position - 4;
			stream.Position = lengthPosition;
			var length = ReadInt32(stream);
			stream.Position = lengthPosition;

			var stringPosition = stream.Position - length;
			stream.Position = stringPosition;
			var stringBytes = new byte[length];
			stream.Read(stringBytes, 0, length);
			stream.Position = stringPosition;

			bytesLength = length;
			return stringBytes;
		}

        public static void WriteBubbleHeader(Stream stream, byte[] header)
        {
            WriteBubbleBytes(stream, header);
        }

		public static void WriteBubbleHeader(Stream stream, string str)
		{
			var bytes = Encoding.ASCII.GetBytes(str);
			WriteBubbleBytes(stream, bytes);
		}

		public static void WriteBubbleData(Stream stream, byte[] data)
		{
			WriteBubbleBytes(stream, data);
		}

		private static void WriteBubbleBytes(Stream stream, byte[] bytes)
		{
			var length = bytes.Length;

			stream.Write(bytes, 0, bytes.Length);

			var buffer = new byte[4];
			buffer[0] = (byte)length;
			buffer[1] = (byte)(length >> 8);
			buffer[2] = (byte)(length >> 16);
			buffer[3] = (byte)(length >> 24);
			stream.Write(buffer, 0, 4);
		}
	}
}

