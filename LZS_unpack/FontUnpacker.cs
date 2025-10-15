using System;
using System.Globalization;
using System.IO;
using System.Text;
using APPLIB;

namespace LZS_unpack
{
	/// <summary>
	/// Unpacker for Phyre Engine font files
	/// </summary>
	internal class FontUnpacker
	{
		public static bool IsLikelyFontFile(string filePath)
		{
			string fileName = Path.GetFileName(filePath).ToLower();
			return fileName.Contains("font") || fileName.Contains("text");
		}

		public static void UnpackFont(string inputPath)
		{
			Console.WriteLine("Unpacking as FONT file: " + Path.GetFileName(inputPath));
			Console.WriteLine("Size: " + new FileInfo(inputPath).Length + " bytes");
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
				int objCount = br.ReadInt32();

				Console.WriteLine("Detected font file structure:");
				Console.WriteLine("  Objects: " + objCount);
				Console.WriteLine();

				// Jump to class definitions (following original algorithm)
				fs.Seek((long)(offset1 + 8), SeekOrigin.Begin);
				int num12 = br.ReadInt32();
				int numClasses = br.ReadInt32();
				int num14 = br.ReadInt32();

				Console.WriteLine("  Classes: " + numClasses);
				Console.WriteLine();

				// Export font info to text file
				string outputPath = Path.GetFileNameWithoutExtension(inputPath) + ".font.txt";
				StreamWriter sw = new StreamWriter(outputPath);

				sw.WriteLine("Phyre Engine Font File");
				sw.WriteLine("======================");
				sw.WriteLine("Source: " + Path.GetFileName(inputPath));
				sw.WriteLine("Size: " + fs.Length + " bytes");
				sw.WriteLine();
				sw.WriteLine("Structure:");
				sw.WriteLine("  Objects: " + objCount);
				sw.WriteLine("  Classes: " + numClasses);
				sw.WriteLine();

				// Skip array (num12 * 4 + 12) - following original code
				fs.Seek((long)(num12 * 4 + 12), SeekOrigin.Current);

				// Read class definitions to get name offsets
				sw.WriteLine("Classes:");
				int[] classNameOffsets = new int[numClasses];
				for (int i = 0; i < numClasses; i++)
				{
					br.ReadInt32(); // Field 1
					br.ReadInt32(); // Field 2
					classNameOffsets[i] = br.ReadInt32(); // Field 3 - NAME OFFSET
					br.ReadInt32(); // Field 4
					// Skip rest of class definition (20 bytes = 5 int32)
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
				}

				// Calculate string table start position (following original: position + num14 * 24)
				long stringTableStart = fs.Position + (long)(num14 * 24);
				
				for (int i = 0; i < numClasses; i++)
				{
					long namePos = stringTableStart + (long)classNameOffsets[i];
					fs.Seek(namePos, SeekOrigin.Begin);
					StringBuilder sb = new StringBuilder();
					byte b;
					int charCount = 0;
					while ((b = br.ReadByte()) > 0 && charCount < 100)
					{
						sb.Append((char)b);
						charCount++;
					}
					string className = sb.ToString();
					if (string.IsNullOrEmpty(className))
					{
						className = "(empty)";
					}
					sw.WriteLine("  [" + i + "] " + className);
				}
				sw.WriteLine();

				// Try to extract glyph data if present
				sw.WriteLine("Note: Font glyphs and texture data require specialized extraction.");
				sw.WriteLine("This is a basic structure dump. Full font rendering data not extracted.");

				sw.Close();

				Console.WriteLine("Font structure exported to: " + outputPath);
				Console.WriteLine();
				Console.WriteLine("Note: Font files contain glyph/texture data that cannot be");
				Console.WriteLine("      converted to standard 3D model formats (SMD/mesh.ascii).");
				Console.WriteLine("      Use specialized font tools for complete extraction.");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error unpacking font: " + ex.Message);
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}
	}
}

