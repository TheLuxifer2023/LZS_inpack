using System;
using System.IO;
using System.Text;

namespace LZS_unpack
{
	/// <summary>
	/// Analyzer for Phyre Engine file structure
	/// </summary>
	internal class PhyreAnalyzer
	{
		public static void AnalyzeFile(string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open);
			BinaryReader br = new BinaryReader(fs);

			Console.WriteLine("=== Phyre File Analysis ===");
			Console.WriteLine("File: " + Path.GetFileName(filePath));
			Console.WriteLine("Size: " + fs.Length + " bytes");
			Console.WriteLine();

			try
			{
				// Read header
				Console.WriteLine("--- Header (first 72 bytes) ---");
				int magic = br.ReadInt32();
				Console.WriteLine("Magic/Version: 0x" + magic.ToString("X8"));

				int offset1 = br.ReadInt32();
				int offset2 = br.ReadInt32();
				br.ReadInt32();
				int count1 = br.ReadInt32();
				int size1 = br.ReadInt32();
				br.ReadInt32();
				int size2 = br.ReadInt32();
				br.ReadInt32();
				int size3 = br.ReadInt32();
				br.ReadInt32();
				br.ReadInt32();
				int count2 = br.ReadInt32();
				int count3 = br.ReadInt32();
				br.ReadInt32();
				int count4 = br.ReadInt32();
				int count5 = br.ReadInt32();
				br.ReadInt32();
				int dataSize = br.ReadInt32();

				Console.WriteLine("Offset 1: " + offset1);
				Console.WriteLine("Offset 2: " + offset2);
				Console.WriteLine("Count 1 (Objects?): " + count1);
				Console.WriteLine("Size 1: " + size1);
				Console.WriteLine("Size 2: " + size2);
				Console.WriteLine("Size 3: " + size3);
				Console.WriteLine("Count 2: " + count2);
				Console.WriteLine("Count 3: " + count3);
				Console.WriteLine("Count 4: " + count4);
				Console.WriteLine("Count 5: " + count5);
				Console.WriteLine("Data Size: " + dataSize);
				Console.WriteLine();

				// Jump to class definitions (following original algorithm)
				fs.Seek((long)(offset1 + 8), SeekOrigin.Begin);
				int num12 = br.ReadInt32(); // Some count
				int numClasses = br.ReadInt32(); // Number of classes (num13 in original)
				int num14 = br.ReadInt32(); // Another count

				Console.WriteLine("--- Class Definitions ---");
				Console.WriteLine("Number of Classes: " + numClasses);
				Console.WriteLine("Num12 (array size): " + num12);
				Console.WriteLine("Num14 (properties?): " + num14);
				Console.WriteLine();

				// Calculate number of instances from offset2 and data structure
				int numInstances = count1; // Use object count as approximation

				// Skip array (num12 * 4 + 12) - following original code
				fs.Seek((long)(num12 * 4 + 12), SeekOrigin.Current);

				// Read class definitions to get name offsets
				int[] classNameOffsets = new int[numClasses];
				int[] classPropertyCounts = new int[numClasses];
				string[] classNames = new string[numClasses];

				for (int i = 0; i < numClasses; i++)
				{
					br.ReadInt32(); // Field 1
					br.ReadInt32(); // Field 2
					classNameOffsets[i] = br.ReadInt32(); // Field 3 - NAME OFFSET
					classPropertyCounts[i] = br.ReadInt32(); // Field 4 - property count
					// Skip rest of class definition (20 bytes = 5 int32)
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
					br.ReadInt32();
				}

				// Calculate string table start position (following original: position + num14 * 24)
				long stringTableStart = fs.Position + (long)(num14 * 24);

				// Read class names from string table
				Console.WriteLine("--- Class Names ---");
				Console.WriteLine("String table starts at offset: 0x" + stringTableStart.ToString("X"));
				Console.WriteLine();

				for (int i = 0; i < numClasses; i++)
				{
					long namePos = stringTableStart + (long)classNameOffsets[i];
					Console.Write("  [" + i + "] offset=" + classNameOffsets[i] + " (0x" + namePos.ToString("X") + "): ");
					
					fs.Seek(namePos, SeekOrigin.Begin);
					StringBuilder sb = new StringBuilder();
					byte b;
					int charCount = 0;
					while ((b = br.ReadByte()) > 0 && charCount < 100)
					{
						sb.Append((char)b);
						charCount++;
					}
					classNames[i] = sb.ToString();
					Console.WriteLine("\"" + classNames[i] + "\"");
				}
				Console.WriteLine();

				// Analyze object instances
				Console.WriteLine("--- Object Instance Analysis ---");
				fs.Seek((long)(offset1 + offset2), SeekOrigin.Begin);

				int[] instanceClasses = new int[numInstances];
				int[] instanceCounts = new int[numInstances];

				for (int i = 0; i < numInstances && i < 50; i++)
				{
					int classId = br.ReadInt32() - 1;
					int count = br.ReadInt32();
					int dataOffset = br.ReadInt32();

					if (classId >= 0 && classId < classNames.Length)
					{
						Console.WriteLine("  [" + i + "] Class: " + classNames[classId] + ", Count: " + count + ", Offset: " + dataOffset);
					}

					// Skip rest of instance data
					for (int j = 0; j < 6; j++) br.ReadInt32();
				}

				if (numInstances > 50)
				{
					Console.WriteLine("  ... (" + (numInstances - 50) + " more instances)");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error during analysis: " + ex.Message);
			}
			finally
			{
				br.Close();
				fs.Close();
			}

			Console.WriteLine();
			Console.WriteLine("=== Analysis Complete ===");
		}
	}
}

