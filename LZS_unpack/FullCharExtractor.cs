using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace LZS_unpack
{
	public class FullCharExtractor
	{
		public struct CharInfo
		{
			public float x, y, w, h;
			public int code;
			public float offsetX, offsetY, advanceX;
			public int page;
		}

		public static void ExtractWithStructure(string filePath, long offset, int count, int structSize)
		{
			Console.WriteLine();
			Console.WriteLine("=== Extracting Font Characters ===");
			Console.WriteLine("File: " + Path.GetFileName(filePath));
			Console.WriteLine("Offset: 0x" + offset.ToString("X") + " (" + offset + ")");
			Console.WriteLine("Count: " + count);
			Console.WriteLine("Structure size: " + structSize + " bytes");
			Console.WriteLine();

			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				fs.Seek(offset, SeekOrigin.Begin);
				
				CharInfo[] chars = new CharInfo[count];
				int validCount = 0;
				
				Console.WriteLine("Reading characters...");
				
				for (int i = 0; i < count && fs.Position < fs.Length - structSize; i++)
				{
					long charPos = fs.Position;
					
					try
					{
						// Read Pattern 1: code(int), then other fields
						CharInfo info = new CharInfo();
						info.code = br.ReadInt32();
						
						// For 45-byte structure, skip 12 bytes before coordinates
						if (structSize == 45)
						{
							fs.Seek(12, SeekOrigin.Current); // Skip textureX, textureY, unknown fields
						}
						
						// Read additional fields based on structure size
						if (structSize >= 20)
						{
							// Try reading x, y, w, h
							info.x = br.ReadSingle();
							info.y = br.ReadSingle();
							info.w = br.ReadSingle();
							info.h = br.ReadSingle();
							int bytesRead = structSize == 45 ? 32 : 20;
							
							// Read more fields if available
							if (structSize > bytesRead)
							{
								int remainingBytes = structSize - bytesRead;
								
								// Try to read as floats (offsetX, offsetY, advanceX, etc)
								if (remainingBytes >= 12)
								{
									info.offsetX = br.ReadSingle();
									info.offsetY = br.ReadSingle();
									info.advanceX = br.ReadSingle();
									bytesRead += 12;
									remainingBytes -= 12;
								}
								
								// Skip remaining bytes
								if (remainingBytes > 0)
								{
									fs.Seek(remainingBytes, SeekOrigin.Current);
								}
							}
						}
						else
						{
							// Just skip remaining bytes if structure is smaller than expected
							fs.Seek(structSize - 4, SeekOrigin.Current);
						}
						
						// Fix negative height (use absolute value)
						if (info.h < 0)
						{
							info.h = Math.Abs(info.h);
						}
						
						// Validate (relaxed for font data)
						bool valid = info.code >= 0 && info.code <= 0x10FFFF &&
						             info.x >= 0 && info.x <= 8192 &&
						             info.y >= 0 && info.y <= 8192 &&
						             info.w >= 0 && info.w <= 512 &&  // Allow width=0
						             info.h >= 0 && info.h <= 512;    // Now always positive
						
						if (valid)
						{
							chars[validCount++] = info;
							
							if (validCount <= 20)
							{
								Console.WriteLine("  Char #" + validCount + ": code=" + info.code + 
									" '" + GetCharDisplay(info.code) + "'" +
									", x=" + info.x + ", y=" + info.y + 
									", w=" + info.w + ", h=" + info.h +
									", offX=" + info.offsetX + ", offY=" + info.offsetY +
									", advX=" + info.advanceX);
							}
						}
						
						// Move to next char
						fs.Seek(charPos + structSize, SeekOrigin.Begin);
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error reading char " + i + ": " + ex.Message);
						break;
					}
				}
				
				Console.WriteLine();
				Console.WriteLine("Successfully read: " + validCount + " / " + count + " characters (" + 
					(validCount * 100 / count) + "%)");
				
				int invalidCount = count - validCount;
				if (invalidCount > 0 && invalidCount <= 20)
				{
					Console.WriteLine();
					Console.WriteLine("Invalid characters detected: " + invalidCount);
					Console.WriteLine("Showing invalid entries:");
					
					// Re-read to show invalid ones
					fs.Seek(offset, SeekOrigin.Begin);
					for (int i = 0; i < count && fs.Position < fs.Length - structSize; i++)
					{
						long charPos = fs.Position;
						try
						{
							CharInfo info = new CharInfo();
							info.code = br.ReadInt32();
							
							if (structSize == 45)
							{
								fs.Seek(12, SeekOrigin.Current);
							}
							
							if (structSize >= 20)
							{
								info.x = br.ReadSingle();
								info.y = br.ReadSingle();
								info.w = br.ReadSingle();
								info.h = br.ReadSingle();
							}
							
							// Fix negative height
							if (info.h < 0)
							{
								info.h = Math.Abs(info.h);
							}
							
							bool valid = info.code >= 0 && info.code <= 0x10FFFF &&
							             info.x >= 0 && info.x <= 8192 &&
							             info.y >= 0 && info.y <= 8192 &&
							             info.w >= 0 && info.w <= 512 &&
							             info.h >= 0 && info.h <= 512;
							
							if (!valid)
							{
								Console.WriteLine("  Index " + i + ": code=" + info.code + 
									", x=" + info.x + ", y=" + info.y + 
									", w=" + info.w + ", h=" + info.h);
							}
							
							fs.Seek(charPos + structSize, SeekOrigin.Begin);
						}
						catch
						{
							break;
						}
					}
				}
				
				if (validCount > 100)
				{
					// Export to BMFont format
					string baseName = Path.GetFileNameWithoutExtension(filePath);
					string fntPath = baseName + "_extracted.fnt";
					string jsonPath = baseName + "_extracted.json";
					
					ExportBMFont(chars, validCount, fntPath);
					ExportJSON(chars, validCount, jsonPath);
					
					Console.WriteLine();
					Console.WriteLine("Exported files:");
					Console.WriteLine("  " + fntPath);
					Console.WriteLine("  " + jsonPath);
				}
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		static string GetCharDisplay(int code)
		{
			if (code == 32) return "space";
			if (code < 32) return "?"; // Control characters
			
			// For valid Unicode characters, return the actual character
			try
			{
				return char.ConvertFromUtf32(code);
			}
			catch
			{
				return "?"; // Invalid Unicode code point
			}
		}

		static void ExportBMFont(CharInfo[] chars, int count, string outputPath)
		{
			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";
			
			StreamWriter sw = new StreamWriter(outputPath);
			
			sw.WriteLine("info face=\"PhyreFont\" size=32 bold=0 italic=0");
			sw.WriteLine("common lineHeight=32 base=26 scaleW=2048 scaleH=2048 pages=1");
			sw.WriteLine("page id=0 file=\"font_texture.png\"");
			sw.WriteLine("chars count=" + count);
			
			for (int i = 0; i < count; i++)
			{
				CharInfo c = chars[i];
				sw.WriteLine("char id=" + c.code + 
					" x=" + (int)c.x + 
					" y=" + (int)c.y + 
					" width=" + (int)c.w + 
					" height=" + (int)c.h + 
					" xoffset=" + (int)c.offsetX + 
					" yoffset=" + (int)c.offsetY + 
					" xadvance=" + (int)c.advanceX + 
					" page=0 chnl=15");
			}
			
			sw.Close();
		}

		static void ExportJSON(CharInfo[] chars, int count, string outputPath)
		{
			StreamWriter sw = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
			
			sw.WriteLine("{");
			sw.WriteLine("  \"font\": \"PhyreFont\",");
			sw.WriteLine("  \"charCount\": " + count + ",");
			sw.WriteLine("  \"characters\": [");
			
			for (int i = 0; i < count; i++)
			{
				CharInfo c = chars[i];
				string charDisplay = GetCharDisplay(c.code);
				
				// Escape JSON special characters
				charDisplay = charDisplay.Replace("\\", "\\\\")
				                       .Replace("\"", "\\\"")
				                       .Replace("\n", "\\n")
				                       .Replace("\r", "\\r")
				                       .Replace("\t", "\\t");
				
				sw.Write("    { \"code\": " + c.code + 
					", \"char\": \"" + charDisplay + "\"" +
					", \"x\": " + c.x + 
					", \"y\": " + c.y + 
					", \"w\": " + c.w + 
					", \"h\": " + c.h + 
					", \"offsetX\": " + c.offsetX + 
					", \"offsetY\": " + c.offsetY + 
					", \"advanceX\": " + c.advanceX + 
					" }");
				
				if (i < count - 1)
					sw.WriteLine(",");
				else
					sw.WriteLine();
			}
			
			sw.WriteLine("  ]");
			sw.WriteLine("}");
			
			sw.Close();
		}
	}
}

