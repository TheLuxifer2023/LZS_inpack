using System;
using System.Globalization;
using System.IO;
using APPLIB;

namespace LZS_unpack
{
	// Token: 0x0200000B RID: 11
	internal class Program
	{
		// Token: 0x0600006A RID: 106 RVA: 0x0000446C File Offset: 0x0000266C
		private static void ShowUsage()
		{
			Console.WriteLine("LZS Phyre Engine Pack/Unpack Tool v2.7");
			Console.WriteLine("========================================");
			Console.WriteLine("");
			Console.WriteLine("Usage:");
			Console.WriteLine("  Unpack:    LZS_inpack.exe <input.phyre>");
			Console.WriteLine("  Pack:      LZS_inpack.exe -pack <input.smd> <input.mesh.ascii> <output.phyre>");
			Console.WriteLine("  PackFont:  LZS_inpack.exe -packfont <input.fnt> <input.texture> <output.phyre>  ✨ Updated!");
			Console.WriteLine("  Analyze:   LZS_inpack.exe -analyze <input.phyre>");
			Console.WriteLine("  Texture:   LZS_inpack.exe -texture <font.phyre>  (Extract texture with auto-format detection) ✨ Updated!");
			Console.WriteLine("  Verify:    LZS_inpack.exe -verify <packed.phyre> [original.phyre]  (Verify packed file quality) ✨ NEW!");
			Console.WriteLine("  Detect:    LZS_inpack.exe -detect <file>  (Auto-detect file format & content type) ✨ Enhanced!");
			Console.WriteLine("  Web UI:    LZS_inpack.exe -startweb  (Start web interface on http://localhost:5000) ✨ NEW!");
			Console.WriteLine("");
			Console.WriteLine("Font Analysis & Extraction:");
			Console.WriteLine("  Extract:   LZS_inpack.exe -extract <font.phyre>  (Auto-extract font - may not find all chars)");
			Console.WriteLine("  ExtractChar: LZS_inpack.exe -extractchar <font.phyre> <offset> <count> <size>  (Precise extraction)");
			Console.WriteLine("  Debug:     LZS_inpack.exe -debug <font.phyre> <offset>  (Hex dump & structure analysis)");
			Console.WriteLine("  FindChars: LZS_inpack.exe -findchars <font.phyre> <expected_count>  (Auto-find character data)");
			Console.WriteLine("  AnalyzeChar: LZS_inpack.exe -analyzechar <font.phyre> <offset> <count>  (Deep char structure analysis)");
			Console.WriteLine("  FindData:  LZS_inpack.exe -finddata <font.phyre>  (Find absolute offset for font data)");
			Console.WriteLine("  FindSize:  LZS_inpack.exe -findsize <font.phyre> <offset>  (Find structure size from sequential codes)");
			Console.WriteLine("");
			Console.WriteLine("Texture Conversion:");
			Console.WriteLine("  ToPNG:     LZS_inpack.exe -topng <input.dds/gtf> <output.png>  (Convert to PNG) ✨ Updated!");
			Console.WriteLine("  ToDDS:     LZS_inpack.exe -todds <input.png> <output.dds>  (Convert PNG to DDS L8)");
			Console.WriteLine("");
			Console.WriteLine("Supported Texture Formats:");
			Console.WriteLine("  GTF (Sony GTF) - Original Phyre Engine format ✨ NEW!");
			Console.WriteLine("  DDS (DirectDraw Surface) - Standard format");
			Console.WriteLine("  PNG (Portable Network Graphics) - For editing");
			Console.WriteLine("");
			Console.WriteLine("Examples:");
			Console.WriteLine("  LZS_inpack.exe model.phyre");
			Console.WriteLine("  LZS_inpack.exe -pack model.smd model.mesh.ascii model_new.phyre");
			Console.WriteLine("  LZS_inpack.exe -packfont font.fnt texture.gtf font_new.phyre  ✨ GTF support!");
			Console.WriteLine("  LZS_inpack.exe -analyze font.phyre");
			Console.WriteLine("  LZS_inpack.exe -extract font00_usa.fgen.phyre  # Auto-extract (may miss some chars)");
			Console.WriteLine("  LZS_inpack.exe -extractchar font00_usa.fgen.phyre 3446 7447 45  # Precise extraction");
			Console.WriteLine("  LZS_inpack.exe -debug font.phyre 335120");
			Console.WriteLine("  LZS_inpack.exe -findchars font00_usa.fgen.phyre 7447");
			Console.WriteLine("  LZS_inpack.exe -analyzechar font00_usa.fgen.phyre 335120 7447");
			Console.WriteLine("  LZS_inpack.exe -finddata font00_usa.fgen.phyre");
			Console.WriteLine("  LZS_inpack.exe -detect unknown_file.bin  ✨ Enhanced detection!");
			Console.WriteLine("  LZS_inpack.exe -detect font00_usa.fgen.phyre  ✨ Detects: PhyreFont + GTF!");
			Console.WriteLine("  LZS_inpack.exe -verify font_new.phyre font_original.phyre  ✨ Verify quality!");
			Console.WriteLine("  LZS_inpack.exe -startweb  ✨ Launch web interface!");
		}

