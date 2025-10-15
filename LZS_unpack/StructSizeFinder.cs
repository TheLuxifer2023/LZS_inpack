using System;
using System.IO;

namespace LZS_unpack
{
	public class StructSizeFinder
	{
		public static void FindStructSize(string filePath, long startOffset)
		{
			Console.WriteLine();
			Console.WriteLine("=== Finding Structure Size ===");
			Console.WriteLine("File: " + Path.GetFileName(filePath));
			Console.WriteLine("Start offset: 0x" + startOffset.ToString("X") + " (" + startOffset + ")");
			Console.WriteLine();

			FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				fs.Seek(startOffset, SeekOrigin.Begin);
				
				// Read first code
				int firstCode = br.ReadInt32();
				Console.WriteLine("First code at 0x" + startOffset.ToString("X") + ": " + firstCode + 
					" ('" + (char)firstCode + "')");
				
				// Search for next sequential code
				long searchStart = startOffset + 4;
				long searchEnd = Math.Min(startOffset + 200, fs.Length - 4);
				
				for (long offset = searchStart; offset < searchEnd; offset++)
				{
					fs.Seek(offset, SeekOrigin.Begin);
					int code = br.ReadInt32();
					
					// Check if this is the next sequential code
					if (code == firstCode + 1)
					{
						long structSize = offset - startOffset;
						Console.WriteLine("Next code found at 0x" + offset.ToString("X") + ": " + code + 
							" ('" + (char)code + "')");
						Console.WriteLine();
						Console.WriteLine("========================================");
						Console.WriteLine("STRUCTURE SIZE: " + structSize + " bytes");
						Console.WriteLine("========================================");
						Console.WriteLine();
						Console.WriteLine("To extract, use:");
						Console.WriteLine("  LZS_inpack.exe -extractchar " + Path.GetFileName(filePath) + 
							" " + startOffset + " 7447 " + structSize);
						return;
					}
				}
				
				Console.WriteLine("Could not find next sequential code in search range.");
				Console.WriteLine("The structure might not start with a sequential code pattern.");
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}
	}
}

