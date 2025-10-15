using System;
using System.IO;

namespace LZS_unpack
{
	/// <summary>
	/// BinaryWriter with Big Endian byte order support
	/// </summary>
	internal class BinaryWriterBE : BinaryWriter
	{
		private byte[] buffer16 = new byte[2];
		private byte[] buffer32 = new byte[4];

		public BinaryWriterBE(Stream stream) : base(stream)
		{
		}

		public override void Write(float value)
		{
			buffer32 = BitConverter.GetBytes(value);
			Array.Reverse(buffer32);
			base.Write(buffer32);
		}

		public override void Write(ushort value)
		{
			buffer16 = BitConverter.GetBytes(value);
			Array.Reverse(buffer16);
			base.Write(buffer16);
		}

		public override void Write(short value)
		{
			buffer16 = BitConverter.GetBytes(value);
			Array.Reverse(buffer16);
			base.Write(buffer16);
		}

		public override void Write(int value)
		{
			buffer32 = BitConverter.GetBytes(value);
			Array.Reverse(buffer32);
			base.Write(buffer32);
		}

		public void WritePadding(int count)
		{
			for (int i = 0; i < count; i++)
			{
				base.Write((byte)0);
			}
		}
	}
}

