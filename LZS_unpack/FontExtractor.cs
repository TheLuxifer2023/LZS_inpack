using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LZS_unpack
{
	/// <summary>
	/// Full extractor for Phyre Engine bitmap fonts
	/// </summary>
	internal class FontExtractor
	{
		public class BitmapFontChar
		{
			public int CharCode;
			public float X;
			public float Y;
			public float Width;
			public float Height;
			public float OffsetX;
			public float OffsetY;
			public float AdvanceX;
			public int Page;
		}

		public static void ExtractFont(string inputPath)
		{
			Console.WriteLine("=== FULL Font Extraction ===");
			Console.WriteLine("File: " + Path.GetFileName(inputPath));
			Console.WriteLine();

			FileStream fs = new FileStream(inputPath, FileMode.Open);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				// Read header
				br.ReadInt32(); // Magic
				int offset1 = br.ReadInt32();
				int offset2 = br.ReadInt32();
				br.ReadInt32();

				// Jump to class definitions
				fs.Seek((long)(offset1 + 8), SeekOrigin.Begin);
				int num12 = br.ReadInt32();
				int numClasses = br.ReadInt32();
				int num14 = br.ReadInt32();

				Console.WriteLine("Classes found: " + numClasses);

				// Skip array
				fs.Seek((long)(num12 * 4 + 12), SeekOrigin.Current);

				// Read class definitions
				int[] classNameOffsets = new int[numClasses];
				for (int i = 0; i < numClasses; i++)
				{
					br.ReadInt32();
					br.ReadInt32();
					classNameOffsets[i] = br.ReadInt32();
					for (int j = 0; j < 6; j++) br.ReadInt32();
				}

				// Read class names
				long stringTableStart = fs.Position + (long)(num14 * 24);
				string[] classNames = new string[numClasses];
				for (int i = 0; i < numClasses; i++)
				{
					fs.Seek(stringTableStart + (long)classNameOffsets[i], SeekOrigin.Begin);
					StringBuilder sb = new StringBuilder();
					byte b;
					while ((b = br.ReadByte()) > 0)
					{
						sb.Append((char)b);
					}
					classNames[i] = sb.ToString();
				}

				// Find important classes
				int bitmapFontIndex = -1;
				int charInfoIndex = -1;
				int textureIndex = -1;

				for (int i = 0; i < numClasses; i++)
				{
					if (classNames[i] == "PBitmapFont")
						bitmapFontIndex = i;
					else if (classNames[i] == "PBitmapFontCharInfo")
						charInfoIndex = i;
					else if (classNames[i] == "PTexture2D")
						textureIndex = i;
				}

				Console.WriteLine("PBitmapFont class: " + bitmapFontIndex);
				Console.WriteLine("PBitmapFontCharInfo class: " + charInfoIndex);
				Console.WriteLine("PTexture2D class: " + textureIndex);
				Console.WriteLine();

				// Read object instances
				fs.Seek((long)(offset1 + offset2), SeekOrigin.Begin);

				int charInfoCount = 0;
				long charInfoOffset = 0;

				// Find character info data
				for (int i = 0; i < 50; i++) // Read first 50 instances
				{
					long instancePos = fs.Position;
					int classId = br.ReadInt32() - 1;
					int count = br.ReadInt32();
					int dataOffset = br.ReadInt32();

					if (classId == charInfoIndex)
					{
						charInfoCount = count;
						charInfoOffset = dataOffset;
						Console.WriteLine("Found character data: " + charInfoCount + " chars at offset " + dataOffset);
					}

					// Skip rest of instance
					for (int j = 0; j < 6; j++) br.ReadInt32();

					if (fs.Position >= fs.Length - 100)
						break;
				}

				// Extract character information
				if (charInfoCount > 0 && charInfoOffset > 0)
				{
					ExtractCharacterData(fs, br, charInfoOffset, charInfoCount, inputPath);
				}
				else
				{
					Console.WriteLine("WARNING: Could not find character data!");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error extracting font: " + ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
			finally
			{
				br.Close();
				fs.Close();
			}

			// Extract textures automatically (AFTER closing the file)
			try
			{
				Console.WriteLine();
				DDSExtractor.FindAndExtractTextures(inputPath, Path.GetFileNameWithoutExtension(inputPath));
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error extracting textures: " + ex.Message);
			}
		}

		private static void ExtractCharacterData(FileStream fs, BinaryReader br, long offset, int count, string inputPath)
		{
			Console.WriteLine();
			Console.WriteLine("=== Extracting Character Data ===");
			Console.WriteLine("Characters: " + count);
			Console.WriteLine("Starting offset: 0x" + offset.ToString("X"));

			List<BitmapFontChar> chars = new List<BitmapFontChar>();

			// Jump to character data
			fs.Seek(offset, SeekOrigin.Begin);

			// PBitmapFontCharInfo structure
			// Try different structure sizes
			int structSize = 32; // 8 int32 fields
			int validChars = 0;
			int invalidChars = 0;

			for (int i = 0; i < count && fs.Position < fs.Length - structSize; i++)
			{
				long charPos = fs.Position;
				
				try
				{
					BitmapFontChar ch = new BitmapFontChar();

					// Try reading common bitmap font char structure
					ch.CharCode = br.ReadInt32();
					ch.X = br.ReadSingle();
					ch.Y = br.ReadSingle();
					ch.Width = br.ReadSingle();
					ch.Height = br.ReadSingle();
					ch.OffsetX = br.ReadSingle();
					ch.OffsetY = br.ReadSingle();
					ch.AdvanceX = br.ReadSingle();
					ch.Page = 0;

					// Validate character data
					bool isValid = ch.CharCode >= 0 && ch.CharCode < 0x10FFFF &&
					               ch.X >= 0 && ch.X <= 8192 &&
					               ch.Y >= 0 && ch.Y <= 8192 &&
					               ch.Width >= 0 && ch.Width <= 512 &&
					               ch.Height >= 0 && ch.Height <= 512;

					if (isValid)
					{
						chars.Add(ch);
						validChars++;
					}
					else
					{
						invalidChars++;
						// Show debug info for first few invalid chars
						if (invalidChars <= 5)
						{
							Console.WriteLine("  Invalid char at index " + i + 
								": code=" + ch.CharCode + 
								", x=" + ch.X + ", y=" + ch.Y + 
								", w=" + ch.Width + ", h=" + ch.Height);
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("  Error reading char at index " + i + ": " + ex.Message);
					break;
				}
			}

			Console.WriteLine("Successfully read: " + chars.Count + " characters");
			Console.WriteLine("Valid: " + validChars + ", Invalid: " + invalidChars);

			// If too many invalid chars, suggest debugging
			if (invalidChars > validChars)
			{
				Console.WriteLine();
				Console.WriteLine("========================================");
				Console.WriteLine("WARNING: More invalid than valid characters!");
				Console.WriteLine("========================================");
				Console.WriteLine("The PBitmapFontCharInfo structure might be different.");
				Console.WriteLine();
				Console.WriteLine("To find correct structure, run:");
				Console.WriteLine("  LZS_inpack.exe -debug \"" + Path.GetFileName(inputPath) + "\" " + offset);
				Console.WriteLine();
				Console.WriteLine("This will test different structure sizes and show");
				Console.WriteLine("which one gives the most valid characters.");
				Console.WriteLine("========================================");
				Console.WriteLine();
			}

			// Export to BMFont format (even with partial data)
			ExportBMFont(chars, inputPath);

			// Export to JSON
			ExportJSON(chars, inputPath);
		}

		private static void ExportBMFont(List<BitmapFontChar> chars, string inputPath)
		{
			string outputPath = Path.GetFileNameWithoutExtension(inputPath) + ".fnt";
			StreamWriter sw = new StreamWriter(outputPath);
			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";

			sw.WriteLine("info face=\"" + Path.GetFileNameWithoutExtension(inputPath) + "\" size=32 bold=0 italic=0");
			sw.WriteLine("common lineHeight=32 base=26 scaleW=2048 scaleH=2048 pages=1");
			sw.WriteLine("page id=0 file=\"" + Path.GetFileNameWithoutExtension(inputPath) + ".png\"");
			sw.WriteLine("chars count=" + chars.Count);

			foreach (var ch in chars)
			{
				sw.WriteLine(string.Format(nfi,
					"char id={0} x={1} y={2} width={3} height={4} xoffset={5} yoffset={6} xadvance={7} page={8} chnl=15",
					ch.CharCode,
					(int)ch.X, (int)ch.Y,
					(int)ch.Width, (int)ch.Height,
					(int)ch.OffsetX, (int)ch.OffsetY,
					(int)ch.AdvanceX,
					ch.Page
				));
			}

			sw.Close();

			Console.WriteLine("Exported BMFont format: " + outputPath);
		}

		private static void ExportJSON(List<BitmapFontChar> chars, string inputPath)
		{
			string outputPath = Path.GetFileNameWithoutExtension(inputPath) + "_chars.json";
			StreamWriter sw = new StreamWriter(outputPath);
			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";

			sw.WriteLine("{");
			sw.WriteLine("  \"font\": \"" + Path.GetFileNameWithoutExtension(inputPath) + "\",");
			sw.WriteLine("  \"charCount\": " + chars.Count + ",");
			sw.WriteLine("  \"characters\": [");

			for (int i = 0; i < chars.Count; i++)
			{
				var ch = chars[i];
				sw.Write("    {");
				sw.Write("\"code\": " + ch.CharCode + ", ");
				sw.Write("\"char\": \"");
				
				// Safe character output
				if (ch.CharCode >= 32 && ch.CharCode < 127)
					sw.Write((char)ch.CharCode);
				else
					sw.Write("\\u" + ch.CharCode.ToString("X4"));
				
				sw.Write("\", ");
				sw.Write(string.Format(nfi, "\"x\": {0}, \"y\": {1}, \"w\": {2}, \"h\": {3}, \"xoff\": {4}, \"yoff\": {5}, \"adv\": {6}",
					ch.X, ch.Y, ch.Width, ch.Height, ch.OffsetX, ch.OffsetY, ch.AdvanceX));
				sw.Write("}");
				
				if (i < chars.Count - 1)
					sw.WriteLine(",");
				else
					sw.WriteLine();
			}

			sw.WriteLine("  ]");
			sw.WriteLine("}");
			sw.Close();

			Console.WriteLine("Exported JSON format: " + outputPath);
		}

	}
}

