using System;
using System.Diagnostics;

namespace APPLIB
{
	// Token: 0x02000002 RID: 2
	[DebuggerDisplay("X = {X}, Y = {Y}, Z = {Z}")]
	public class Vector3D
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			Vector3D vector3D = obj as Vector3D;
			return vector3D != null && this.IsEqualTo(vector3D);
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002078 File Offset: 0x00000278
		public override int GetHashCode()
		{
			int num = 17;
			num = num * 23 + this.X.GetHashCode();
			return num * 23 + this.Y.GetHashCode();
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020AB File Offset: 0x000002AB
		public Vector3D()
		{
			this.X = 0f;
			this.Y = 0f;
			this.Z = 0f;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020D4 File Offset: 0x000002D4
		public Vector3D(float xx, float yy, float zz)
		{
			this.X = xx;
			this.Y = yy;
			this.Z = zz;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020F1 File Offset: 0x000002F1
		public Vector3D(Vector3D Vec)
		{
			this.X = Vec.X;
			this.Y = Vec.Y;
			this.Z = Vec.Z;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x0000211D File Offset: 0x0000031D
		public void SetVector(float xx, float yy, float zz)
		{
			this.X = xx;
			this.Y = yy;
			this.Z = zz;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002134 File Offset: 0x00000334
		public float DotProduct(Vector3D Vec)
		{
			return this.X * Vec.X + this.Y * Vec.Y + this.Z * Vec.Z;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x0000215F File Offset: 0x0000035F
		public float Length()
		{
			return (float)Math.Sqrt((double)(this.X * this.X + this.Y * this.Y + this.Z * this.Z));
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002194 File Offset: 0x00000394
		public float AngleTo(Vector3D Vec)
		{
			float num = this.DotProduct(Vec);
			float num2 = this.Length() * Vec.Length();
			if (num2 == 0f)
			{
				return 0f;
			}
			return (float)Math.Acos((double)(num / num2));
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000021D0 File Offset: 0x000003D0
		public Vector3D UnitVector()
		{
			Vector3D vector3D = new Vector3D();
			float num = this.Length();
			if (num == 0f)
			{
				vector3D.SetVector(0f, 0f, 0f);
				return vector3D;
			}
			vector3D.X = this.X / num;
			vector3D.Y = this.Y / num;
			vector3D.Z = this.Z / num;
			return vector3D;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002234 File Offset: 0x00000434
		public bool IsCodirectionalTo(Vector3D Vec)
		{
			Vector3D vector3D = this.UnitVector();
			Vector3D vector3D2 = Vec.UnitVector();
			return vector3D.X == vector3D2.X && vector3D.Y == vector3D2.Y && vector3D.Z == vector3D2.Z;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000227C File Offset: 0x0000047C
		public bool IsEqualTo(Vector3D Vec)
		{
			return this.X == Vec.X && this.Y == Vec.Y && this.Z == Vec.Z;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000022AC File Offset: 0x000004AC
		public bool IsParallelTo(Vector3D Vec)
		{
			Vector3D vector3D = this.UnitVector();
			Vector3D vector3D2 = Vec.UnitVector();
			return (vector3D.X == vector3D2.X && vector3D.Y == vector3D2.Y && vector3D.Z == vector3D2.Z) | (vector3D.X == -vector3D2.X && vector3D.Y == -vector3D2.Y && vector3D.Z == vector3D2.Z);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000232C File Offset: 0x0000052C
		public bool IsPerpendicularTo(Vector3D Vec)
		{
			double num = (double)this.AngleTo(Vec);
			return num == 1.5707963267948966;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002351 File Offset: 0x00000551
		public object IsXAxis()
		{
			if (this.X != 0f && this.Y == 0f && this.Z == 0f)
			{
				return true;
			}
			return false;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002387 File Offset: 0x00000587
		public object IsYAxis()
		{
			if (this.X == 0f && this.Y != 0f && this.Z == 0f)
			{
				return true;
			}
			return false;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000023BD File Offset: 0x000005BD
		public object IsZAxis()
		{
			if (this.X == 0f && this.Y == 0f && this.Z != 0f)
			{
				return true;
			}
			return false;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000023F4 File Offset: 0x000005F4
		public void Negate()
		{
			this.X = (float)((double)this.X * -1.0);
			this.Y = (float)((double)this.Y * -1.0);
			this.Z = (float)((double)this.Z * -1.0);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x0000244C File Offset: 0x0000064C
		public Vector3D Add(Vector3D Vec)
		{
			return new Vector3D
			{
				X = this.X + Vec.X,
				Y = this.Y + Vec.Y,
				Z = this.Z + Vec.Z
			};
		}

		// Token: 0x06000014 RID: 20 RVA: 0x0000249C File Offset: 0x0000069C
		public Vector3D Subtract(Vector3D Vec)
		{
			return new Vector3D
			{
				X = this.X - Vec.X,
				Y = this.Y - Vec.Y,
				Z = this.Z - Vec.Z
			};
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000024E9 File Offset: 0x000006E9
		public static bool operator ==(Vector3D a, Vector3D b)
		{
			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002518 File Offset: 0x00000718
		public static bool operator !=(Vector3D a, Vector3D b)
		{
			return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002548 File Offset: 0x00000748
		public static Vector3D operator +(Vector3D a, Vector3D b)
		{
			return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002588 File Offset: 0x00000788
		public static Vector3D operator -(Vector3D left, Vector3D right)
		{
			return new Vector3D
			{
				X = left.X - right.X,
				Y = left.Y - right.Y,
				Z = left.Z - right.Z
			};
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000025D8 File Offset: 0x000007D8
		public static Vector3D Multiply(Vector3D vector, float scale)
		{
			return new Vector3D(vector.X * scale, vector.Y * scale, vector.Z * scale);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002604 File Offset: 0x00000804
		public static Vector3D Multiply(Vector3D vector, int scale)
		{
			return new Vector3D(vector.X * (float)scale, vector.Y * (float)scale, vector.Z * (float)scale);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002634 File Offset: 0x00000834
		public static Vector3D Multiply(Vector3D vector, Vector3D scale)
		{
			return new Vector3D(vector.X * scale.X, vector.Y * scale.Y, vector.Z * scale.Z);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002670 File Offset: 0x00000870
		public static Vector3D Multiply(Vector3D vec, Quaternion3D q)
		{
			float num = Convert.ToSingle(2) * (q.i * vec.X + q.j * vec.Y + q.k * vec.Z);
			float num2 = Convert.ToSingle(2) * q.real;
			float num3 = num2 * q.real - Convert.ToSingle(1);
			float xx = num3 * vec.X + num * q.i + num2 * (q.k * vec.Z - q.k * vec.Y);
			float yy = num3 * vec.Y + num * q.j + num2 * (q.k * vec.X - q.i * vec.Z);
			float zz = num3 * vec.Z + num * q.k + num2 * (q.i * vec.Y - q.j * vec.X);
			return new Vector3D(xx, yy, zz);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000276E File Offset: 0x0000096E
		public static Vector3D operator *(Vector3D left, float right)
		{
			return Vector3D.Multiply(left, right);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002777 File Offset: 0x00000977
		public static Vector3D operator *(Vector3D left, int right)
		{
			return Vector3D.Multiply(left, right);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002780 File Offset: 0x00000980
		public static Vector3D operator *(float left, Vector3D right)
		{
			return Vector3D.Multiply(right, left);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002789 File Offset: 0x00000989
		public static Vector3D operator *(Vector3D left, Vector3D right)
		{
			return Vector3D.Multiply(left, right);
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002792 File Offset: 0x00000992
		public static Vector3D operator *(Vector3D left, Quaternion3D right)
		{
			return Vector3D.Multiply(left, right);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x0000279C File Offset: 0x0000099C
		public static Vector3D operator /(Vector3D vec, float scale)
		{
			float num = 1f / scale;
			vec.X *= num;
			vec.Y *= num;
			vec.Z *= num;
			return vec;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x000027DC File Offset: 0x000009DC
		public static Vector3D Cross(Vector3D left, Vector3D right)
		{
			return new Vector3D(left.Y * right.Z - left.Z * right.Y, left.Z * right.X - left.X * right.Z, left.X * right.Y - left.Y * right.X);
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002843 File Offset: 0x00000A43
		public static float Dot(Vector3D left, Vector3D right)
		{
			return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002870 File Offset: 0x00000A70
		public Vector3D Normalized()
		{
			this.Normalize();
			return this;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002888 File Offset: 0x00000A88
		public void Normalize()
		{
			float num = 1f / this.Length();
			this.X *= num;
			this.Y *= num;
			this.Z *= num;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000028CC File Offset: 0x00000ACC
		public static Vector3D Normalize(Vector3D vec)
		{
			float num = 1f / vec.Length();
			vec.X *= num;
			vec.Y *= num;
			vec.Z *= num;
			return vec;
		}

		// Token: 0x17000001 RID: 1
		public float this[int index]
		{
			get
			{
				if (index == 0)
				{
					return this.X;
				}
				if (index == 1)
				{
					return this.Y;
				}
				if (index == 2)
				{
					return this.Z;
				}
				throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
			}
			set
			{
				if (index == 0)
				{
					this.X = value;
					return;
				}
				if (index == 1)
				{
					this.Y = value;
					return;
				}
				if (index == 2)
				{
					this.Z = value;
					return;
				}
				throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600002A RID: 42 RVA: 0x00002982 File Offset: 0x00000B82
		public float LengthSquared
		{
			get
			{
				return this.X * this.X + this.Y * this.Y + this.Z * this.Z;
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000029B0 File Offset: 0x00000BB0
		public string WriteString()
		{
			return string.Format("{0} {1} {2}", this.X, this.Y, this.Z);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x000029EC File Offset: 0x00000BEC
		public string WriteFloat()
		{
			return string.Format("{0:0.000000} {1:0.000000} {2:0.000000}", this.X, this.Y, this.Z);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002A28 File Offset: 0x00000C28
		public string WriteFloatWD()
		{
			return string.Format("{2:0.000000} {1:0.000000} {0:0.000000}", this.X, this.Y, this.Z);
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002A64 File Offset: 0x00000C64
		public string WriteInt()
		{
			return string.Format("{0:0} {1:0} {2:0}", this.X, this.Y, this.Z);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002AA0 File Offset: 0x00000CA0
		public string WriteFlipFloat()
		{
			return string.Format("{0:0.000000} {2:0.000000} {1:0.000000}", this.X, this.Y, this.Z);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002ADC File Offset: 0x00000CDC
		public string WriteFlipFloatNEGX()
		{
			return string.Format("{0:0.000000} {2:0.000000} {1:0.000000}", this.X, this.Y, C3D.FlipFloat(this.Z));
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002B1C File Offset: 0x00000D1C
		public string WriteFloatFBX()
		{
			return string.Format("{0:0.000000},{1:0.000000},{2:0.000000}", this.X, this.Y, this.Z);
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002B58 File Offset: 0x00000D58
		public string WriteStringFBX()
		{
			return string.Format("{0},{1},{2}", this.X, this.Y, this.Z);
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002B94 File Offset: 0x00000D94
		public string WriteFlipFloatFBX()
		{
			return string.Format("{0:0.000000},{1:0.000000},{2:0.000000}", this.X, this.Y, C3D.FlipFloat(this.Z));
		}

		// Token: 0x04000001 RID: 1
		public float X;

		// Token: 0x04000002 RID: 2
		public float Y;

		// Token: 0x04000003 RID: 3
		public float Z;

		// Token: 0x04000004 RID: 4
		public static readonly Vector3D UnitX = new Vector3D(1f, 0f, 0f);

		// Token: 0x04000005 RID: 5
		public static readonly Vector3D UnitY = new Vector3D(0f, 1f, 0f);

		// Token: 0x04000006 RID: 6
		public static readonly Vector3D UnitZ = new Vector3D(0f, 0f, 1f);

		// Token: 0x04000007 RID: 7
		public static readonly Vector3D Zero = new Vector3D(0f, 0f, 0f);

		// Token: 0x04000008 RID: 8
		public static readonly Vector3D One = new Vector3D(1f, 1f, 1f);
	}
}
