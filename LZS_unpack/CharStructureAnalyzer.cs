using System;
using System.IO;

namespace LZS_unpack
{
	public class CharStructureAnalyzer
	{
		public static void AnalyzeCharData(string filePath, long offset, int expectedCount)
		{
			Console.WriteLine();
			Console.WriteLine("=== Deep Character Structure Analysis ===");
			Console.WriteLine("Offset: 0x" + offset.ToString("X") + " (" + offset + ")");
			Console.WriteLine("Expected chars: " + expectedCount);
			Console.WriteLine();

			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				// Try different field orders
				TestFieldOrder(fs, br, offset, expectedCount, "Pattern 1: code(int), x, y, w, h (floats)", 
					ReadPattern1);
				TestFieldOrder(fs, br, offset, expectedCount, "Pattern 2: x, y, w, h (floats), code(int)", 
					ReadPattern2);
				TestFieldOrder(fs, br, offset, expectedCount, "Pattern 3: x, y (floats), code(int), w, h (floats)", 
					ReadPattern3);
				TestFieldOrder(fs, br, offset, expectedCount, "Pattern 4: code(short), padding, x, y, w, h (floats)", 
					ReadPattern4);
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		delegate CharData ReadPatternDelegate(BinaryReader br, long startPos, int structSize, out bool valid);

		static void TestFieldOrder(FileStream fs, BinaryReader br, long offset, int expectedCount, 
			string patternName, ReadPatternDelegate readFunc)
		{
			Console.WriteLine();
			Console.WriteLine("--- " + patternName + " ---");
			
			int[] structSizes = { 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64 };
			
			int bestValidPercent = 0;
			int bestStructSize = 0;
			
			foreach (int structSize in structSizes)
			{
				fs.Seek(offset, SeekOrigin.Begin);
				
				int validCount = 0;
				int sampleSize = Math.Min(100, expectedCount);
				
				for (int i = 0; i < sampleSize && fs.Position < fs.Length - structSize; i++)
				{
					long charPos = fs.Position;
					
					try
					{
						bool valid;
						CharData data = readFunc(br, charPos, structSize, out valid);
						
						if (valid)
						{
							validCount++;
							if (validCount <= 3) // Show first 3 valid samples
							{
								Console.WriteLine("  Valid #" + validCount + ": code=" + data.code + 
									", x=" + data.x + ", y=" + data.y + 
									", w=" + data.w + ", h=" + data.h);
							}
						}
						
						fs.Seek(charPos + structSize, SeekOrigin.Begin);
					}
					catch
					{
						break;
					}
				}
				
				int validPercent = validCount * 100 / sampleSize;
				
				if (validPercent > bestValidPercent)
				{
					bestValidPercent = validPercent;
					bestStructSize = structSize;
				}
				
				if (validPercent >= 30)
				{
					Console.WriteLine("  Size " + structSize + ": " + validPercent + "% valid (" + validCount + "/" + sampleSize + ")");
				}
			}
			
			Console.WriteLine("Best: size=" + bestStructSize + ", valid=" + bestValidPercent + "%");
		}

		struct CharData
		{
			public int code;
			public float x, y, w, h;
		}

		// Pattern 1: code(int), x, y, w, h (floats)
		static CharData ReadPattern1(BinaryReader br, long startPos, int structSize, out bool valid)
		{
			CharData data = new CharData();
			data.code = br.ReadInt32();
			data.x = br.ReadSingle();
			data.y = br.ReadSingle();
			data.w = br.ReadSingle();
			data.h = br.ReadSingle();
			
			valid = IsValidChar(data);
			return data;
		}

		// Pattern 2: x, y, w, h (floats), code(int)
		static CharData ReadPattern2(BinaryReader br, long startPos, int structSize, out bool valid)
		{
			CharData data = new CharData();
			data.x = br.ReadSingle();
			data.y = br.ReadSingle();
			data.w = br.ReadSingle();
			data.h = br.ReadSingle();
			data.code = br.ReadInt32();
			
			valid = IsValidChar(data);
			return data;
		}

		// Pattern 3: x, y (floats), code(int), w, h (floats)
		static CharData ReadPattern3(BinaryReader br, long startPos, int structSize, out bool valid)
		{
			CharData data = new CharData();
			data.x = br.ReadSingle();
			data.y = br.ReadSingle();
			data.code = br.ReadInt32();
			data.w = br.ReadSingle();
			data.h = br.ReadSingle();
			
			valid = IsValidChar(data);
			return data;
		}

		// Pattern 4: code(short), padding, x, y, w, h (floats)
		static CharData ReadPattern4(BinaryReader br, long startPos, int structSize, out bool valid)
		{
			CharData data = new CharData();
			data.code = br.ReadInt16();
			br.ReadInt16(); // padding
			data.x = br.ReadSingle();
			data.y = br.ReadSingle();
			data.w = br.ReadSingle();
			data.h = br.ReadSingle();
			
			valid = IsValidChar(data);
			return data;
		}

		static bool IsValidChar(CharData data)
		{
			return data.code >= 0 && data.code <= 0x10FFFF &&
			       data.x >= 0 && data.x <= 8192 &&
			       data.y >= 0 && data.y <= 8192 &&
			       data.w > 0 && data.w <= 512 &&
			       data.h > 0 && data.h <= 512;
		}
	}
}

