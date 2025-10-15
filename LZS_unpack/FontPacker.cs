using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LZS_unpack
{
	public class FontPacker
	{
		public class CharData
		{
			public int code;
			public float x, y, width, height;
			public float offsetX, offsetY, advanceX;
		}

		public static void PackFont(string fntPath, string ddsPath, string outputPhyre, string templatePhyre = null)
		{
			Console.WriteLine();
			Console.WriteLine("=== Packing Phyre Engine Font ===");
			Console.WriteLine("FNT:    " + Path.GetFileName(fntPath));
			Console.WriteLine("DDS:    " + Path.GetFileName(ddsPath));
			Console.WriteLine("Output: " + Path.GetFileName(outputPhyre));
			Console.WriteLine();

			if (!File.Exists(fntPath))
			{
				Console.WriteLine("ERROR: FNT file not found: " + fntPath);
				return;
			}

			if (!File.Exists(ddsPath))
			{
				Console.WriteLine("ERROR: DDS file not found: " + ddsPath);
				return;
			}

			// Parse FNT file
			Console.WriteLine("Parsing BMFont file...");
			List<CharData> chars = ParseBMFont(fntPath);
			Console.WriteLine("  Loaded " + chars.Count + " characters");

			// Load DDS texture
			Console.WriteLine("Loading DDS texture...");
			byte[] textureData;
			int texWidth, texHeight;
			LoadDDSTexture(ddsPath, out textureData, out texWidth, out texHeight);
			Console.WriteLine("  Texture: " + texWidth + "x" + texHeight + ", " + (textureData.Length / 1024) + " KB");

			// Try to find template (original .phyre file in same directory)
			if (templatePhyre == null)
			{
				string baseName = Path.GetFileNameWithoutExtension(fntPath).Replace("_extracted", "");
				string[] possibleTemplates = {
					baseName + ".phyre",
					baseName + ".fgen.phyre",
					"font00_usa.fgen.phyre" // fallback
				};
				
				foreach (string template in possibleTemplates)
				{
					if (File.Exists(template))
					{
						templatePhyre = template;
						Console.WriteLine("  Using template: " + Path.GetFileName(template));
						break;
					}
				}
			}

			// Pack into Phyre format
			Console.WriteLine();
			Console.WriteLine("Packing into Phyre format...");
			PackPhyreFont(chars, textureData, texWidth, texHeight, outputPhyre, templatePhyre);

			Console.WriteLine();
			Console.WriteLine("========================================");
			Console.WriteLine("PACKING COMPLETE!");
			Console.WriteLine("========================================");
			Console.WriteLine("Output: " + outputPhyre);
			Console.WriteLine("Size: " + (new FileInfo(outputPhyre).Length / 1024) + " KB");
			Console.WriteLine();
		}

		static List<CharData> ParseBMFont(string fntPath)
		{
			List<CharData> chars = new List<CharData>();
			string[] lines = File.ReadAllLines(fntPath);

			foreach (string line in lines)
			{
				if (line.StartsWith("char id="))
				{
					CharData ch = new CharData();
					
					// Parse: char id=33 x=1028 y=4 width=23 height=5 xoffset=22 yoffset=14 xadvance=0
					string[] parts = line.Split(new char[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
					
					for (int i = 0; i < parts.Length - 1; i++)
					{
						string key = parts[i];
						string value = parts[i + 1];
						
						if (key == "id") ch.code = int.Parse(value);
						else if (key == "x") ch.x = float.Parse(value);
						else if (key == "y") ch.y = float.Parse(value);
						else if (key == "width") ch.width = float.Parse(value);
						else if (key == "height") ch.height = float.Parse(value);
						else if (key == "xoffset") ch.offsetX = float.Parse(value);
						else if (key == "yoffset") ch.offsetY = float.Parse(value);
						else if (key == "xadvance") ch.advanceX = float.Parse(value);
					}
					
					chars.Add(ch);
				}
			}

			// Sort by code
			chars.Sort((a, b) => a.code.CompareTo(b.code));
			
			return chars;
		}

		static void LoadDDSTexture(string ddsPath, out byte[] data, out int width, out int height)
		{
			FileStream fs = new FileStream(ddsPath, FileMode.Open, FileAccess.Read);
			BinaryReader br = new BinaryReader(fs);
			
			try
			{
				// Read DDS header
				uint magic = br.ReadUInt32();
				if (magic != 0x20534444) // "DDS "
				{
					throw new Exception("Invalid DDS file (wrong magic)");
				}

				uint headerSize = br.ReadUInt32(); // 124
				uint flags = br.ReadUInt32();
				height = br.ReadInt32();
				width = br.ReadInt32();
				uint pitchOrLinearSize = br.ReadUInt32();
				uint depth = br.ReadUInt32();
				uint mipMapCount = br.ReadUInt32();

				// Skip reserved
				for (int i = 0; i < 11; i++)
				{
					br.ReadUInt32();
				}

				// Read pixel format
				uint pfSize = br.ReadUInt32();
				uint pfFlags = br.ReadUInt32();
				uint fourCC = br.ReadUInt32();
				uint rgbBitCount = br.ReadUInt32();
				uint rBitMask = br.ReadUInt32();
				uint gBitMask = br.ReadUInt32();
				uint bBitMask = br.ReadUInt32();
				uint aBitMask = br.ReadUInt32();

				// Skip caps
				for (int i = 0; i < 5; i++)
				{
					br.ReadUInt32();
				}

				// Check if this is L8 (luminance 8-bit)
				if ((pfFlags & 0x20000) == 0 || rgbBitCount != 8)
				{
					throw new Exception("DDS must be L8 format (8-bit Luminance). Current format not supported.");
				}

				// Read texture data (after 128-byte header)
				int dataSize = width * height;
				data = br.ReadBytes(dataSize);
				
				if (data.Length != dataSize)
				{
					throw new Exception("DDS file truncated or corrupted");
				}
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		static void PackPhyreFont(List<CharData> chars, byte[] textureData, int texWidth, int texHeight, string outputPath, string templatePath)
		{
			if (templatePath == null || !File.Exists(templatePath))
			{
				Console.WriteLine("ERROR: Template file not found!");
				Console.WriteLine("Place original .phyre file in the same directory for proper packing.");
				return;
			}

			// Read template file for metadata
			FileStream templateFs = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
			BinaryReader templateBr = new BinaryReader(templateFs);
			
			// Read entire template up to character data
			int charDataOffset = 3446;
			byte[] templateMetadata = templateBr.ReadBytes(charDataOffset);
			
			// Also need to copy texture header area (between chars and texture data)
			templateBr.BaseStream.Seek(charDataOffset + (chars.Count * 45), SeekOrigin.Begin);
			long textureHeaderStart = templateBr.BaseStream.Position;
			
			// Find where texture data starts in template (look for high-entropy data)
			long templateTexDataOffset = FindTextureDataInTemplate(templateBr);
			
			templateBr.BaseStream.Seek(textureHeaderStart, SeekOrigin.Begin);
			int texHeaderSize = (int)(templateTexDataOffset - textureHeaderStart);
			byte[] textureHeaderData = templateBr.ReadBytes(texHeaderSize);
			
			// Read trailing data after texture (important! ~1.2 MB)
			long afterTextureOffset = templateTexDataOffset + textureData.Length;
			templateBr.BaseStream.Seek(afterTextureOffset, SeekOrigin.Begin);
			byte[] trailingData = templateBr.ReadBytes((int)(templateFs.Length - afterTextureOffset));
			
			Console.WriteLine("  Template metadata: " + (templateMetadata.Length / 1024) + " KB");
			Console.WriteLine("  Texture header: " + texHeaderSize + " bytes");
			Console.WriteLine("  Trailing data: " + (trailingData.Length / 1024) + " KB");
			
			templateBr.Close();
			templateFs.Close();

			// Write output file
			FileStream fs = new FileStream(outputPath, FileMode.Create);
			BinaryWriter bw = new BinaryWriter(fs);

			try
			{
				Console.WriteLine("Copying metadata from template...");
				// Write template metadata (header + class defs + instance list + padding)
				bw.Write(templateMetadata);

				// Write character data
				Console.WriteLine("Writing " + chars.Count + " characters...");
				WriteCharacterData(bw, chars);

				// Write texture header from template
				Console.WriteLine("Writing texture header...");
				bw.Write(textureHeaderData);

				// Write texture data
				Console.WriteLine("Writing texture data (" + (textureData.Length / 1024) + " KB)...");
				bw.Write(textureData);

				// Write trailing data from template
				Console.WriteLine("Writing trailing data (" + (trailingData.Length / 1024) + " KB)...");
				bw.Write(trailingData);

				Console.WriteLine("Writing complete!");
			}
			finally
			{
				bw.Close();
				fs.Close();
			}
		}

		static long FindTextureDataInTemplate(BinaryReader br)
		{
			// Texture data has high entropy (many unique bytes)
			long start = br.BaseStream.Position;
			long fileSize = br.BaseStream.Length;
			
			// Align search to 256-byte boundaries
			long searchStart = (start / 256) * 256;
			
			for (long offset = searchStart; offset < fileSize - 4096; offset += 256)
			{
				br.BaseStream.Seek(offset, SeekOrigin.Begin);
				byte[] sample = br.ReadBytes(1024);
				
				int uniqueBytes = CountUniqueBytes(sample);
				if (uniqueBytes >= 100)
				{
					return offset;
				}
			}
			
			// Default fallback
			return start + 1000;
		}

		static int CountUniqueBytes(byte[] data)
		{
			bool[] seen = new bool[256];
			int count = 0;
			foreach (byte b in data)
			{
				if (!seen[b])
				{
					seen[b] = true;
					count++;
				}
			}
			return count;
		}


		static void WriteCharacterData(BinaryWriter bw, List<CharData> chars)
		{
			foreach (CharData ch in chars)
			{
				// Structure: code(4) + skip(12) + x,y,w,h(16) + offsets(12) + pad(1) = 45
				bw.Write(ch.code);
				
				// TextureX, TextureY, unknown (skip 12 bytes with reasonable values)
				bw.Write(2047.5f); // textureX
				bw.Write(507.0f);  // textureY
				bw.Write(0.0f);    // unknown
				
				// Coordinates
				bw.Write(ch.x);
				bw.Write(ch.y);
				bw.Write(ch.width);
				bw.Write(ch.height);
				
				// Offsets
				bw.Write(ch.offsetX);
				bw.Write(ch.offsetY);
				bw.Write(ch.advanceX);
				
				// Padding
				bw.Write((byte)0);
			}
		}

	}
}

