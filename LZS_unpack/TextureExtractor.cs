using System;
using System.IO;
using System.Text;

namespace LZS_unpack
{
	public class TextureExtractor
	{
		public static void ExtractTexture(string filePath)
		{
			Console.WriteLine();
			Console.WriteLine("=== Extracting Phyre Engine Texture ===");
			Console.WriteLine("File: " + Path.GetFileName(filePath));
			Console.WriteLine();

			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				// First, find PTexture2D instance using same logic as FontDataFinder
				br.ReadInt32(); // magic
				int num = br.ReadInt32(); // offset1
				int num2 = br.ReadInt32(); // offset2
				br.ReadInt32();
				int num3 = br.ReadInt32(); // count1 (objects)
				
				// Skip to class definitions
				fs.Seek((long)(num + 8), SeekOrigin.Begin);
				int num12 = br.ReadInt32();
				int num13 = br.ReadInt32(); // num classes
				int num14 = br.ReadInt32(); // num instances
				
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
				
				long dataStart = fs.Position + (long)(num3 * 36);
				
				for (int i = 0; i < num3; i++)
				{
					int instanceClass = br.ReadInt32() - 1;
					int instanceCount = br.ReadInt32();
					int instanceSize = br.ReadInt32();
					long instanceAbsOffset = dataStart;
					dataStart += (long)instanceSize;
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					
					string className = instanceClass >= 0 && instanceClass < classNames.Length ? 
						classNames[instanceClass] : "UNKNOWN";
					
					if (className == "PTexture2D")
					{
						Console.WriteLine("Found PTexture2D at offset: 0x" + instanceAbsOffset.ToString("X") + " (" + instanceAbsOffset + ")");
						Console.WriteLine("Size: " + instanceSize + " bytes");
						Console.WriteLine();
						
						// Parse texture structure
						ParseTexture(fs, br, instanceAbsOffset, instanceSize, filePath);
						return;
					}
				}
				
				Console.WriteLine("ERROR: PTexture2D not found in file!");
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		static void ParseTexture(FileStream fs, BinaryReader br, long offset, int size, string filePath)
		{
			fs.Seek(offset, SeekOrigin.Begin);
			
			Console.WriteLine("=== Parsing Texture Structure ===");
			Console.WriteLine();
			
			// Read texture header as shorts to catch dimensions
			ushort s1 = br.ReadUInt16();
			ushort s2 = br.ReadUInt16();
			ushort s3 = br.ReadUInt16();
			ushort s4 = br.ReadUInt16();
			ushort s5 = br.ReadUInt16();
			ushort s6 = br.ReadUInt16();
			ushort s7 = br.ReadUInt16();
			ushort s8 = br.ReadUInt16();
			ushort s9 = br.ReadUInt16();
			ushort s10 = br.ReadUInt16();
			ushort s11 = br.ReadUInt16();
			
			Console.WriteLine("Header fields (as shorts):");
			Console.WriteLine("  s1=" + s1 + ", s2=" + s2 + ", s3=" + s3 + ", s4=" + s4);
			Console.WriteLine("  s5=" + s5 + ", s6=" + s6 + ", s7=" + s7 + ", s8=" + s8);
			Console.WriteLine("  s9=" + s9 + ", s10=" + s10 + ", s11=" + s11);
			Console.WriteLine();
			
			// Look for format string (e.g., "L8", "DXT1", "DXT5")
			long startPos = fs.Position;
			byte[] searchData = new byte[100];
			fs.Read(searchData, 0, 100);
			
			string formatStr = "";
			int formatIdx = -1;
			for (int i = 0; i < searchData.Length - 4; i++)
			{
				if (searchData[i] == 'L' && searchData[i + 1] == '8')
				{
					formatStr = "L8";
					formatIdx = i;
					break;
				}
				if (searchData[i] == 'D' && searchData[i + 1] == 'X' && searchData[i + 2] == 'T')
				{
					formatStr = "DXT" + (char)searchData[i + 3];
					formatIdx = i;
					break;
				}
			}
			
			if (formatStr != "")
			{
				Console.WriteLine("Texture format found: " + formatStr + " at offset +" + formatIdx);
				Console.WriteLine();
				
				// Try to use s8 as width/height hint
				int hintSize = s8;
				Console.WriteLine("Size hint from header: " + hintSize);
				
				// Check if this is power of 2
				if (IsPowerOfTwo(hintSize) && hintSize >= 256 && hintSize <= 4096)
				{
					Console.WriteLine("Valid texture dimension found: " + hintSize);
					Console.WriteLine();
					
					// Try square texture first
					int texWidth = hintSize;
					int texHeight = hintSize;
					int expectedSize = formatStr == "L8" ? texWidth * texHeight : 
						CalculateCompressedSize(texWidth, texHeight, formatStr);
					
					Console.WriteLine("Testing " + texWidth + "x" + texHeight + " (" + (expectedSize / 1024) + " KB)...");
					
					// Search for data block starting AFTER current position
					long dataOffset = FindDataBlockAfter(fs, br, offset + 100, expectedSize);
					if (dataOffset > 0)
					{
						Console.WriteLine();
						Console.WriteLine("========================================");
						Console.WriteLine("FOUND TEXTURE DATA!");
						Console.WriteLine("========================================");
						Console.WriteLine("Dimensions: " + texWidth + "x" + texHeight);
						Console.WriteLine("Data size: " + expectedSize + " bytes (" + (expectedSize / 1024) + " KB)");
						Console.WriteLine("Data offset: 0x" + dataOffset.ToString("X"));
						Console.WriteLine();
						
						ExtractDDS(fs, br, dataOffset, texWidth, texHeight, formatStr, filePath);
						return;
					}
				}
				
				Console.WriteLine("Could not find texture data block.");
				Console.WriteLine("The texture data might be compressed or in a different location.");
			}
			else
			{
				Console.WriteLine("Texture format string not found.");
			}
			
			Console.WriteLine("Could not determine texture dimensions automatically.");
			Console.WriteLine("Try manual extraction with QuickBMS or Noesis.");
		}

		static long FindDataBlockAfter(FileStream fs, BinaryReader br, long startOffset, int expectedSize)
		{
			long fileSize = fs.Length;
			
			Console.WriteLine("  Searching for " + (expectedSize / 1024) + " KB data block after offset 0x" + startOffset.ToString("X") + "...");
			
			// Search from startOffset forward, aligned to 256-byte boundaries
			long searchStart = (startOffset / 256) * 256;
			
			for (long offset = searchStart; offset < fileSize - expectedSize; offset += 256)
			{
				// Skip offset 0 (header)
				if (offset == 0)
					continue;
				
				fs.Seek(offset, SeekOrigin.Begin);
				
				// Check if this looks like texture data (has variety of bytes)
				byte[] sample = br.ReadBytes(Math.Min(4096, expectedSize));
				int uniqueBytes = CountUniqueBytes(sample);
				
				// For font textures (L8), lower threshold (fonts have less variety than photos)
				// For L8: at least 80 unique byte values
				int minUnique = expectedSize > 1000000 ? 150 : 80;
				
				if (uniqueBytes >= minUnique)
				{
					// Verify we have enough data
					if (offset + expectedSize <= fileSize)
					{
						// Additional check: not all zeros
						bool notAllZeros = false;
						int nonZeroCount = 0;
						for (int i = 0; i < Math.Min(256, sample.Length); i++)
						{
							if (sample[i] != 0)
							{
								nonZeroCount++;
							}
						}
						notAllZeros = nonZeroCount > 100;
						
						if (notAllZeros)
						{
							Console.WriteLine("  Found candidate at offset 0x" + offset.ToString("X") + 
								" (unique bytes: " + uniqueBytes + ", non-zero: " + nonZeroCount + ")");
							return offset;
						}
					}
				}
			}
			
			return -1;
		}

		static int CountUniqueBytes(byte[] data)
		{
			bool[] seen = new bool[256];
			int count = 0;
			
			for (int i = 0; i < data.Length; i++)
			{
				if (!seen[data[i]])
				{
					seen[data[i]] = true;
					count++;
				}
			}
			
			return count;
		}

		static bool IsPowerOfTwo(int x)
		{
			return x > 0 && (x & (x - 1)) == 0;
		}

		static void ExtractDDS(FileStream fs, BinaryReader br, long dataOffset, int width, int height, string format, string sourceFile)
		{
			Console.WriteLine();
			Console.WriteLine("=== Extracting Texture ===");
			Console.WriteLine("Format: " + format);
			Console.WriteLine("Size: " + width + "x" + height);
			Console.WriteLine("Data offset: 0x" + dataOffset.ToString("X"));
			Console.WriteLine();
			
			string baseName = Path.GetFileNameWithoutExtension(sourceFile);
			
			// Определяем оригинальный формат текстуры
			TextureFormatConverter.TextureFormat detectedFormat = TextureFormatConverter.TextureFormat.DDS; // По умолчанию DDS
			
			// Читаем первые байты данных для определения формата
			long currentPos = fs.Position;
			fs.Seek(dataOffset, SeekOrigin.Begin);
			byte[] formatBytes = br.ReadBytes(16);
			detectedFormat = TextureFormatConverter.DetectTextureFormat(formatBytes);
			fs.Seek(currentPos, SeekOrigin.Begin);
			
			// Если не удалось определить по данным, используем информацию из FormatDetector
			if (detectedFormat == TextureFormatConverter.TextureFormat.Unknown)
			{
				Console.WriteLine("Could not detect format from texture data, checking file signatures...");
				
				// Проверяем файл на наличие GTF сигнатур по частям
				bool foundGTF = false;
				long gtfOffset = -1;
				fs.Seek(0, SeekOrigin.Begin);
				
				// Читаем файл блоками по 64KB для поиска GTF сигнатуры
				byte[] buffer = new byte[65536]; // 64KB buffer
				int bytesRead;
				long currentFilePos = 0;
				while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
				{
					// Ищем GTF сигнатуру в текущем буфере
					for (int i = 0; i <= bytesRead - 4; i++)
					{
						if (buffer[i] == 0x04 && buffer[i+1] == 0x01 && buffer[i+2] == 0x00 && buffer[i+3] == 0x00)
						{
							foundGTF = true;
							gtfOffset = currentFilePos + i;
							break;
						}
					}
					if (foundGTF) break;
					currentFilePos += bytesRead;
				}
				
				if (foundGTF)
				{
					detectedFormat = TextureFormatConverter.TextureFormat.GTF;
					Console.WriteLine($"Found GTF signature in file at offset 0x{gtfOffset:X} ({gtfOffset}) - using GTF format");
				}
				else
				{
					Console.WriteLine("No GTF signature found - using DDS format");
					detectedFormat = TextureFormatConverter.TextureFormat.DDS;
				}
			}
			
			Console.WriteLine("Detected texture format: " + detectedFormat);
			Console.WriteLine();
			
			// Создаем имя файла с правильным расширением
			string extension = TextureFormatConverter.GetRecommendedExtension(detectedFormat);
			string outputPath = baseName + "_texture" + extension;
			
			FileStream outFs = new FileStream(outputPath, FileMode.Create);
			BinaryWriter bw = new BinaryWriter(outFs);
			
			try
			{
				// Сохраняем в оригинальном формате
				if (detectedFormat == TextureFormatConverter.TextureFormat.GTF)
				{
					// Для GTF просто копируем сырые данные
					fs.Seek(dataOffset, SeekOrigin.Begin);
					byte[] textureData = br.ReadBytes(CalculateCompressedSize(width, height, format));
					bw.Write(textureData);
				}
				else
				{
					// Для DDS пишем заголовок
					WriteDDSHeader(bw, width, height, format);
					
					// Копируем данные текстуры
					fs.Seek(dataOffset, SeekOrigin.Begin);
					int dataSize = format == "L8" ? width * height : CalculateCompressedSize(width, height, format);
					byte[] textureData = br.ReadBytes(dataSize);
					bw.Write(textureData);
				}
				
				Console.WriteLine("Successfully extracted texture!");
				Console.WriteLine("Output file: " + outputPath);
				Console.WriteLine("Size: " + (new FileInfo(outputPath).Length / 1024) + " KB");
				
				// Auto-convert to PNG
				string pngPath = baseName + "_texture.png";
				Console.WriteLine();
				Console.WriteLine("Auto-converting to PNG...");
				
				bw.Close();
				outFs.Close();
				
				// Используем универсальный конвертер
				try
				{
					TextureFormatConverter.ConvertToPNG(outputPath, pngPath);
					Console.WriteLine("Both original and PNG files are ready!");
					Console.WriteLine("  Original: " + outputPath);
					Console.WriteLine("  PNG: " + pngPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("PNG conversion failed: " + ex.Message);
					Console.WriteLine("Original file saved: " + outputPath);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				throw;
			}
			finally
			{
				if (bw != null && outFs != null)
				{
					try { bw.Close(); } catch { }
					try { outFs.Close(); } catch { }
				}
			}
		}

		static void WriteDDSHeader(BinaryWriter bw, int width, int height, string format)
		{
			// DDS magic
			bw.Write(0x20534444); // "DDS "
			
			// DDS_HEADER
			bw.Write(124); // dwSize
			bw.Write(0x1 | 0x2 | 0x4 | 0x1000); // dwFlags: CAPS | HEIGHT | WIDTH | PIXELFORMAT
			bw.Write(height); // dwHeight
			bw.Write(width); // dwWidth
			bw.Write(width); // dwPitchOrLinearSize (for L8: width)
			bw.Write(0); // dwDepth
			bw.Write(0); // dwMipMapCount
			
			// dwReserved1[11]
			for (int i = 0; i < 11; i++)
			{
				bw.Write(0);
			}
			
			// DDS_PIXELFORMAT
			bw.Write(32); // dwSize
			
			if (format == "L8")
			{
				// Luminance format
				bw.Write(0x20000); // dwFlags: DDPF_LUMINANCE
				bw.Write(0); // dwFourCC
				bw.Write(8); // dwRGBBitCount
				bw.Write(0xFF); // dwRBitMask
				bw.Write(0); // dwGBitMask
				bw.Write(0); // dwBBitMask
				bw.Write(0); // dwABitMask
			}
			else if (format.StartsWith("DXT"))
			{
				// Compressed format
				bw.Write(0x4); // dwFlags: DDPF_FOURCC
				byte[] fourCC = Encoding.ASCII.GetBytes(format + "\0");
				bw.Write(fourCC[0]);
				bw.Write(fourCC[1]);
				bw.Write(fourCC[2]);
				bw.Write(fourCC[3]);
				bw.Write(0); // dwRGBBitCount
				bw.Write(0); // dwRBitMask
				bw.Write(0); // dwGBitMask
				bw.Write(0); // dwBBitMask
				bw.Write(0); // dwABitMask
			}
			
			// DDS_HEADER dwCaps
			bw.Write(0x1000); // dwCaps: DDSCAPS_TEXTURE
			bw.Write(0); // dwCaps2
			bw.Write(0); // dwCaps3
			bw.Write(0); // dwCaps4
			bw.Write(0); // dwReserved2
		}

		static int CalculateCompressedSize(int width, int height, string format)
		{
			int blockSize = format == "DXT1" ? 8 : 16;
			int blocksWide = Math.Max(1, width / 4);
			int blocksHigh = Math.Max(1, height / 4);
			return blocksWide * blocksHigh * blockSize;
		}
	}
}

