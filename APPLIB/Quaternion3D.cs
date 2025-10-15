using System;
using System.Diagnostics;

namespace APPLIB
{
	// Token: 0x02000009 RID: 9
	[DebuggerDisplay("real = {real}, i = {i}, j = {j}, k = {k}")]
	public class Quaternion3D
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000055 RID: 85 RVA: 0x0000406C File Offset: 0x0000226C
		// (set) Token: 0x06000056 RID: 86 RVA: 0x00004085 File Offset: 0x00002285
		public Vector3D xyz
		{
			get
			{
				return new Vector3D(this.i, this.j, this.k);
			}
			set
			{
				this.i = value.X;
				this.j = value.Y;
				this.k = value.Z;
			}
		}

		// Token: 0x06000057 RID: 87 RVA: 0x000040AB File Offset: 0x000022AB
		public Quaternion3D()
		{
			this.real = 0f;
			this.i = 0f;
			this.j = 0f;
			this.k = 0f;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000040DF File Offset: 0x000022DF
		public Quaternion3D(float _real, float _i, float _j, float _k)
		{
			this.real = _real;
			this.i = _i;
			this.j = _j;
			this.k = _k;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00004104 File Offset: 0x00002304
		public Quaternion3D(Vector3D vecXYZ, float _real)
		{
			this.real = _real;
			this.i = vecXYZ.X;
			this.j = vecXYZ.Y;
			this.k = vecXYZ.Z;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00004137 File Offset: 0x00002337
		public Quaternion3D(Quaternion3D q)
		{
			this.real = q.real;
			this.i = q.i;
			this.j = q.j;
			this.k = q.k;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x0000416F File Offset: 0x0000236F
		public Vector3D ToVec()
		{
			return new Vector3D(this.xyz);
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600005C RID: 92 RVA: 0x0000417C File Offset: 0x0000237C
		public float Length
		{
			get
			{
				return Convert.ToSingle(Math.Sqrt((double)(this.real * this.real + this.xyz.LengthSquared)));
			}
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000041A4 File Offset: 0x000023A4
		public void Normalize()
		{
			float num = 1f / this.Length;
			this.xyz *= num;
			this.real *= num;
		}

		// Token: 0x0600005E RID: 94 RVA: 0x000041E0 File Offset: 0x000023E0
		public static Quaternion3D Invert(Quaternion3D q)
		{
			float lengthSquared = q.LengthSquared;
			Quaternion3D result;
			if (lengthSquared != 0f)
			{
				float num = 1f / lengthSquared;
				result = new Quaternion3D(q.xyz * -num, q.real * num);
			}
			else
			{
				result = q;
			}
			return result;
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00004226 File Offset: 0x00002426
		public float LengthSquared
		{
			get
			{
				return this.real * this.real + this.xyz.LengthSquared;
			}
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00004244 File Offset: 0x00002444
		public static Quaternion3D Multiply(Quaternion3D left, Quaternion3D right)
		{
			return new Quaternion3D(right.real * left.xyz + left.real * right.xyz + Vector3D.Cross(left.xyz, right.xyz), left.real * right.real - Vector3D.Dot(left.xyz, right.xyz));
		}

		// Token: 0x06000061 RID: 97 RVA: 0x000042B6 File Offset: 0x000024B6
		public static Quaternion3D operator *(Quaternion3D left, Quaternion3D right)
		{
			return Quaternion3D.Multiply(left, right);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x000042C0 File Offset: 0x000024C0
		public string WriteString()
		{
			return string.Format("{0} {1} {2} {3}", new object[]
			{
				this.real,
				this.i,
				this.j,
				this.k
			});
		}

		// Token: 0x06000063 RID: 99 RVA: 0x0000431C File Offset: 0x0000251C
		public string WriteFloat()
		{
			return string.Format("{0:0.000000} {1:0.000000} {2:0.000000} {3:0.000000}", new object[]
			{
				this.real,
				this.i,
				this.j,
				this.k
			});
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00004378 File Offset: 0x00002578
		public string WriteFloatXYZONLY()
		{
			return string.Format("{0:0.000000} {1:0.000000} {2:0.000000}", this.i, this.j, this.k);
		}

		// Token: 0x0400001E RID: 30
		public float real;

		// Token: 0x0400001F RID: 31
		public float i;

		// Token: 0x04000020 RID: 32
		public float j;

		// Token: 0x04000021 RID: 33
		public float k;
	}
}
