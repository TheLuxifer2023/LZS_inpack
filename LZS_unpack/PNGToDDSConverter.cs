using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LZS_unpack
{
	public class PNGToDDSConverter
	{
		public static bool ConvertPNGToDDS(string pngPath, string ddsPath)
		{
			Console.WriteLine();
			Console.WriteLine("=== Converting PNG to DDS ===");
			Console.WriteLine("Input:  " + Path.GetFileName(pngPath));
			Console.WriteLine("Output: " + Path.GetFileName(ddsPath));
			Console.WriteLine();

			if (!File.Exists(pngPath))
			{
				Console.WriteLine("ERROR: PNG file not found: " + pngPath);
				return false;
			}

			try
			{
				// Load PNG
				Console.WriteLine("Loading PNG...");
				Bitmap bmp = new Bitmap(pngPath);
				int width = bmp.Width;
				int height = bmp.Height;
				
				Console.WriteLine("  Size: " + width + "x" + height);
				Console.WriteLine();

				// Convert to L8 (grayscale)
				Console.WriteLine("Converting to L8 grayscale...");
				byte[] pixelData = new byte[width * height];
				
				BitmapData bmpData = bmp.LockBits(
					new Rectangle(0, 0, width, height),
					ImageLockMode.ReadOnly,
					PixelFormat.Format24bppRgb);
				
				try
				{
					IntPtr ptr = bmpData.Scan0;
					int stride = bmpData.Stride;
					
					for (int y = 0; y < height; y++)
					{
						for (int x = 0; x < width; x++)
						{
							// Read RGB
							byte b = Marshal.ReadByte(ptr, y * stride + x * 3);
							byte g = Marshal.ReadByte(ptr, y * stride + x * 3 + 1);
							byte r = Marshal.ReadByte(ptr, y * stride + x * 3 + 2);
							
							// Convert to grayscale (luminance formula)
							byte luminance = (byte)((r * 0.299 + g * 0.587 + b * 0.114));
							pixelData[y * width + x] = luminance;
						}
					}
				}
				finally
				{
					bmp.UnlockBits(bmpData);
					bmp.Dispose();
				}

				// Write DDS file
				Console.WriteLine("Writing DDS file...");
				FileStream fs = new FileStream(ddsPath, FileMode.Create);
				BinaryWriter bw = new BinaryWriter(fs);
				
				try
				{
					// Write DDS header
					WriteDDSHeader(bw, width, height);
					
					// Write pixel data
					bw.Write(pixelData);
				}
				finally
				{
					bw.Close();
					fs.Close();
				}

				Console.WriteLine();
				Console.WriteLine("========================================");
				Console.WriteLine("CONVERSION COMPLETE!");
				Console.WriteLine("========================================");
				Console.WriteLine("Output: " + ddsPath);
				Console.WriteLine("Size: " + (new FileInfo(ddsPath).Length / 1024) + " KB");
				Console.WriteLine("Format: L8 (8-bit Luminance)");
				Console.WriteLine();
				
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("ERROR: " + ex.Message);
				return false;
			}
		}

		static void WriteDDSHeader(BinaryWriter bw, int width, int height)
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
			
			// DDS_PIXELFORMAT (L8 format)
			bw.Write(32); // dwSize
			bw.Write(0x20000); // dwFlags: DDPF_LUMINANCE
			bw.Write(0); // dwFourCC
			bw.Write(8); // dwRGBBitCount
			bw.Write(0xFF); // dwRBitMask
			bw.Write(0); // dwGBitMask
			bw.Write(0); // dwBBitMask
			bw.Write(0); // dwABitMask
			
			// DDS_HEADER dwCaps
			bw.Write(0x1000); // dwCaps: DDSCAPS_TEXTURE
			bw.Write(0); // dwCaps2
			bw.Write(0); // dwCaps3
			bw.Write(0); // dwCaps4
			bw.Write(0); // dwReserved2
		}
	}
}

