using System;
using System.IO;

namespace LZS_unpack
{
	public class FontDataFinder
	{
		public static void FindFontData(string filePath)
		{
			Console.WriteLine();
			Console.WriteLine("=== Finding Font Data Absolute Offsets ===");
			Console.WriteLine("File: " + Path.GetFileName(filePath));
			Console.WriteLine();

			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				// Read header (same as original unpacker)
				br.ReadInt32(); // magic
				int num = br.ReadInt32(); // offset1 = 84
				int num2 = br.ReadInt32(); // offset2 = 3162
				br.ReadInt32();
				int num3 = br.ReadInt32(); // count1 (objects) = 4
				
				Console.WriteLine("Header:");
				Console.WriteLine("  Offset1: " + num);
				Console.WriteLine("  Offset2: " + num2);
				Console.WriteLine("  Object count: " + num3);
				Console.WriteLine();

				// Skip to class definitions
				fs.Seek((long)(num + 8), SeekOrigin.Begin);
				int num12 = br.ReadInt32();
				int num13 = br.ReadInt32(); // num classes
				int num14 = br.ReadInt32(); // num instances
				
				Console.WriteLine("Classes: " + num13);
				Console.WriteLine("Instances: " + num14);
				Console.WriteLine();

				// Skip class def table
				fs.Seek((long)(num12 * 4 + 12), SeekOrigin.Current);
				
				// Read class names offsets
				int[] classNameOffsets = new int[num13];
				for (int i = 0; i < num13; i++)
				{
					br.ReadInt32();
					br.ReadInt32();
					classNameOffsets[i] = br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
				}
				
				long stringTableStart = fs.Position + (long)(num14 * 24);
				
				// Read class names
				string[] classNames = new string[num13];
				for (int i = 0; i < num13; i++)
				{
					fs.Seek(stringTableStart + (long)classNameOffsets[i], SeekOrigin.Begin);
					string name = "";
					byte b;
					while ((b = br.ReadByte()) > 0)
					{
						name += (char)b;
					}
					classNames[i] = name;
				}
				
				// Read instance list
				fs.Seek((long)(num + num2), SeekOrigin.Begin);
				
				int[] instanceClass = new int[num3];
				int[] instanceCount = new int[num3];
				int[] instanceSize = new int[num3];
				int[] instanceOffset = new int[num3];
				long[] instanceAbsOffset = new long[num3];
				
				long dataStart = fs.Position + (long)(num3 * 36);
				
				Console.WriteLine("Instance List:");
				Console.WriteLine();
				
				for (int i = 0; i < num3; i++)
				{
					instanceClass[i] = br.ReadInt32() - 1;
					instanceCount[i] = br.ReadInt32();
					instanceSize[i] = br.ReadInt32();
					instanceAbsOffset[i] = dataStart;
					dataStart += (long)instanceSize[i];
					instanceOffset[i] = br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					
					string className = instanceClass[i] >= 0 && instanceClass[i] < classNames.Length ? 
						classNames[instanceClass[i]] : "UNKNOWN";
					
					Console.WriteLine("Instance " + i + ":");
					Console.WriteLine("  Class: " + className + " (ID: " + instanceClass[i] + ")");
					Console.WriteLine("  Count: " + instanceCount[i]);
					Console.WriteLine("  Size: " + instanceSize[i] + " bytes");
					Console.WriteLine("  Relative offset: " + instanceOffset[i]);
					Console.WriteLine("  ABSOLUTE offset: 0x" + instanceAbsOffset[i].ToString("X") + " (" + instanceAbsOffset[i] + ")");
					
					if (className == "PBitmapFontCharInfo")
					{
						Console.WriteLine();
						Console.WriteLine("========================================");
						Console.WriteLine("FOUND PBitmapFontCharInfo!");
						Console.WriteLine("========================================");
						Console.WriteLine("Absolute offset: 0x" + instanceAbsOffset[i].ToString("X") + " (" + instanceAbsOffset[i] + ")");
						Console.WriteLine("Character count: " + instanceCount[i]);
						Console.WriteLine("Total size: " + instanceSize[i] + " bytes");
						
						if (instanceCount[i] > 0)
						{
							int structSize = instanceSize[i] / instanceCount[i];
							Console.WriteLine("Structure size: " + structSize + " bytes per char");
							Console.WriteLine();
							Console.WriteLine("To extract, use:");
							Console.WriteLine("  LZS_inpack.exe -extractchar " + Path.GetFileName(filePath) + 
								" " + instanceAbsOffset[i] + " " + instanceCount[i] + " " + structSize);
						}
						Console.WriteLine("========================================");
					}
					
					Console.WriteLine();
				}
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}
	}
}

