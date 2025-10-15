using System;
using System.IO;

namespace LZS_unpack
{
	// Token: 0x0200000A RID: 10
	internal class BinaryReaderBE : BinaryReader
	{
		// Token: 0x06000065 RID: 101 RVA: 0x000043B2 File Offset: 0x000025B2
		public BinaryReaderBE(Stream stream) : base(stream)
		{
		}

		// Token: 0x06000066 RID: 102 RVA: 0x000043D3 File Offset: 0x000025D3
		public override float ReadSingle()
		{
			this.a32 = base.ReadBytes(4);
			Array.Reverse(this.a32);
			return BitConverter.ToSingle(this.a32, 0);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x000043F9 File Offset: 0x000025F9
		public override ushort ReadUInt16()
		{
			this.a16 = base.ReadBytes(2);
			Array.Reverse(this.a16);
			return BitConverter.ToUInt16(this.a16, 0);
		}

		// Token: 0x06000068 RID: 104 RVA: 0x0000441F File Offset: 0x0000261F
		public override short ReadInt16()
		{
			this.a16 = base.ReadBytes(2);
			Array.Reverse(this.a16);
			return BitConverter.ToInt16(this.a16, 0);
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004445 File Offset: 0x00002645
		public override int ReadInt32()
		{
			this.a32 = base.ReadBytes(4);
			Array.Reverse(this.a32);
			return BitConverter.ToInt32(this.a32, 0);
		}

		// Token: 0x04000022 RID: 34
		private byte[] a16 = new byte[2];

		// Token: 0x04000023 RID: 35
		private byte[] a32 = new byte[4];
	}
}
