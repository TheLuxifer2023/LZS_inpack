using System;
using System.Collections;

namespace APPLIB
{
	// Token: 0x02000003 RID: 3
	internal static class C3D
	{
		// Token: 0x06000035 RID: 53 RVA: 0x00002C60 File Offset: 0x00000E60
		public static int CalculateFaceCount(int[] indices, C3D.IndexType type)
		{
			if (type == C3D.IndexType.TriStrip)
			{
				int i = 0;
				int num = 0;
				while (i < indices.Length - 2)
				{
					if (indices[i + 2] != -1)
					{
						i++;
						num++;
					}
					else
					{
						i += 3;
					}
				}
				return num;
			}
			if (type == C3D.IndexType.TriList)
			{
				return indices.Length / 3;
			}
			throw new NotSupportedException("Unknown index type.");
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002CAC File Offset: 0x00000EAC
		public static int CalculateVertexCount(int[] indices)
		{
			Hashtable hashtable = new Hashtable();
			foreach (int num in indices)
			{
				if (num != -1)
				{
					hashtable[num] = 0;
				}
			}
			return hashtable.Count;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002CF0 File Offset: 0x00000EF0
		public static int[] GenerateTriangleList(int[] indices, int faceCount)
		{
			int[] array = new int[faceCount * 3];
			bool flag = true;
			int i = 0;
			int num = 0;
			while (i < indices.Length - 2)
			{
				int num2 = indices[i + 2];
				if (num2 != -1)
				{
					int num3 = indices[i];
					int num4 = indices[i + 1];
					i++;
					if (num3 != num4 && num4 != num2 && num3 != num2)
					{
						if (flag)
						{
							array[num] = num3;
							array[num + 1] = num4;
							array[num + 2] = num2;
						}
						else
						{
							array[num] = num4;
							array[num + 1] = num3;
							array[num + 2] = num2;
						}
					}
					num += 3;
					flag = !flag;
				}
				else
				{
					flag = true;
					i += 3;
				}
			}
			return array;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002D90 File Offset: 0x00000F90
		public static int[] GenerateTriList(int[] indices, int faceCount, bool windOrder)
		{
			int[] array = new int[faceCount * 3];
			bool flag = windOrder;
			int i = 0;
			int num = 0;
			while (i < indices.Length - 2)
			{
				int num2 = indices[i + 2];
				if (num2 != -1)
				{
					int num3 = indices[i];
					int num4 = indices[i + 1];
					i++;
					if (num3 != num4 && num4 != num2 && num3 != num2)
					{
						if (flag)
						{
							array[num] = num3;
							array[num + 1] = num4;
							array[num + 2] = num2;
						}
						else
						{
							array[num] = num4;
							array[num + 1] = num3;
							array[num + 2] = num2;
						}
					}
					num += 3;
					flag = !flag;
				}
				else
				{
					flag = true;
					i += 3;
				}
			}
			return array;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002E30 File Offset: 0x00001030
		public static int[] GenerateTriangleListWO(int[] indices, int faceCount, bool windOrder)
		{
			int[] array = new int[faceCount * 3];
			bool flag = windOrder;
			int i = 0;
			int num = 0;
			while (i < indices.Length - 2)
			{
				int num2 = indices[i + 2];
				if (num2 != -1)
				{
					int num3 = indices[i];
					int num4 = indices[i + 1];
					i++;
					if (num3 != num4 && num4 != num2 && num3 != num2)
					{
						if (flag)
						{
							array[num] = num3;
							array[num + 1] = num4;
							array[num + 2] = num2;
						}
						else
						{
							array[num] = num4;
							array[num + 1] = num3;
							array[num + 2] = num2;
						}
					}
					num += 3;
					flag = !flag;
				}
				else
				{
					flag = true;
					i += 3;
				}
			}
			return array;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002ED0 File Offset: 0x000010D0
		public static int[] GenerateTriangleListWO_SMART(int[] indices, int faceCount, bool windOrder)
		{
			if (indices.Length == 3)
			{
				return indices;
			}
			int[] array = new int[faceCount * 3];
			bool flag = windOrder;
			int i = 0;
			int num = 0;
			while (i < indices.Length - 2)
			{
				int num2 = indices[i + 2];
				if (num2 != -1)
				{
					int num3 = indices[i];
					int num4 = indices[i + 1];
					i++;
					if (num3 != num4 && num4 != num2 && num3 != num2)
					{
						if (flag)
						{
							array[num] = num3;
							array[num + 1] = num4;
							array[num + 2] = num2;
						}
						else
						{
							array[num] = num4;
							array[num + 1] = num3;
							array[num + 2] = num2;
						}
					}
					num += 3;
					flag = !flag;
				}
				else
				{
					flag = true;
					i += 3;
				}
			}
			return array;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002F78 File Offset: 0x00001178
		public static float DecompressUByte2Float(byte PackedByte)
		{
			return (float)((double)PackedByte / 255.0 * 2.0 - 1.0);
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002F9B File Offset: 0x0000119B
		public static float DecompressBoneWeights(byte PackedByte)
		{
			return (float)((double)PackedByte / 255.0);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002FAC File Offset: 0x000011AC
		public static float S_RTDecompressPosition(short Input)
		{
			short maxValue = short.MaxValue;
			return (float)Input / (float)maxValue;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002FC8 File Offset: 0x000011C8
		public static float U_RTDecompressPosition(ushort Input)
		{
			ushort maxValue = ushort.MaxValue;
			return (float)Input / (float)maxValue;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00002FE4 File Offset: 0x000011E4
		public static float unpackUV(ushort Input)
		{
			ushort maxValue = ushort.MaxValue;
			return (float)Input / (float)maxValue;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00003000 File Offset: 0x00001200
		public static float unpackUVSigned(short Input)
		{
			short maxValue = short.MaxValue;
			return (float)Input / (float)maxValue;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x0000301C File Offset: 0x0000121C
		public static float unpackUV2(short uv, int UVscale)
		{
			float num = 65535f;
			float num2 = Convert.ToSingle(uv);
			num2 += 32768f;
			num2 /= num;
			return num2 * (float)UVscale;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003048 File Offset: 0x00001248
		public static float FlipFloat(float inputF)
		{
			return -inputF;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x0000305C File Offset: 0x0000125C
		private static double deg2rad(double deg)
		{
			double num = 0.017453292519943295;
			return deg * num;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003078 File Offset: 0x00001278
		private static double rad2deg(double rad)
		{
			double num = 57.29577951308232;
			return rad * num;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003092 File Offset: 0x00001292
		public static float NanSafe(float val)
		{
			if (float.IsNaN(val))
			{
				return 0f;
			}
			if ((double)val + 0.0 == 0.0)
			{
				return 0f;
			}
			return Convert.ToSingle(val);
		}

		// Token: 0x06000046 RID: 70 RVA: 0x000030C8 File Offset: 0x000012C8
		public static Vector3D Quat2Euler_UBISOFT(Quaternion3D quat)
		{
			Vector3D vector3D = new Vector3D();
			double num = 1.5707963267948966;
			float num2 = quat.i * quat.j + quat.k * quat.real;
			if (num2 > 0.499f)
			{
				vector3D.X = 2f * (float)Math.Atan2((double)quat.i, (double)quat.real);
				vector3D.Y = (float)num;
				vector3D.Z = 0f;
			}
			else if (num2 < -0.499f)
			{
				vector3D.X = -2f * (float)Math.Atan2((double)quat.i, (double)quat.real);
				vector3D.Y = (float)(-(float)num);
				vector3D.Z = 0f;
			}
			else
			{
				float num3 = quat.i * quat.i;
				float num4 = quat.j * quat.j;
				float num5 = quat.k * quat.k;
				vector3D.X = (float)Math.Atan2((double)(2f * quat.j * quat.real - 2f * quat.i * quat.k), (double)(1f - 2f * num4 - 2f * num5));
				vector3D.Y = Convert.ToSingle(Math.Asin((double)(2f * num2)));
				vector3D.Z = (float)Math.Atan2((double)(2f * quat.i * quat.real - 2f * quat.j * quat.k), (double)(1f - 2f * num3 - 2f * num5));
			}
			if (float.IsNaN(vector3D.X))
			{
				vector3D.X = 0f;
			}
			if (float.IsNaN(vector3D.Y))
			{
				vector3D.Y = 0f;
			}
			if (float.IsNaN(vector3D.Z))
			{
				vector3D.Z = 0f;
			}
			return vector3D;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000032A8 File Offset: 0x000014A8
		public static Vector3D QuaternionToEuler(Quaternion3D quat)
		{
			Vector3D vector3D = new Vector3D();
			float num = quat.real * quat.real;
			float num2 = quat.i * quat.i;
			float num3 = quat.j * quat.j;
			float num4 = quat.k * quat.k;
			vector3D.Z = (float)C3D.rad2deg(Math.Atan2(2.0 * (double)(quat.j * quat.k + quat.i * quat.real), (double)(-(double)num2 - num3 + num4 + num)));
			vector3D.X = (float)C3D.rad2deg(Math.Asin(-2.0 * (double)(quat.i * quat.k - quat.j * quat.real)));
			vector3D.Y = (float)C3D.rad2deg(Math.Atan2(2.0 * (double)(quat.i * quat.j + quat.k * quat.real), (double)(num2 - num3 - num4 + num)));
			if (float.IsNaN(vector3D.X))
			{
				vector3D.X = 0f;
			}
			if (float.IsNaN(vector3D.Y))
			{
				vector3D.Y = 0f;
			}
			if (float.IsNaN(vector3D.Z))
			{
				vector3D.Z = 0f;
			}
			return vector3D;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x000033F8 File Offset: 0x000015F8
		public static Vector3D QuaternionToEulerRAD(Quaternion3D quat)
		{
			Vector3D vector3D = new Vector3D();
			float num = quat.real * quat.real;
			float num2 = quat.i * quat.i;
			float num3 = quat.j * quat.j;
			float num4 = quat.k * quat.k;
			vector3D.Z = (float)Math.Atan2(2.0 * (double)(quat.j * quat.k + quat.i * quat.real), (double)(-(double)num2 - num3 + num4 + num));
			vector3D.X = (float)Math.Asin(-2.0 * (double)(quat.i * quat.k - quat.j * quat.real));
			vector3D.Y = (float)Math.Atan2(2.0 * (double)(quat.i * quat.j + quat.k * quat.real), (double)(num2 - num3 - num4 + num));
			if (float.IsNaN(vector3D.X))
			{
				vector3D.X = 0f;
			}
			if (float.IsNaN(vector3D.Y))
			{
				vector3D.Y = 0f;
			}
			if (float.IsNaN(vector3D.Z))
			{
				vector3D.Z = 0f;
			}
			return vector3D;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003538 File Offset: 0x00001738
		public static Vector3D QuaternionToEulerRAD2(Quaternion3D quat)
		{
			Vector3D vector3D = new Vector3D();
			float real = quat.real;
			float i = quat.i;
			float j = quat.j;
			float k = quat.k;
			float num = i * i;
			float num2 = j * j;
			float num3 = k * k;
			vector3D.Z = (float)Math.Atan2(2.0 * (double)(real * i + j * k), (double)(1f - 2f * (num + num2)));
			vector3D.X = (float)Math.Asin(2.0 * (double)(real * j - k * i));
			vector3D.Y = (float)Math.Atan2(2.0 * (double)(real * k + i * j), (double)(1f - 2f * (num2 + num3)));
			return vector3D;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x000035FC File Offset: 0x000017FC
		public static Quaternion3D EulerAnglesToQuaternion(float yaw, float pitch, float roll)
		{
			double num = (double)C3D.NormalizeAngle(yaw);
			double num2 = (double)C3D.NormalizeAngle(pitch);
			double num3 = (double)C3D.NormalizeAngle(roll);
			double num4 = Math.Cos(num);
			double num5 = Math.Cos(num2);
			double num6 = Math.Cos(num3);
			double num7 = Math.Sin(num);
			double num8 = Math.Sin(num2);
			double num9 = Math.Sin(num3);
			return new Quaternion3D
			{
				real = (float)(num4 * num5 * num6 - num7 * num8 * num9),
				i = (float)(num7 * num8 * num6 + num4 * num5 * num9),
				j = (float)(num7 * num5 * num6 + num4 * num8 * num9),
				k = (float)(num4 * num8 * num6 - num7 * num5 * num9)
			};
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000036BC File Offset: 0x000018BC
		public static Quaternion3D DEG_EulerAnglesToQuaternion(float yaw, float pitch, float roll)
		{
			double num = C3D.deg2rad((double)yaw);
			double num2 = C3D.deg2rad((double)pitch);
			double num3 = C3D.deg2rad((double)roll);
			double num4 = Math.Cos(num);
			double num5 = Math.Cos(num2);
			double num6 = Math.Cos(num3);
			double num7 = Math.Sin(num);
			double num8 = Math.Sin(num2);
			double num9 = Math.Sin(num3);
			return new Quaternion3D
			{
				real = (float)(num4 * num5 * num6 - num7 * num8 * num9),
				i = (float)(num7 * num8 * num6 + num4 * num5 * num9),
				j = (float)(num7 * num5 * num6 + num4 * num8 * num9),
				k = (float)(num4 * num8 * num6 - num7 * num5 * num9)
			};
		}

		// Token: 0x0600004C RID: 76 RVA: 0x0000377C File Offset: 0x0000197C
		public static Quaternion3D Euler2Quat(Vector3D orientation)
		{
			Quaternion3D quaternion3D = new Quaternion3D();
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			C3D.MathUtil.SinCos(ref num, ref num4, orientation.X * 0.5f);
			C3D.MathUtil.SinCos(ref num2, ref num5, orientation.Y * 0.5f);
			C3D.MathUtil.SinCos(ref num3, ref num6, orientation.Z * 0.5f);
			quaternion3D.real = num6 * num4 * num5 + num3 * num * num2;
			quaternion3D.i = -num6 * num * num5 - num3 * num4 * num2;
			quaternion3D.j = num6 * num * num2 - num3 * num5 * num4;
			quaternion3D.k = num3 * num * num5 - num6 * num4 * num2;
			return quaternion3D;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00003848 File Offset: 0x00001A48
		private static float NormalizeAngle(float input)
		{
			return (float)((double)input * 3.141592653589793 / 360.0);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003870 File Offset: 0x00001A70
		public static Vector3D FromObjectToInertialQuaternion(ref Quaternion3D q)
		{
			float num = -2f * (q.j * q.k - q.real * q.i);
			float yy;
			float xx;
			float zz;
			if (Math.Abs(num) > 0.9999f)
			{
				yy = C3D.MathUtil.kPiOver2 * num;
				xx = Convert.ToSingle(Math.Atan2((double)(-(double)q.i * q.k + q.real * q.j), (double)(0.5f - q.j * q.j - q.k * q.k)));
				zz = 0f;
			}
			else
			{
				yy = Convert.ToSingle(Math.Asin((double)num));
				xx = Convert.ToSingle(Math.Atan2((double)(q.i * q.k + q.real * q.j), (double)(0.5f - q.i * q.i - q.j * q.j)));
				zz = Convert.ToSingle(Math.Atan2((double)(q.i * q.j + q.real * q.k), (double)(0.5f - q.i * q.i - q.k * q.k)));
			}
			return new Vector3D(xx, yy, zz);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x000039E0 File Offset: 0x00001BE0
		public static Vector3D FromInertialToObjectQuaternion(ref Quaternion3D q)
		{
			float num = -2f * (q.j * q.k + q.real * q.i);
			float yy;
			float xx;
			float zz;
			if (Math.Abs(num) > 0.9999f)
			{
				yy = C3D.MathUtil.kPiOver2 * num;
				xx = Convert.ToSingle(Math.Atan2((double)(-(double)q.i * q.k - q.real * q.j), (double)(0.5f - q.j * q.j - q.k * q.k)));
				zz = 0f;
			}
			else
			{
				yy = Convert.ToSingle(Math.Asin((double)num));
				xx = Convert.ToSingle(Math.Atan2((double)(q.i * q.k - q.real * q.j), (double)(0.5f - q.i * q.i - q.j * q.j)));
				zz = Convert.ToSingle(Math.Atan2((double)(q.i * q.j - q.real * q.k), (double)(0.5f - q.i * q.i - q.k * q.k)));
			}
			return new Vector3D(xx, yy, zz);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003B4E File Offset: 0x00001D4E
		public static Vector3D ToEulerAngles(Quaternion3D q)
		{
			return C3D.Eul_FromQuat(q, 0, 1, 2, 0, C3D.EulerParity.Even, C3D.EulerRepeat.No, C3D.EulerFrame.S);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00003B60 File Offset: 0x00001D60
		private static Vector3D Eul_FromQuat(Quaternion3D q, int i, int j, int k, int h, C3D.EulerParity parity, C3D.EulerRepeat repeat, C3D.EulerFrame frame)
		{
			double[,] array = new double[4, 4];
			double[,] array2 = array;
			double num = (double)(q.i * q.i + q.j * q.j + q.k * q.k + q.real * q.real);
			double num2;
			if (num > 0.0)
			{
				num2 = 2.0 / num;
			}
			else
			{
				num2 = 0.0;
			}
			double num3 = (double)q.i * num2;
			double num4 = (double)q.j * num2;
			double num5 = (double)q.k * num2;
			double num6 = (double)q.real * num3;
			double num7 = (double)q.real * num4;
			double num8 = (double)q.real * num5;
			double num9 = (double)q.i * num3;
			double num10 = (double)q.i * num4;
			double num11 = (double)q.i * num5;
			double num12 = (double)q.j * num4;
			double num13 = (double)q.j * num5;
			double num14 = (double)q.k * num5;
			array2[0, 0] = 1.0 - (num12 + num14);
			array2[0, 1] = num10 - num8;
			array2[0, 2] = num11 + num7;
			array2[1, 0] = num10 + num8;
			array2[1, 1] = 1.0 - (num9 + num14);
			array2[1, 2] = num13 - num6;
			array2[2, 0] = num11 - num7;
			array2[2, 1] = num13 + num6;
			array2[2, 2] = 1.0 - (num9 + num12);
			array2[3, 3] = 1.0;
			return C3D.Eul_FromHMatrix(array2, i, j, k, h, parity, repeat, frame);
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00003DAC File Offset: 0x00001FAC
		private static Vector3D Eul_FromHMatrix(double[,] M, int i, int j, int k, int h, C3D.EulerParity parity, C3D.EulerRepeat repeat, C3D.EulerFrame frame)
		{
			Vector3D vector3D = new Vector3D();
			if (repeat == C3D.EulerRepeat.Yes)
			{
				double num = Math.Sqrt(M[i, j] * M[i, j] + M[i, k] * M[i, k]);
				if (num > 0.00016)
				{
					vector3D.X = (float)Math.Atan2(M[i, j], M[i, k]);
					vector3D.Y = (float)Math.Atan2(num, M[i, i]);
					vector3D.Z = (float)Math.Atan2(M[j, i], -M[k, i]);
				}
				else
				{
					vector3D.X = (float)Math.Atan2(-M[j, k], M[j, j]);
					vector3D.Y = (float)Math.Atan2(num, M[i, i]);
					vector3D.Z = 0f;
				}
			}
			else
			{
				double num2 = Math.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
				if (num2 > 0.00016)
				{
					vector3D.X = (float)Math.Atan2(M[k, j], M[k, k]);
					vector3D.Y = (float)Math.Atan2(-M[k, i], num2);
					vector3D.Z = (float)Math.Atan2(M[j, i], M[i, i]);
				}
				else
				{
					vector3D.X = (float)Math.Atan2(-M[j, k], M[j, j]);
					vector3D.Y = (float)Math.Atan2(-M[k, i], num2);
					vector3D.Z = 0f;
				}
			}
			if (parity == C3D.EulerParity.Odd)
			{
				vector3D.X = -vector3D.X;
				vector3D.Y = -vector3D.Y;
				vector3D.Z = -vector3D.Z;
			}
			if (frame == C3D.EulerFrame.R)
			{
				double num3 = (double)vector3D.X;
				vector3D.X = vector3D.Z;
				vector3D.Z = (float)num3;
			}
			return vector3D;
		}

		// Token: 0x04000009 RID: 9
		private const double FLT_EPSILON = 1E-05;

		// Token: 0x02000004 RID: 4
		public enum IndexType
		{
			// Token: 0x0400000B RID: 11
			TriList = 4,
			// Token: 0x0400000C RID: 12
			TriStrip = 6
		}

		// Token: 0x02000005 RID: 5
		private enum EulerParity
		{
			// Token: 0x0400000E RID: 14
			Even,
			// Token: 0x0400000F RID: 15
			Odd
		}

		// Token: 0x02000006 RID: 6
		private enum EulerRepeat
		{
			// Token: 0x04000011 RID: 17
			No,
			// Token: 0x04000012 RID: 18
			Yes
		}

		// Token: 0x02000007 RID: 7
		private enum EulerFrame
		{
			// Token: 0x04000014 RID: 20
			S,
			// Token: 0x04000015 RID: 21
			R
		}

		// Token: 0x02000008 RID: 8
		public class MathUtil
		{
			// Token: 0x06000053 RID: 83 RVA: 0x00003FA4 File Offset: 0x000021A4
			public MathUtil()
			{
				C3D.MathUtil.kPi = 3.1415927f;
				C3D.MathUtil.k2Pi = C3D.MathUtil.kPi * 2f;
				C3D.MathUtil.kPiOver2 = C3D.MathUtil.kPi / 2f;
				C3D.MathUtil.k1OverPi = 1f / C3D.MathUtil.kPi;
				C3D.MathUtil.k1Over2Pi = 1f / C3D.MathUtil.k2Pi;
				C3D.MathUtil.kPiOver180 = C3D.MathUtil.kPi / 180f;
				C3D.MathUtil.k180OverPi = 180f / C3D.MathUtil.kPi;
				C3D.MathUtil.kZeroVector = new Vector3D(0f, 0f, 0f);
			}

			// Token: 0x06000054 RID: 84 RVA: 0x0000403A File Offset: 0x0000223A
			public static void SinCos(ref float returnSin, ref float returnCos, float theta)
			{
				returnSin = Convert.ToSingle(Math.Sin(Convert.ToDouble(C3D.deg2rad((double)theta))));
				returnCos = Convert.ToSingle(Math.Cos(Convert.ToDouble(C3D.deg2rad((double)theta))));
			}

			// Token: 0x04000016 RID: 22
			public static float kPi;

			// Token: 0x04000017 RID: 23
			public static float k2Pi;

			// Token: 0x04000018 RID: 24
			public static float kPiOver2;

			// Token: 0x04000019 RID: 25
			public static float k1OverPi;

			// Token: 0x0400001A RID: 26
			public static float k1Over2Pi;

			// Token: 0x0400001B RID: 27
			public static float kPiOver180;

			// Token: 0x0400001C RID: 28
			public static float k180OverPi;

			// Token: 0x0400001D RID: 29
			public static Vector3D kZeroVector;
		}
	}
}
