using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LZS_unpack
{
	public class DDSToPNGConverter
	{
		public static bool ConvertDDSToPNG(string ddsPath, string pngPath)
		{
			Console.WriteLine();
			Console.WriteLine("=== Converting DDS to PNG ===");
			Console.WriteLine("Input:  " + Path.GetFileName(ddsPath));
			Console.WriteLine("Output: " + Path.GetFileName(pngPath));
			Console.WriteLine();

			if (!File.Exists(ddsPath))
			{
				Console.WriteLine("ERROR: DDS file not found: " + ddsPath);
				return false;
			}

			FileStream fs = new FileStream(ddsPath, FileMode.Open, FileAccess.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				// Read DDS header
				uint magic = br.ReadUInt32();
				if (magic != 0x20534444) // "DDS "
				{
					Console.WriteLine("ERROR: Invalid DDS file (wrong magic)");
					return false;
				}

				uint headerSize = br.ReadUInt32(); // 124
				uint flags = br.ReadUInt32();
				int height = br.ReadInt32();
				int width = br.ReadInt32();
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

				Console.WriteLine("DDS Info:");
				Console.WriteLine("  Size: " + width + "x" + height);
				Console.WriteLine("  Format flags: 0x" + pfFlags.ToString("X"));
				Console.WriteLine("  RGB bit count: " + rgbBitCount);
				Console.WriteLine();

				// Check if this is L8 (luminance 8-bit)
				if ((pfFlags & 0x20000) != 0 && rgbBitCount == 8)
				{
					Console.WriteLine("Format: L8 (8-bit Luminance)");
					ConvertL8ToPNG(br, width, height, pngPath);
					return true;
				}
				else if (fourCC != 0)
				{
					string fourCCStr = System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes(fourCC));
					Console.WriteLine("Format: " + fourCCStr + " (compressed)");
					Console.WriteLine("Compressed formats not yet supported.");
					Console.WriteLine("Use ImageMagick or GIMP to convert.");
					return false;
				}
				else
				{
					Console.WriteLine("Format: RGB/RGBA (" + rgbBitCount + "-bit)");
					Console.WriteLine("This format is not yet supported.");
					return false;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("ERROR: " + ex.Message);
				return false;
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		static void ConvertL8ToPNG(BinaryReader br, int width, int height, string outputPath)
		{
			Console.WriteLine("Reading pixel data...");
			
			// Read L8 data
			byte[] pixelData = br.ReadBytes(width * height);
			
			if (pixelData.Length != width * height)
			{
				throw new Exception("Not enough pixel data in DDS file");
			}

			Console.WriteLine("Creating bitmap...");
			
			// Create bitmap (8-bit indexed for grayscale)
			Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
			
			// Set up grayscale palette
			ColorPalette palette = bmp.Palette;
			for (int i = 0; i < 256; i++)
			{
				palette.Entries[i] = Color.FromArgb(255, i, i, i);
			}
			bmp.Palette = palette;
			
			// Lock bitmap for direct pixel access
			BitmapData bmpData = bmp.LockBits(
				new Rectangle(0, 0, width, height),
				ImageLockMode.WriteOnly,
				PixelFormat.Format8bppIndexed);
			
			try
			{
				Console.WriteLine("Writing pixels...");
				
				// Copy pixel data
				IntPtr ptr = bmpData.Scan0;
				
				// Calculate stride (may be padded to 4-byte boundary)
				int stride = bmpData.Stride;
				
				if (stride == width)
				{
					// No padding, direct copy
					Marshal.Copy(pixelData, 0, ptr, pixelData.Length);
				}
				else
				{
					// Row by row copy (with padding)
					for (int y = 0; y < height; y++)
					{
						Marshal.Copy(pixelData, y * width, ptr + y * stride, width);
					}
				}
			}
			finally
			{
				bmp.UnlockBits(bmpData);
			}
			
			Console.WriteLine("Saving PNG...");
			
			// Save as PNG
			bmp.Save(outputPath, ImageFormat.Png);
			bmp.Dispose();
			
			Console.WriteLine();
			Console.WriteLine("========================================");
			Console.WriteLine("CONVERSION COMPLETE!");
			Console.WriteLine("========================================");
			Console.WriteLine("Output: " + outputPath);
			Console.WriteLine("Size: " + (new FileInfo(outputPath).Length / 1024) + " KB");
			Console.WriteLine();
		}
	}
}

