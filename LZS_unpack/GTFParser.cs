using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace LZS_unpack
{
    /// <summary>
    /// Парсер Sony GTF (Graphics Texture Format) файлов
    /// Поддерживает извлечение текстур в PNG формат
    /// </summary>
    public class GTFParser
    {
        /// <summary>
        /// GTF заголовок (базовая структура)
        /// </summary>
        public struct GTFHeader
        {
            public uint Magic;           // 0x04010000 (GTF signature)
            public uint Version;         // Версия формата
            public uint FileSize;        // Размер файла
            public uint HeaderSize;      // Размер заголовка
            public uint TextureCount;    // Количество текстур
            public uint TextureOffset;   // Смещение к данным текстур
            public uint TextureSize;     // Размер данных текстур
            public uint Format;          // Формат текстуры
            public uint Width;           // Ширина
            public uint Height;          // Высота
            public uint MipCount;        // Количество mip-уровней
            public uint Flags;           // Флаги
        }

        /// <summary>
        /// Информация о текстуре
        /// </summary>
        public struct TextureInfo
        {
            public uint Format;
            public uint Width;
            public uint Height;
            public uint MipCount;
            public uint DataSize;
            public uint DataOffset;
        }

        /// <summary>
        /// Конвертирует GTF файл в PNG
        /// </summary>
        public static void ConvertGTFToPNG(string gtfPath, string pngPath)
        {
            try
            {
                Console.WriteLine($"Converting GTF to PNG: {gtfPath} -> {pngPath}");
                
                using (FileStream fs = new FileStream(gtfPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // Сначала пытаемся прочитать как настоящий GTF файл
                    try
                    {
                        GTFHeader header = ReadGTFHeader(br);
                        
                        Console.WriteLine($"GTF Header:");
                        Console.WriteLine($"  Magic: 0x{header.Magic:X8}");
                        Console.WriteLine($"  Version: {header.Version}");
                        Console.WriteLine($"  File Size: {header.FileSize} bytes");
                        Console.WriteLine($"  Texture Count: {header.TextureCount}");
                        Console.WriteLine($"  Format: 0x{header.Format:X8}");
                        Console.WriteLine($"  Dimensions: {header.Width}x{header.Height}");
                        
                        // Получаем информацию о текстуре
                        TextureInfo texInfo = GetTextureInfo(header, br);
                        
                        Console.WriteLine($"Texture Info:");
                        Console.WriteLine($"  Format: {GetFormatName(texInfo.Format)}");
                        Console.WriteLine($"  Size: {texInfo.Width}x{texInfo.Height}");
                        Console.WriteLine($"  Mip Levels: {texInfo.MipCount}");
                        Console.WriteLine($"  Data Size: {texInfo.DataSize} bytes");
                        Console.WriteLine($"  Data Offset: 0x{texInfo.DataOffset:X}");
                        
                        // Извлекаем и конвертируем текстуру
                        Bitmap gtfBitmap = ExtractTexture(texInfo, br);
                        
                        // Сохраняем PNG
                        gtfBitmap.Save(pngPath, ImageFormat.Png);
                        Console.WriteLine($"Successfully saved PNG: {pngPath}");
                        return;
                    }
                    catch (InvalidDataException)
                    {
                        Console.WriteLine("Not a standard GTF file, trying raw L8 texture extraction...");
                    }
                    
                    // Если не GTF, пытаемся извлечь как сырые L8 данные
                    // Предполагаем размер 2048x2048 (наиболее частый для шрифтов)
                    fs.Seek(0, SeekOrigin.Begin);
                    
                    long fileSize = fs.Length;
                    Console.WriteLine($"File size: {fileSize} bytes");
                    
                    // Вычисляем размеры на основе размера файла
                    uint width = 2048, height = 2048; // По умолчанию
                    
                    if (fileSize == 4194304) // 2048x2048 L8
                    {
                        width = 2048; height = 2048;
                        Console.WriteLine("Detected 2048x2048 L8 texture");
                    }
                    else if (fileSize == 1048576) // 1024x1024 L8
                    {
                        width = 1024; height = 1024;
                        Console.WriteLine("Detected 1024x1024 L8 texture");
                    }
                    else if (fileSize == 262144) // 512x512 L8
                    {
                        width = 512; height = 512;
                        Console.WriteLine("Detected 512x512 L8 texture");
                    }
                    else
                    {
                        // Пытаемся угадать размер
                        uint estimatedSize = (uint)Math.Sqrt(fileSize);
                        width = height = estimatedSize;
                        Console.WriteLine($"Estimated texture size: {width}x{height}");
                    }
                    
                    // Извлекаем L8 текстуру
                    Bitmap bitmap = ExtractRawL8Texture(width, height, br);
                    
                    // Сохраняем PNG
                    bitmap.Save(pngPath, ImageFormat.Png);
                    Console.WriteLine($"Successfully saved PNG: {pngPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting GTF to PNG: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Читает GTF заголовок
        /// </summary>
        private static GTFHeader ReadGTFHeader(BinaryReader br)
        {
            GTFHeader header = new GTFHeader();
            
            header.Magic = br.ReadUInt32();
            if (header.Magic != 0x04010000)
            {
                throw new InvalidDataException($"Invalid GTF magic: 0x{header.Magic:X8}");
            }
            
            header.Version = br.ReadUInt32();
            header.FileSize = br.ReadUInt32();
            header.HeaderSize = br.ReadUInt32();
            header.TextureCount = br.ReadUInt32();
            header.TextureOffset = br.ReadUInt32();
            header.TextureSize = br.ReadUInt32();
            header.Format = br.ReadUInt32();
            header.Width = br.ReadUInt32();
            header.Height = br.ReadUInt32();
            header.MipCount = br.ReadUInt32();
            header.Flags = br.ReadUInt32();
            
            return header;
        }

        /// <summary>
        /// Получает информацию о текстуре
        /// </summary>
        private static TextureInfo GetTextureInfo(GTFHeader header, BinaryReader br)
        {
            TextureInfo info = new TextureInfo();
            
            info.Format = header.Format;
            info.Width = header.Width;
            info.Height = header.Height;
            info.MipCount = header.MipCount;
            info.DataOffset = header.TextureOffset;
            info.DataSize = header.TextureSize;
            
            return info;
        }

        /// <summary>
        /// Извлекает текстуру из GTF данных
        /// </summary>
        private static Bitmap ExtractTexture(TextureInfo texInfo, BinaryReader br)
        {
            // Переходим к данным текстуры
            br.BaseStream.Seek(texInfo.DataOffset, SeekOrigin.Begin);
            
            switch (texInfo.Format)
            {
                case 0x00000001: // L8 (8-bit Luminance)
                    return ExtractL8Texture(texInfo, br);
                case 0x00000002: // L8A8 (8-bit Luminance + Alpha)
                    return ExtractL8A8Texture(texInfo, br);
                case 0x00000003: // RGB888
                    return ExtractRGB888Texture(texInfo, br);
                case 0x00000004: // RGBA8888
                    return ExtractRGBA8888Texture(texInfo, br);
                default:
                    Console.WriteLine($"Warning: Unsupported texture format 0x{texInfo.Format:X8}, trying L8 extraction");
                    return ExtractL8Texture(texInfo, br);
            }
        }

        /// <summary>
        /// Извлекает L8 текстуру (8-bit Luminance)
        /// </summary>
        private static Bitmap ExtractL8Texture(TextureInfo texInfo, BinaryReader br)
        {
            Console.WriteLine("Extracting L8 texture...");
            
            Bitmap bitmap = new Bitmap((int)texInfo.Width, (int)texInfo.Height, PixelFormat.Format24bppRgb);
            
            // Читаем L8 данные
            byte[] l8Data = br.ReadBytes((int)texInfo.DataSize);
            
            // Конвертируем L8 в RGB
            for (int y = 0; y < texInfo.Height; y++)
            {
                for (int x = 0; x < texInfo.Width; x++)
                {
                    int pixelIndex = y * (int)texInfo.Width + x;
                    if (pixelIndex < l8Data.Length)
                    {
                        byte luminance = l8Data[pixelIndex];
                        Color color = Color.FromArgb(luminance, luminance, luminance);
                        bitmap.SetPixel(x, y, color);
                    }
                }
            }
            
            Console.WriteLine($"Extracted L8 texture: {texInfo.Width}x{texInfo.Height}, {l8Data.Length} bytes");
            return bitmap;
        }

        /// <summary>
        /// Извлекает L8A8 текстуру (8-bit Luminance + Alpha)
        /// </summary>
        private static Bitmap ExtractL8A8Texture(TextureInfo texInfo, BinaryReader br)
        {
            Console.WriteLine("Extracting L8A8 texture...");
            
            Bitmap bitmap = new Bitmap((int)texInfo.Width, (int)texInfo.Height, PixelFormat.Format32bppArgb);
            
            // Читаем L8A8 данные (2 байта на пиксель)
            byte[] l8a8Data = br.ReadBytes((int)texInfo.DataSize);
            
            // Конвертируем L8A8 в RGBA
            for (int y = 0; y < texInfo.Height; y++)
            {
                for (int x = 0; x < texInfo.Width; x++)
                {
                    int pixelIndex = (y * (int)texInfo.Width + x) * 2;
                    if (pixelIndex + 1 < l8a8Data.Length)
                    {
                        byte luminance = l8a8Data[pixelIndex];
                        byte alpha = l8a8Data[pixelIndex + 1];
                        Color color = Color.FromArgb(alpha, luminance, luminance, luminance);
                        bitmap.SetPixel(x, y, color);
                    }
                }
            }
            
            Console.WriteLine($"Extracted L8A8 texture: {texInfo.Width}x{texInfo.Height}, {l8a8Data.Length} bytes");
            return bitmap;
        }

        /// <summary>
        /// Извлекает RGB888 текстуру
        /// </summary>
        private static Bitmap ExtractRGB888Texture(TextureInfo texInfo, BinaryReader br)
        {
            Console.WriteLine("Extracting RGB888 texture...");
            
            Bitmap bitmap = new Bitmap((int)texInfo.Width, (int)texInfo.Height, PixelFormat.Format24bppRgb);
            
            // Читаем RGB888 данные (3 байта на пиксель)
            byte[] rgbData = br.ReadBytes((int)texInfo.DataSize);
            
            // Конвертируем RGB888 в Bitmap
            for (int y = 0; y < texInfo.Height; y++)
            {
                for (int x = 0; x < texInfo.Width; x++)
                {
                    int pixelIndex = (y * (int)texInfo.Width + x) * 3;
                    if (pixelIndex + 2 < rgbData.Length)
                    {
                        byte r = rgbData[pixelIndex];
                        byte g = rgbData[pixelIndex + 1];
                        byte b = rgbData[pixelIndex + 2];
                        Color color = Color.FromArgb(r, g, b);
                        bitmap.SetPixel(x, y, color);
                    }
                }
            }
            
            Console.WriteLine($"Extracted RGB888 texture: {texInfo.Width}x{texInfo.Height}, {rgbData.Length} bytes");
            return bitmap;
        }

        /// <summary>
        /// Извлекает RGBA8888 текстуру
        /// </summary>
        private static Bitmap ExtractRGBA8888Texture(TextureInfo texInfo, BinaryReader br)
        {
            Console.WriteLine("Extracting RGBA8888 texture...");
            
            Bitmap bitmap = new Bitmap((int)texInfo.Width, (int)texInfo.Height, PixelFormat.Format32bppArgb);
            
            // Читаем RGBA8888 данные (4 байта на пиксель)
            byte[] rgbaData = br.ReadBytes((int)texInfo.DataSize);
            
            // Конвертируем RGBA8888 в Bitmap
            for (int y = 0; y < texInfo.Height; y++)
            {
                for (int x = 0; x < texInfo.Width; x++)
                {
                    int pixelIndex = (y * (int)texInfo.Width + x) * 4;
                    if (pixelIndex + 3 < rgbaData.Length)
                    {
                        byte r = rgbaData[pixelIndex];
                        byte g = rgbaData[pixelIndex + 1];
                        byte b = rgbaData[pixelIndex + 2];
                        byte a = rgbaData[pixelIndex + 3];
                        Color color = Color.FromArgb(a, r, g, b);
                        bitmap.SetPixel(x, y, color);
                    }
                }
            }
            
            Console.WriteLine($"Extracted RGBA8888 texture: {texInfo.Width}x{texInfo.Height}, {rgbaData.Length} bytes");
            return bitmap;
        }

        /// <summary>
        /// Извлекает сырую L8 текстуру без заголовка
        /// </summary>
        private static Bitmap ExtractRawL8Texture(uint width, uint height, BinaryReader br)
        {
            Console.WriteLine($"Extracting raw L8 texture: {width}x{height}");
            
            Bitmap bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);
            
            // Читаем все L8 данные
            byte[] l8Data = br.ReadBytes((int)(width * height));
            
            Console.WriteLine($"Read {l8Data.Length} bytes of L8 data");
            
            // Конвертируем L8 в RGB
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = y * (int)width + x;
                    if (pixelIndex < l8Data.Length)
                    {
                        byte luminance = l8Data[pixelIndex];
                        Color color = Color.FromArgb(luminance, luminance, luminance);
                        bitmap.SetPixel(x, y, color);
                    }
                }
            }
            
            Console.WriteLine($"Successfully extracted raw L8 texture: {width}x{height}, {l8Data.Length} bytes");
            return bitmap;
        }

        /// <summary>
        /// Возвращает название формата
        /// </summary>
        private static string GetFormatName(uint format)
        {
            switch (format)
            {
                case 0x00000001: return "L8 (8-bit Luminance)";
                case 0x00000002: return "L8A8 (8-bit Luminance + Alpha)";
                case 0x00000003: return "RGB888";
                case 0x00000004: return "RGBA8888";
                case 0x00000005: return "DXT1";
                case 0x00000006: return "DXT3";
                case 0x00000007: return "DXT5";
                default: return $"Unknown (0x{format:X8})";
            }
        }
    }
}