		private static Quaternion3D matrix2quat(double[,] m)
		{
			int[] array = new int[3];
			array[0] = 1;
			array[1] = 2;
			int[] array2 = array;
			Quaternion3D quaternion3D = new Quaternion3D();
			double[] array3 = new double[4];
			double num = m[0, 0] + m[1, 1] + m[2, 2];
			if (num > 0.0)
			{
				double num2 = Math.Pow(num + 1.0, 0.5);
				quaternion3D.real = (float)(num2 / 2.0);
				num2 = 0.5 / num2;
				quaternion3D.i = (float)((m[1, 2] - m[2, 1]) * num2);
				quaternion3D.j = (float)((m[2, 0] - m[0, 2]) * num2);
				quaternion3D.k = (float)((m[0, 1] - m[1, 0]) * num2);
			}
			else
			{
				int num3 = 0;
				if (m[1, 1] > m[0, 0])
				{
					num3 = 1;
				}
				if (m[2, 2] > m[num3, num3])
				{
					num3 = 2;
				}
				int num4 = array2[num3];
				int num5 = array2[num4];
				double num2 = Math.Pow(m[num3, num3] - (m[num4, num4] + m[num5, num5]) + 1.0, 0.5);
				array3[num3] = num2 * 0.5;
				if (num2 != 0.0)
				{
					num2 = 0.5 / num2;
				}
				array3[3] = (m[num4, num5] - m[num5, num4]) * num2;
				array3[num4] = (m[num3, num4] + m[num4, num3]) * num2;
				array3[num5] = (m[num3, num5] + m[num5, num3]) * num2;
				quaternion3D.i = (float)array3[0];
				quaternion3D.j = (float)array3[1];
				quaternion3D.k = (float)array3[2];
				quaternion3D.real = (float)array3[3];
			}
			return quaternion3D;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x0000466C File Offset: 0x0000286C
		private static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				ShowUsage();
				return;
			}

