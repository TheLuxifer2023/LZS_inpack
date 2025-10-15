using System;
using System.IO;
using System.Text;

namespace LZS_unpack
{
	/// <summary>
	/// Debug tool for analyzing Phyre font structure
	/// </summary>
	internal class PhyreDebugger
	{
		public static void DumpHexAtOffset(string filePath, long offset, int bytes)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				fs.Seek(offset, SeekOrigin.Begin);
				
				Console.WriteLine("=== Hex Dump at offset 0x" + offset.ToString("X") + " ===");
				Console.WriteLine();
				
				for (int i = 0; i < bytes && fs.Position < fs.Length; i += 16)
				{
					long pos = fs.Position;
					Console.Write(pos.ToString("X8") + ":  ");
					
					// Read 16 bytes
					byte[] lineBytes = new byte[16];
					int bytesRead = 0;
					for (int j = 0; j < 16 && fs.Position < fs.Length; j++)
					{
						lineBytes[j] = br.ReadByte();
						bytesRead++;
					}
					
					// Print hex
					for (int j = 0; j < bytesRead; j++)
					{
						Console.Write(lineBytes[j].ToString("X2") + " ");
						if (j == 7) Console.Write(" ");
					}
					
					// Padding
					for (int j = bytesRead; j < 16; j++)
					{
						Console.Write("   ");
					}
					
					// Print ASCII
					Console.Write("  |");
					for (int j = 0; j < bytesRead; j++)
					{
						char c = (char)lineBytes[j];
						if (c >= 32 && c < 127)
							Console.Write(c);
						else
							Console.Write(".");
					}
					Console.WriteLine("|");
				}
				
				Console.WriteLine();
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		public static void AnalyzeCharStructure(string filePath, long offset, int sampleSize)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				Console.WriteLine("=== Analyzing Character Structure ===");
				Console.WriteLine("Offset: 0x" + offset.ToString("X"));
				Console.WriteLine("Testing different structure sizes...");
				Console.WriteLine();

				// Test different structure sizes
				int[] testSizes = { 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64 };

