using System;
using System.IO;

namespace LZS_unpack
{
	/// <summary>
	/// DDS texture extractor for Phyre Engine
	/// </summary>
	internal class DDSExtractor
	{
		// DDS format constants
		private const uint DDS_MAGIC = 0x20534444; // "DDS "
		private const uint DDSD_CAPS = 0x1;
		private const uint DDSD_HEIGHT = 0x2;
		private const uint DDSD_WIDTH = 0x4;
		private const uint DDSD_PITCH = 0x8;
		private const uint DDSD_PIXELFORMAT = 0x1000;
		private const uint DDSD_MIPMAPCOUNT = 0x20000;
		private const uint DDSD_LINEARSIZE = 0x80000;
		private const uint DDSD_DEPTH = 0x800000;

		private const uint DDPF_ALPHAPIXELS = 0x1;
		private const uint DDPF_ALPHA = 0x2;
		private const uint DDPF_FOURCC = 0x4;
		private const uint DDPF_RGB = 0x40;
		private const uint DDPF_YUV = 0x200;
		private const uint DDPF_LUMINANCE = 0x20000;

		public static void ExtractTexture(string phyreFile, long textureOffset, int width, int height, string outputPath)
		{
			FileStream fs = new FileStream(phyreFile, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				// Seek to texture data
				fs.Seek(textureOffset, SeekOrigin.Begin);

				Console.WriteLine("Extracting texture:");
				Console.WriteLine("  Size: " + width + "x" + height);
				Console.WriteLine("  Offset: 0x" + textureOffset.ToString("X"));

				// Read texture data (estimate size based on dimensions)
				// DXT5 compressed: 1 byte per pixel
				int estimatedSize = width * height;
				byte[] textureData = br.ReadBytes(estimatedSize);

				Console.WriteLine("  Read: " + textureData.Length + " bytes");

				// Create DDS file
				FileStream ddsFs = new FileStream(outputPath, FileMode.Create);
				BinaryWriter ddsWriter = new BinaryWriter(ddsFs);

				// Write DDS header
				WriteDDSHeader(ddsWriter, width, height, textureData.Length);

				// Write texture data
				ddsWriter.Write(textureData);

				ddsWriter.Close();
				ddsFs.Close();

				Console.WriteLine("  Saved: " + outputPath);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error extracting texture: " + ex.Message);
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		private static void WriteDDSHeader(BinaryWriter writer, int width, int height, int dataSize)
		{
			// DDS magic
			writer.Write(DDS_MAGIC);

			// DDS_HEADER (124 bytes)
			writer.Write((uint)124); // dwSize
			writer.Write(DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_LINEARSIZE); // dwFlags
			writer.Write((uint)height); // dwHeight
			writer.Write((uint)width); // dwWidth
			writer.Write((uint)dataSize); // dwPitchOrLinearSize
			writer.Write((uint)0); // dwDepth
			writer.Write((uint)0); // dwMipMapCount
			
			// dwReserved1[11]
			for (int i = 0; i < 11; i++)
				writer.Write((uint)0);

			// DDS_PIXELFORMAT (32 bytes)
			writer.Write((uint)32); // dwSize
			writer.Write(DDPF_FOURCC); // dwFlags
			writer.Write((uint)0x35545844); // dwFourCC = "DXT5"
			writer.Write((uint)0); // dwRGBBitCount
			writer.Write((uint)0); // dwRBitMask
			writer.Write((uint)0); // dwGBitMask
			writer.Write((uint)0); // dwBBitMask
			writer.Write((uint)0); // dwABitMask

			// DDS_CAPS (16 bytes)
			writer.Write((uint)0x1000); // dwCaps
			writer.Write((uint)0); // dwCaps2
			writer.Write((uint)0); // dwCaps3
			writer.Write((uint)0); // dwCaps4

			// dwReserved2
			writer.Write((uint)0);
		}

		public static void FindAndExtractTextures(string phyreFile, string outputBaseName)
		{
			Console.WriteLine("=== Searching for Textures ===");

			FileStream fs = new FileStream(phyreFile, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				// Read header to find texture objects
				br.ReadInt32(); // Magic
				int offset1 = br.ReadInt32();
				int offset2 = br.ReadInt32();

				// Jump to object instances
				fs.Seek((long)(offset1 + offset2), SeekOrigin.Begin);

				// Search through instances for texture data
				int textureCount = 0;
				
				for (int i = 0; i < 100 && fs.Position < fs.Length - 100; i++)
				{
					long instancePos = fs.Position;
					int classId = br.ReadInt32() - 1;
					int count = br.ReadInt32();
					int dataOffset = br.ReadInt32();
					
					// Read more fields to find texture dimensions
					int field4 = br.ReadInt32();
					int field5 = br.ReadInt32();
					int field6 = br.ReadInt32();
					
					// Skip rest
					for (int j = 0; j < 3; j++) br.ReadInt32();

					// Check if this looks like texture data
					// Typical: count = 1, reasonable dataOffset, field4/5 might be dimensions
					if (count == 1 && dataOffset > 0 && dataOffset < fs.Length)
					{
						// Try to interpret as texture
						if (field4 > 0 && field4 <= 4096 && field5 > 0 && field5 <= 4096)
						{
							// field4 and field5 might be width/height
							Console.WriteLine("Found potential texture:");
							Console.WriteLine("  Instance " + i + ": offset=" + dataOffset + 
								", possible size=" + field4 + "x" + field5);

							// Try to extract
							string texPath = outputBaseName + "_tex" + textureCount + ".dds";
							
							try
							{
								long savedPos = fs.Position;
								ExtractTexture(phyreFile, dataOffset, field4, field5, texPath);
								fs.Seek(savedPos, SeekOrigin.Begin);
								textureCount++;
							}
							catch
							{
								// Skip if extraction fails
							}
						}
					}
				}

				if (textureCount == 0)
				{
					Console.WriteLine("No textures found automatically.");
					Console.WriteLine("Trying alternative method...");
					TryBruteForceTextureSearch(phyreFile, outputBaseName);
				}
				else
				{
					Console.WriteLine();
					Console.WriteLine("Extracted " + textureCount + " texture(s)");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error searching textures: " + ex.Message);
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}

		private static void TryBruteForceTextureSearch(string phyreFile, string outputBaseName)
		{
			// Search for DDS magic or compressed texture patterns
			FileStream fs = new FileStream(phyreFile, FileMode.Open, FileAccess.Read, FileShare.Read);
			BinaryReader br = new BinaryReader(fs);

			try
			{
				byte[] buffer = new byte[4096];
				long fileSize = fs.Length;
				int textureCount = 0;

				// Common texture sizes for fonts
				int[] commonSizes = { 256, 512, 1024, 2048, 4096 };

				for (long pos = 0; pos < fileSize - 100000; pos += 1024)
				{
					fs.Seek(pos, SeekOrigin.Begin);
					int bytesRead = fs.Read(buffer, 0, buffer.Length);

					// Look for patterns that might indicate texture data
					// DXT compressed data has specific byte patterns
					
					if (bytesRead >= 1024 && textureCount < 5)
					{
						// Check if this looks like compressed texture
						bool looksLikeTexture = false;
						
						// DXT compressed data often has repeating patterns
						// and specific byte value distributions
						int uniqueBytes = 0;
						bool[] seen = new bool[256];
						
						for (int i = 0; i < Math.Min(1024, bytesRead); i++)
						{
							if (!seen[buffer[i]])
							{
								seen[buffer[i]] = true;
								uniqueBytes++;
							}
						}

						// Compressed data typically has good byte distribution
						if (uniqueBytes > 100)
						{
							// Try extracting with common sizes
							foreach (int size in commonSizes)
							{
								if (pos + size * size <= fileSize)
								{
									string texPath = outputBaseName + "_bruteforce_" + size + "x" + size + "_" + textureCount + ".dds";
									
									try
									{
										ExtractTexture(phyreFile, pos, size, size, texPath);
										Console.WriteLine("Extracted potential texture: " + texPath);
										textureCount++;
										break;
									}
									catch
									{
										// Skip
									}
								}
							}
						}
					}
				}

				Console.WriteLine("Brute force search completed. Found " + textureCount + " potential textures.");
				Console.WriteLine("Note: Some extracted files might not be valid textures.");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error in brute force search: " + ex.Message);
			}
			finally
			{
				br.Close();
				fs.Close();
			}
		}
	}
}