			// Check if web interface mode
			if (args.Length >= 1 && args[0] == "-startweb")
			{
				try
				{
					StartWebInterface();
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error starting web interface: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if format detection mode
			if (args.Length >= 2 && args[0] == "-detect")
			{
				try
				{
					string inputPath = args[1];
					
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					FormatDetector.FormatInfo formatInfo = FormatDetector.DetectFormat(inputPath);
					
					Console.WriteLine("=== File Format Detection ===");
					Console.WriteLine("File: " + Path.GetFileName(inputPath));
					Console.WriteLine("Size: " + new FileInfo(inputPath).Length + " bytes");
					Console.WriteLine();
					Console.WriteLine("Detected Format: " + formatInfo.Format);
					Console.WriteLine("Description: " + formatInfo.Description);
					Console.WriteLine("Recommended Extension: " + formatInfo.RecommendedExtension);
					Console.WriteLine("Is Valid: " + (formatInfo.IsValid ? "Yes" : "No"));
					
					if (formatInfo.MagicBytes != null && formatInfo.MagicBytes.Length > 0)
					{
						Console.WriteLine("Magic Bytes: " + BitConverter.ToString(formatInfo.MagicBytes).Replace("-", " "));
					}
					
					Console.WriteLine();
					Console.WriteLine("Recommended Output Name: " + FormatDetector.CreateOutputFileName(inputPath, formatInfo));
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error detecting format: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if PNG to texture conversion mode
			if (args.Length >= 3 && args[0] == "-todds")
			{
				try
				{
					string inputPNG = args[1];
					string outputTexture = args[2];
					
					if (!File.Exists(inputPNG))
					{
						Console.WriteLine("Error: PNG file not found: " + inputPNG);
						return;
					}

					// Определяем целевой формат по расширению
					string extension = Path.GetExtension(outputTexture).ToLower();
					if (extension == ".gtf")
					{
						TextureFormatConverter.ConvertFromPNG(inputPNG, outputTexture, TextureFormatConverter.TextureFormat.GTF);
					}
					else
					{
						// По умолчанию DDS
						TextureFormatConverter.ConvertFromPNG(inputPNG, outputTexture, TextureFormatConverter.TextureFormat.DDS);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error converting PNG to texture: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if texture to PNG conversion mode
			if (args.Length >= 3 && args[0] == "-topng")
			{
				try
				{
					string inputTexture = args[1];
					string outputPNG = args[2];
					
					if (!File.Exists(inputTexture))
					{
						Console.WriteLine("Error: Texture file not found: " + inputTexture);
						return;
					}

					// Используем универсальный конвертер
					TextureFormatConverter.ConvertToPNG(inputTexture, outputPNG);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error converting texture to PNG: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if texture extraction mode
			if (args.Length >= 2 && args[0] == "-texture")
			{
				try
				{
					string inputPath = args[1];
					
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					TextureExtractor.ExtractTexture(inputPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error extracting texture: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if find size mode
			if (args.Length >= 3 && args[0] == "-findsize")
			{
				try
				{
					string inputPath = args[1];
					long offset = long.Parse(args[2]);
					
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					StructSizeFinder.FindStructSize(inputPath, offset);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error in findsize mode: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if find data mode
			if (args.Length >= 2 && args[0] == "-finddata")
			{
				try
				{
					string inputPath = args[1];
					
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					FontDataFinder.FindFontData(inputPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error in finddata mode: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if extract char mode
			if (args.Length >= 5 && args[0] == "-extractchar")
			{
				try
				{
					string inputPath = args[1];
					long offset = long.Parse(args[2]);
					int count = int.Parse(args[3]);
					int structSize = int.Parse(args[4]);
					
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					FullCharExtractor.ExtractWithStructure(inputPath, offset, count, structSize);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error in extractchar mode: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if analyze char mode
			if (args.Length >= 4 && args[0] == "-analyzechar")
			{
				try
				{
					string inputPath = args[1];
					long offset = long.Parse(args[2]);
					int expectedCount = int.Parse(args[3]);
					
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					CharStructureAnalyzer.AnalyzeCharData(inputPath, offset, expectedCount);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error in analyzechar mode: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if find chars mode
			if (args.Length >= 3 && args[0] == "-findchars")
			{
				try
				{
					string inputPath = args[1];
					int expectedCount = int.Parse(args[2]);
					
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					PhyreDebugger.FindCharacterDataOffsets(inputPath, expectedCount);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error in findchars mode: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if debug mode
			if (args.Length >= 2 && args[0] == "-debug")
			{
				try
				{
					string inputPath = args[1];
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					if (args.Length >= 3)
					{
						// Hex dump at specific offset
						long offset = long.Parse(args[2]);
						Console.WriteLine("Analyzing structure at offset " + offset + " (0x" + offset.ToString("X") + ")");
						Console.WriteLine();
						
						PhyreDebugger.DumpHexAtOffset(inputPath, offset, 512);
						PhyreDebugger.AnalyzeCharStructure(inputPath, offset, 100);
					}
					
					// Always analyze textures
					PhyreDebugger.FindTextureData(inputPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error in debug mode: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if extract mode (FULL font extraction)
			if (args.Length >= 2 && args[0] == "-extract")
			{
				try
				{
					string inputPath = args[1];
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					FontExtractor.ExtractFont(inputPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error extracting font: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if analyze mode
			if (args.Length >= 2 && args[0] == "-analyze")
			{
				try
				{
					string inputPath = args[1];
					if (!File.Exists(inputPath))
					{
						Console.WriteLine("Error: File not found: " + inputPath);
						return;
					}

					PhyreAnalyzer.AnalyzeFile(inputPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error analyzing file: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if pack font mode
			if (args.Length >= 4 && args[0] == "-packfont")
			{
				try
				{
					string fntPath = args[1];
					string ddsPath = args[2];
					string outputPath = args[3];

					if (!File.Exists(fntPath))
					{
						Console.WriteLine("Error: FNT file not found: " + fntPath);
						return;
					}

					if (!File.Exists(ddsPath))
					{
						Console.WriteLine("Error: DDS file not found: " + ddsPath);
						return;
					}

					FontPacker.PackFont(fntPath, ddsPath, outputPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error packing font: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if verify mode
			if (args.Length >= 2 && args[0] == "-verify")
			{
				try
				{
					string packedPath = args[1];
					string originalPath = args.Length >= 3 ? args[2] : null;

					if (!File.Exists(packedPath))
					{
						Console.WriteLine("Error: Packed file not found: " + packedPath);
						return;
					}

					if (originalPath != null && !File.Exists(originalPath))
					{
						Console.WriteLine("Error: Original file not found: " + originalPath);
						return;
					}

					PhyrePackVerifier.VerifyPackedFile(packedPath, originalPath);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error verifying packed file: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Check if pack mode
			if (args.Length >= 4 && args[0] == "-pack")
			{
				try
				{
					string smdPath = args[1];
					string meshAsciiPath = args[2];
					string outputPath = args[3];

					if (!File.Exists(smdPath))
					{
						Console.WriteLine("Error: SMD file not found: " + smdPath);
						return;
					}

					if (!File.Exists(meshAsciiPath))
					{
						Console.WriteLine("Error: Mesh.ascii file not found: " + meshAsciiPath);
						return;
					}

					PhyrePacker packer = new PhyrePacker(smdPath, meshAsciiPath, outputPath);
					packer.Pack();
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error packing file: " + ex.Message);
					Console.WriteLine(ex.StackTrace);
				}
				return;
			}

			// Unpack mode (original functionality)
			if (args.Length < 1)
			{
				ShowUsage();
				return;
			}

			if (!File.Exists(args[0]))
			{
				Console.WriteLine("Error: File not found: " + args[0]);
				return;
			}

			// Check if it's a font file
			if (FontUnpacker.IsLikelyFontFile(args[0]))
			{
				Console.WriteLine("Detected font file. Using specialized font unpacker...");
				Console.WriteLine();
				FontUnpacker.UnpackFont(args[0]);
				Console.WriteLine();
				Console.WriteLine("Tip: For detailed structure analysis, use:");
				Console.WriteLine("  LZS_inpack.exe -analyze \"" + Path.GetFileName(args[0]) + "\"");
				return;
			}

			UnpackPhyre(args[0]);
		}

		private static void UnpackPhyre(string inputPath)
		{
			Console.WriteLine("Unpacking: " + Path.GetFileName(inputPath));
			Console.WriteLine("Size: " + new FileInfo(inputPath).Length + " bytes");
			Console.WriteLine();

			NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
			numberFormatInfo.NumberDecimalSeparator = ".";
			FileStream fileStream = new FileStream(inputPath, FileMode.Open);
			BinaryReader binaryReader = new BinaryReader(fileStream);
			binaryReader.ReadInt32();
			int num = binaryReader.ReadInt32();
			int num2 = binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			int num3 = binaryReader.ReadInt32();
			int num4 = binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			int num5 = binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			int num6 = binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			int num7 = binaryReader.ReadInt32();
			int num8 = binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			int num9 = binaryReader.ReadInt32();
			int num10 = binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			int num11 = binaryReader.ReadInt32();
			fileStream.Seek((long)(num + 8), SeekOrigin.Begin);
			int num12 = binaryReader.ReadInt32();
			int num13 = binaryReader.ReadInt32();
			int num14 = binaryReader.ReadInt32();
			fileStream.Seek((long)(num12 * 4 + 12), SeekOrigin.Current);
			int[] array = new int[num13];
			int[] array2 = new int[num13];
			string[] array3 = new string[num13];
			for (int i = 0; i < num13; i++)
			{
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				array[i] = binaryReader.ReadInt32();
				array2[i] = binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
			}
			long num15 = fileStream.Position + (long)(num14 * 24);
			int[] array4 = new int[num14];
			for (int i = 0; i < num14; i++)
			{
				array4[i] = binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
			}
			int num16 = 0;
			for (int i = 0; i < num13; i++)
			{
				string text = "";
				fileStream.Seek(num15 + (long)array[i], SeekOrigin.Begin);
				byte b;
				while ((b = binaryReader.ReadByte()) > 0)
				{
					text += (char)b;
				}
				array3[i] = text;
				for (int j = 0; j < array2[i]; j++)
				{
					text = "";
					fileStream.Seek(num15 + (long)array4[num16++], SeekOrigin.Begin);
					while ((b = binaryReader.ReadByte()) > 0)
					{
						text += (char)b;
					}
				}
			}
			fileStream.Seek((long)(num + num2), SeekOrigin.Begin);
			int[] array5 = new int[num3];
			int[] array6 = new int[num3];
			int[] array7 = new int[num3];
			int[] array8 = new int[num3];
			int[] array9 = new int[num3];
			long[] array10 = new long[num3];
			long num17 = fileStream.Position + (long)(num3 * 36);
			int num18 = 0;
			int num19 = 0;
			int num20 = 0;
			int num21 = 0;
			int num22 = 0;
			int num23 = 0;
			for (int i = 0; i < num3; i++)
			{
				array5[i] = binaryReader.ReadInt32() - 1;
				array6[i] = binaryReader.ReadInt32();
				array7[i] = binaryReader.ReadInt32();
				array10[i] = num17;
				num17 += (long)array7[i];
				array8[i] = binaryReader.ReadInt32();
				array9[i] = binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				if (array3[array5[i]] == "PDataBlock")
				{
					num21 = i;
				}
				if (array3[array5[i]] == "PMatrix4")
				{
					num19 = i;
				}
				if (array3[array5[i]] == "PMesh")
				{
					num18 = i;
				}
				if (array3[array5[i]] == "PMeshSegment")
				{
					num22 = i;
				}
				if (array3[array5[i]] == "PSkinBoneRemap")
				{
					num20 = i;
				}
				if (array3[array5[i]] == "PNode")
				{
					num23 = i;
				}
			}
			long num24 = num17 + (long)num8 + (long)num4 + (long)num5 + (long)(num7 * 12) + (long)(num9 * 4) + (long)(num10 * 16) + (long)num6;
			StreamWriter streamWriter = new StreamWriter(Path.GetFileNameWithoutExtension(inputPath) + ".smd");
			streamWriter.WriteLine("version 1");
			StreamWriter streamWriter2 = new StreamWriter(Path.GetFileNameWithoutExtension(inputPath) + ".mesh.ascii");
			streamWriter2.WriteLine("0");
			fileStream.Seek(array10[num18], SeekOrigin.Begin);
			int num25 = 0;
			int num26 = 0;
			int num27 = 0;
			for (int i = 0; i < array6[num18]; i++)
			{
				num25 += (binaryReader.ReadInt32() & 65535);
				binaryReader.ReadInt32();
				num27 += (binaryReader.ReadInt32() & 65535);
				fileStream.Seek(12L, SeekOrigin.Current);
				num26 += (binaryReader.ReadInt32() & 65535);
				fileStream.Seek(28L, SeekOrigin.Current);
			}
			streamWriter2.WriteLine(num25);

			// Check if file has mesh data
			if (num25 == 0)
			{
				Console.WriteLine("WARNING: No mesh segments found!");
				Console.WriteLine("This might be a font or non-mesh file.");
				Console.WriteLine("Try using: LZS_inpack.exe -analyze \"" + Path.GetFileName(inputPath) + "\"");
				Console.WriteLine();
			}

			Console.WriteLine("Mesh segments: " + num25);
			Console.WriteLine("Bones: " + num26);
			Console.WriteLine();

			int[] array11 = new int[num26];
			for (int i = 0; i < num26; i++)
			{
				array11[i] = binaryReader.ReadInt32();
			}
			streamWriter.WriteLine("nodes");
			for (int i = 0; i < num26; i++)
			{
				streamWriter.WriteLine(string.Concat(new object[]
				{
					i,
					" \"bone_",
					i.ToString("X2"),
					"\" ",
					array11[i]
				}));
			}
			streamWriter.WriteLine("end");
			streamWriter.WriteLine("skeleton");
			streamWriter.WriteLine("time 0");
			fileStream.Seek(array10[num23], SeekOrigin.Begin);
			float num28 = binaryReader.ReadSingle();
			float num29 = binaryReader.ReadSingle();
			float num30 = binaryReader.ReadSingle();
			double[,] array12 = new double[3, 3];
			new Quaternion3D();
			Vector3D vector3D = new Vector3D();
			fileStream.Seek(array10[num19] + (long)(64 * num27), SeekOrigin.Begin);
			for (int i = 0; i < num26; i++)
			{
				array12[0, 0] = (double)binaryReader.ReadSingle();
				array12[0, 1] = (double)binaryReader.ReadSingle();
				array12[0, 2] = (double)binaryReader.ReadSingle();
				float num31 = binaryReader.ReadSingle();
				array12[1, 0] = (double)binaryReader.ReadSingle();
				array12[1, 1] = (double)binaryReader.ReadSingle();
				array12[1, 2] = (double)binaryReader.ReadSingle();
				float num32 = binaryReader.ReadSingle();
				array12[2, 0] = (double)binaryReader.ReadSingle();
				array12[2, 1] = (double)binaryReader.ReadSingle();
				array12[2, 2] = (double)binaryReader.ReadSingle();
				float num33 = binaryReader.ReadSingle();
				num31 = binaryReader.ReadSingle();
				num32 = binaryReader.ReadSingle();
				num33 = binaryReader.ReadSingle();
				binaryReader.ReadSingle();
				if (i == 0)
				{
					num31 += num28;
					num32 += num29;
					num33 += num30;
				}
				vector3D = C3D.ToEulerAngles(Program.matrix2quat(array12));
				streamWriter.Write(i + "  ");
				streamWriter.Write(num31.ToString("0.000000", numberFormatInfo));
				streamWriter.Write(" " + num32.ToString("0.000000", numberFormatInfo));
				streamWriter.Write(" " + num33.ToString("0.000000", numberFormatInfo));
				streamWriter.Write("  " + vector3D.X.ToString("0.000000", numberFormatInfo));
				streamWriter.Write(" " + vector3D.Y.ToString("0.000000", numberFormatInfo));
				streamWriter.WriteLine(" " + vector3D.Z.ToString("0.000000", numberFormatInfo));
			}
			streamWriter.WriteLine("end");
			int[] array13 = new int[num25];
			int[] array14 = new int[num25];
			int[] array15 = new int[num25];
			int[] array16 = new int[num25];
			fileStream.Seek(array10[num22], SeekOrigin.Begin);
			for (int i = 0; i < num25; i++)
			{
				int j = binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				array13[i] = (binaryReader.ReadInt32() & 65535);
				fileStream.Seek(28L, SeekOrigin.Current);
				array16[i] = (binaryReader.ReadInt32() & 65535);
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				array14[i] = binaryReader.ReadInt32();
				fileStream.Seek(32L, SeekOrigin.Current);
				array15[i] = binaryReader.ReadInt32();
				fileStream.Seek(12L, SeekOrigin.Current);
			}
			int[] array17 = new int[num25];
			int[] array18 = new int[num25];
			int[] array19 = new int[num25];
			int[] array20 = new int[num25];
			int[] array21 = new int[num25];
			int[] array22 = new int[num25];
			fileStream.Seek(array10[num21], SeekOrigin.Begin);
			for (int i = 0; i < num25; i++)
			{
				int num34 = binaryReader.ReadInt32();
				array17[i] = binaryReader.ReadInt32();
				fileStream.Seek(40L, SeekOrigin.Current);
				array18[i] = binaryReader.ReadInt32();
				fileStream.Seek(12L, SeekOrigin.Current);
				if (array16[i] > 1)
				{
					binaryReader.ReadInt32();
					array17[i] = binaryReader.ReadInt32();
					fileStream.Seek(40L, SeekOrigin.Current);
					array19[i] = binaryReader.ReadInt32();
					fileStream.Seek(12L, SeekOrigin.Current);
				}
				for (int j = 0; j < array16[i] - 2; j++)
				{
					num34 = binaryReader.ReadInt32();
					fileStream.Seek(44L, SeekOrigin.Current);
					int num35 = binaryReader.ReadInt32();
					if (num34 == 8 && array20[i] == 0)
					{
						array20[i] = num35;
					}
					if (num34 == 16)
					{
						array21[i] = num35;
					}
					if (num34 == 4)
					{
						array22[i] = num35;
					}
					fileStream.Seek(12L, SeekOrigin.Current);
				}
			}
			for (int i = 0; i < num25; i++)
			{
				Vector3D[] array23 = new Vector3D[array17[i]];
				Vector3D[] array24 = new Vector3D[array17[i]];
				float[] array25 = new float[array17[i]];
				float[] array26 = new float[array17[i]];
				int[,] array27 = new int[array17[i], 4];
				float[,] array28 = new float[array17[i], 4];
				streamWriter2.WriteLine("Submesh" + i.ToString());
				streamWriter2.WriteLine("1");
				streamWriter2.WriteLine("1");
				streamWriter2.WriteLine("material_" + i.ToString());
				streamWriter2.WriteLine("0");
				streamWriter2.WriteLine(array17[i]);
				fileStream.Seek(num24 + (long)num11 + (long)array18[i], SeekOrigin.Begin);
				for (int j = 0; j < array17[i]; j++)
				{
					float num31 = binaryReader.ReadSingle();
					float num32 = binaryReader.ReadSingle();
					float num33 = binaryReader.ReadSingle();
					array23[j] = new Vector3D(num31, num32, num33);
				}
				if (array16[i] > 1)
				{
					fileStream.Seek(num24 + (long)num11 + (long)array19[i], SeekOrigin.Begin);
					for (int j = 0; j < array17[i]; j++)
					{
						float num31 = binaryReader.ReadSingle();
						float num32 = binaryReader.ReadSingle();
						float num33 = binaryReader.ReadSingle();
						array24[j] = new Vector3D(num31, num32, num33);
					}
					fileStream.Seek(num24 + (long)num11 + (long)array20[i], SeekOrigin.Begin);
					for (int j = 0; j < array17[i]; j++)
					{
						array25[j] = binaryReader.ReadSingle();
						array26[j] = binaryReader.ReadSingle();
					}
					fileStream.Seek(num24 + (long)num11 + (long)array21[i], SeekOrigin.Begin);
					for (int j = 0; j < array17[i]; j++)
					{
						array28[j, 0] = binaryReader.ReadSingle();
						array28[j, 1] = binaryReader.ReadSingle();
						array28[j, 2] = binaryReader.ReadSingle();
						array28[j, 3] = binaryReader.ReadSingle();
					}
					fileStream.Seek(num24 + (long)num11 + (long)array22[i], SeekOrigin.Begin);
					for (int j = 0; j < array17[i]; j++)
					{
						array27[j, 0] = (int)binaryReader.ReadByte();
						array27[j, 1] = (int)binaryReader.ReadByte();
						array27[j, 2] = (int)binaryReader.ReadByte();
						array27[j, 3] = (int)binaryReader.ReadByte();
					}
				}
				else
				{
					for (int j = 0; j < array17[i]; j++)
					{
						array24[j] = new Vector3D(0f, 0f, 0f);
					}
				}
				int[] array29 = new int[array13[i]];
				fileStream.Seek(array10[num20], SeekOrigin.Begin);
				for (int j = 0; j < array13[i]; j++)
				{
					array29[j] = (int)binaryReader.ReadUInt16();
					binaryReader.ReadUInt16();
				}
				array10[num20] = fileStream.Position;
				for (int j = 0; j < array17[i]; j++)
				{
					streamWriter2.Write(array23[j].X.ToString("0.000000", numberFormatInfo));
					streamWriter2.Write(" " + array23[j].Y.ToString("0.000000", numberFormatInfo));
					streamWriter2.WriteLine(" " + array23[j].Z.ToString("0.000000", numberFormatInfo));
					streamWriter2.WriteLine("0.0 0.0 0.0");
					streamWriter2.WriteLine("255 255 255 255");
					streamWriter2.WriteLine(array25[j].ToString("0.000000", numberFormatInfo) + " " + array26[j].ToString("0.000000", numberFormatInfo));
				}
				fileStream.Seek(num24 + (long)array15[i], SeekOrigin.Begin);
				streamWriter2.WriteLine(array14[i] / 3);
				streamWriter.WriteLine("triangles");
				for (int j = 0; j < array14[i] / 3; j++)
				{
					streamWriter.WriteLine("Submesh_" + i.ToString());
					for (int k = 0; k < 3; k++)
					{
						int num36 = (int)binaryReader.ReadUInt16();
						streamWriter.Write("0 ");
						streamWriter.Write(" " + array23[num36].X.ToString("0.000000", numberFormatInfo));
						streamWriter.Write(" " + array23[num36].Y.ToString("0.000000", numberFormatInfo));
						streamWriter.Write(" " + array23[num36].Z.ToString("0.000000", numberFormatInfo));
						streamWriter.Write("  " + array24[num36].X.ToString("0.000000", numberFormatInfo));
						streamWriter.Write(" " + array24[num36].Y.ToString("0.000000", numberFormatInfo));
						streamWriter.Write(" " + array24[num36].Z.ToString("0.000000", numberFormatInfo));
						streamWriter.Write("  " + array25[num36].ToString("0.000000", numberFormatInfo));
						streamWriter.Write(" " + array26[num36].ToString("0.000000", numberFormatInfo));
						if (array13[i] > 0)
						{
							int num37 = 0;
							if (array28[num36, 0] > 0f)
							{
								num37++;
							}
							if (array28[num36, 1] > 0f)
							{
								num37++;
							}
							if (array28[num36, 2] > 0f)
							{
								num37++;
							}
							if (array28[num36, 3] > 0f)
							{
								num37++;
							}
							streamWriter.Write(" " + num37);
							for (int l = 0; l < num37; l++)
							{
								streamWriter.Write(" " + array29[array27[num36, l]]);
								streamWriter.Write(" " + array28[num36, l].ToString("0.######", numberFormatInfo));
							}
						}
						streamWriter.WriteLine();
						streamWriter2.Write(num36 + " ");
					}
					streamWriter2.WriteLine();
				}
				streamWriter.WriteLine("end");
			}
			streamWriter.Close();
			streamWriter2.Close();

			Console.WriteLine("Unpacked successfully!");
			Console.WriteLine("Output files:");
			Console.WriteLine("  " + Path.GetFileNameWithoutExtension(inputPath) + ".smd");
			Console.WriteLine("  " + Path.GetFileNameWithoutExtension(inputPath) + ".mesh.ascii");
		}

		private static void StartWebInterface()
		{
			Console.WriteLine("🌐 Starting LZS Phyre Engine Tool Web Interface...");
			Console.WriteLine();
			Console.WriteLine("📋 Web Interface Features:");
			Console.WriteLine("  • Extract fonts and textures from .phyre files");
			Console.WriteLine("  • Pack fonts and models back to .phyre format");
			Console.WriteLine("  • Verify packed file quality");
			Console.WriteLine("  • Convert textures (DDS ↔ PNG ↔ GTF)");
			Console.WriteLine("  • Analyze file structures");
			Console.WriteLine("  • Auto-detect file formats");
			Console.WriteLine();
			Console.WriteLine("🚀 Launching web server...");
			Console.WriteLine("📍 URL: http://localhost:5000");
			Console.WriteLine();
			Console.WriteLine("💡 Tip: The web interface will open automatically in your default browser.");
			Console.WriteLine("🛑 Press Ctrl+C to stop the web server.");
			Console.WriteLine();

			try
			{
				// Get the current directory
				string currentDir = Directory.GetCurrentDirectory();
				
				// Check if LZS_Web.exe exists in bin/Release/net9.0/
				string webExePath = Path.Combine(currentDir, "bin", "Release", "net9.0", "LZS_Web.exe");
				if (!File.Exists(webExePath))
				{
					Console.WriteLine("❌ Error: LZS_Web.exe not found!");
					Console.WriteLine("   Expected location: " + webExePath);
					Console.WriteLine("   Make sure the web application is built in Release mode.");
					Console.WriteLine("   Run: dotnet build LZS_Web.csproj -c Release");
					return;
				}

				// Start the web application
				var startInfo = new System.Diagnostics.ProcessStartInfo
				{
					FileName = webExePath,
					Arguments = "--urls \"http://localhost:5000\"",
					UseShellExecute = false,
					RedirectStandardOutput = false,
					RedirectStandardError = false,
					CreateNoWindow = false
				};

				Console.WriteLine("🔄 Starting web application...");
				var process = System.Diagnostics.Process.Start(startInfo);
				
				if (process != null)
				{
					Console.WriteLine("✅ Web server started successfully!");
					Console.WriteLine();
					
					// Try to open browser (Windows only)
					try
					{
						System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
						{
							FileName = "http://localhost:5000",
							UseShellExecute = true
						});
						Console.WriteLine("🌐 Browser opened automatically.");
					}
					catch
					{
						Console.WriteLine("💡 Please open your browser manually and go to: http://localhost:5000");
					}
					
					Console.WriteLine();
					Console.WriteLine("⏳ Web server is running... Press Ctrl+C to stop.");
					
					// Wait for the process to exit
					process.WaitForExit();
				}
				else
				{
					Console.WriteLine("❌ Failed to start web server process!");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("❌ Error starting web interface:");
				Console.WriteLine("   " + ex.Message);
				Console.WriteLine();
				Console.WriteLine("💡 Troubleshooting:");
				Console.WriteLine("   • Make sure LZS_Web.exe is built in Release mode");
				Console.WriteLine("   • Check if port 5000 is available");
				Console.WriteLine("   • Run from the project root directory");
				Console.WriteLine("   • Build web app manually: dotnet build LZS_Web.csproj -c Release");
				Console.WriteLine("   • Try running manually: bin\\Release\\net9.0\\LZS_Web.exe");
			}
		}
	}
}