				foreach (int structSize in testSizes)
				{
					fs.Seek(offset, SeekOrigin.Begin);
					
					Console.WriteLine("--- Testing structure size: " + structSize + " bytes ---");
					
					int validCount = 0;
					for (int i = 0; i < sampleSize && fs.Position < fs.Length - structSize; i++)
					{
						long charPos = fs.Position;
						
						// Read as potential character
						int code = br.ReadInt32();
						float x = br.ReadSingle();
						float y = br.ReadSingle();
						float w = br.ReadSingle();
						float h = br.ReadSingle();
						
						// Skip to next structure
						fs.Seek(charPos + structSize, SeekOrigin.Begin);
						
						// Validate
						if (code >= 0 && code < 0x10FFFF &&
						    x >= 0 && x <= 8192 &&
						    y >= 0 && y <= 8192 &&
						    w >= 0 && w <= 512 &&
						    h >= 0 && h <= 512)
						{
							validCount++;
							
							if (validCount <= 3)
							{
								Console.WriteLine("  Sample " + i + ": code=" + code + 
									" '" + (char)code + "'" +
									", x=" + x + ", y=" + y + 
									", w=" + w + ", h=" + h);
							}
						}
					}
					
					float validPercent = (float)validCount / sampleSize * 100;
					Console.WriteLine("  Valid: " + validCount + "/" + sampleSize + 
						" (" + validPercent.ToString("F1") + "%)");
					Console.WriteLine();
					
					if (validPercent > 80)
					{
						Console.WriteLine(">>> This structure size looks correct! <<<");
						Console.WriteLine();
					}
				}
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		public static void FindTextureData(string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				Console.WriteLine("=== Searching for Texture Data ===");
				Console.WriteLine();

				// Common DDS magic
				uint DDS_MAGIC = 0x20534444; // "DDS "
				
				// Search for DDS files embedded
				long pos = 0;
				int foundCount = 0;
				
				while (pos < fs.Length - 4 && foundCount < 10)
				{
					fs.Seek(pos, SeekOrigin.Begin);
					uint magic = br.ReadUInt32();
					
					if (magic == DDS_MAGIC)
					{
						Console.WriteLine("Found DDS magic at offset: 0x" + pos.ToString("X"));
						foundCount++;
					}
					
					pos += 1024; // Skip 1KB
				}
				
				if (foundCount == 0)
				{
					Console.WriteLine("No embedded DDS files found.");
					Console.WriteLine("Texture data is likely in raw compressed format.");
					Console.WriteLine();
					
					// Look for large contiguous data blocks
					Console.WriteLine("Searching for large data blocks (potential textures)...");
					AnalyzeLargeDataBlocks(fs, br);
				}
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		private static void AnalyzeLargeDataBlocks(FileStream fs, BinaryReader br)
		{
			long fileSize = fs.Length;
			
			// Typical texture sizes
			long[] textureSizes = { 
				256 * 256,      // 64 KB
				512 * 512,      // 256 KB
				1024 * 1024,    // 1 MB
				2048 * 2048,    // 4 MB
				4096 * 4096     // 16 MB
			};

			Console.WriteLine();
			Console.WriteLine("File size: " + fileSize + " bytes (" + (fileSize / 1024 / 1024) + " MB)");
			Console.WriteLine();
			Console.WriteLine("Looking for blocks matching common texture sizes:");
			
			foreach (long size in textureSizes)
			{
				int dimension = (int)Math.Sqrt(size);
				long actualSize = size; // DXT5: 1 byte per pixel
				
				if (actualSize < fileSize)
				{
					Console.WriteLine("  " + dimension + "x" + dimension + " = " + 
						(actualSize / 1024) + " KB - searching...");
						
					// Check at common offsets
					long[] testOffsets = { 
						0x50000,  // ~320 KB
						0x80000,  // ~512 KB
						0x100000, // 1 MB
						0x200000, // 2 MB
						fileSize - actualSize - 10000 // Near end
					};
					
					foreach (long offset in testOffsets)
					{
						if (offset > 0 && offset + actualSize <= fileSize)
						{
							fs.Seek(offset, SeekOrigin.Begin);
							byte[] sample = br.ReadBytes(256);
							
							// Check if this looks like compressed texture
							int uniqueBytes = CountUniqueBytes(sample);
							
							if (uniqueBytes > 100) // Good distribution
							{
								Console.WriteLine("    Potential texture at 0x" + offset.ToString("X") + 
									" (unique bytes: " + uniqueBytes + "/256)");
							}
						}
					}
				}
			}
		}

		private static int CountUniqueBytes(byte[] data)
		{
			bool[] seen = new bool[256];
			int count = 0;
			
			for (int i = 0; i < data.Length && i < 1024; i++)
			{
				if (!seen[data[i]])
				{
					seen[data[i]] = true;
					count++;
				}
			}
			
			return count;
		}

		public static void FindCharacterDataOffsets(string filePath, int expectedCount)
		{
			Console.WriteLine();
			Console.WriteLine("=== Searching for Character Data (expecting ~" + expectedCount + " chars) ===");
			Console.WriteLine();

			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				// Test common structure sizes
				int[] structSizes = { 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64 };
				
				// Search at regular intervals
				long fileSize = fs.Length;
				long step = 512; // Check every 512 bytes (more detailed)
				
				int bestValidPercent = 0;
				long bestOffset = 0;
				int bestStructSize = 0;
				
				Console.WriteLine("Scanning file for character-like data patterns...");
				Console.WriteLine("File size: " + (fileSize / 1024 / 1024) + " MB");
				Console.WriteLine();

				int matchesFound = 0;

				for (long testOffset = 0; testOffset < fileSize - 100000; testOffset += step)
				{
					foreach (int structSize in structSizes)
					{
						fs.Seek(testOffset, SeekOrigin.Begin);
						
						int validCount = 0;
						int floatPatternCount = 0; // Count of valid float patterns
						int sampleSize = Math.Min(100, expectedCount);
						
						for (int i = 0; i < sampleSize && fs.Position < fileSize - structSize; i++)
						{
							long charPos = fs.Position;
							
							try
							{
								// Try pattern 1: int code, float x, y, w, h
								int code = br.ReadInt32();
								float x = br.ReadSingle();
								float y = br.ReadSingle();
								float w = br.ReadSingle();
								float h = br.ReadSingle();
								
								// Relaxed validation
								bool validCode = code >= 0 && code <= 0x10FFFF; // Full Unicode range
								bool validFloats = 
									x >= 0 && x <= 8192 &&
									y >= 0 && y <= 8192 &&
									w > 0 && w <= 512 &&
									h > 0 && h <= 512;
								
								if (validCode && validFloats)
								{
									validCount++;
								}
								else if (validFloats)
								{
									floatPatternCount++;
								}
								
								fs.Seek(charPos + structSize, SeekOrigin.Begin);
							}
							catch
							{
								break;
							}
						}
						
						int validPercent = validCount * 100 / sampleSize;
						int floatPercent = floatPatternCount * 100 / sampleSize;
						
						// Accept if either pattern is good
						int effectivePercent = Math.Max(validPercent, floatPercent);
						
						if (effectivePercent > bestValidPercent)
						{
							bestValidPercent = effectivePercent;
							bestOffset = testOffset;
							bestStructSize = structSize;
						}
						
						if (effectivePercent >= 50 && matchesFound < 10)
						{
							matchesFound++;
							Console.WriteLine("Match #" + matchesFound + " at offset 0x" + testOffset.ToString("X") + 
								": size=" + structSize + 
								", valid=" + validPercent + "%" +
								", floats=" + floatPercent + "%");
						}
					}
				}

				Console.WriteLine();
				if (bestValidPercent >= 50)
				{
					Console.WriteLine("========================================");
					Console.WriteLine("BEST MATCH FOUND!");
					Console.WriteLine("========================================");
					Console.WriteLine("Offset: 0x" + bestOffset.ToString("X") + " (" + bestOffset + ")");
					Console.WriteLine("Structure size: " + bestStructSize + " bytes");
					Console.WriteLine("Valid chars: " + bestValidPercent + "%");
					Console.WriteLine();
					Console.WriteLine("Now analyze this offset with -debug:");
					Console.WriteLine("  LZS_inpack.exe -debug " + System.IO.Path.GetFileName(filePath) + " " + bestOffset);
					Console.WriteLine("========================================");
				}
				else
				{
					Console.WriteLine("Could not find character data with good confidence.");
					Console.WriteLine("Best match: offset=0x" + bestOffset.ToString("X") + 
						", size=" + bestStructSize + ", valid=" + bestValidPercent + "%");
					Console.WriteLine();
					Console.WriteLine("Try analyzing offset from -analyze command (335120 / 0x51D10):");
					Console.WriteLine("  LZS_inpack.exe -debug " + System.IO.Path.GetFileName(filePath) + " 335120");
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

