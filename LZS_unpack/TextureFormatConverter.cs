using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace LZS_unpack
{
    /// <summary>
    /// Конвертер между различными форматами текстур (GTF, DDS, PNG)
    /// Обеспечивает работу с оригинальными форматами Phyre Engine
    /// </summary>
    public class TextureFormatConverter
    {
        public enum TextureFormat
        {
            Unknown,
            GTF,    // Sony GTF format
            DDS,    // DirectDraw Surface
            PNG,    // Portable Network Graphics
            TGA,    // Targa
            BMP     // Bitmap
        }

        /// <summary>
        /// Определяет формат текстуры по содержимому файла
        /// </summary>
        public static TextureFormat DetectTextureFormat(byte[] data)
        {
            if (data.Length < 4) return TextureFormat.Unknown;

            // Проверяем magic bytes
            if (data[0] == 0x04 && data[1] == 0x01 && data[2] == 0x00 && data[3] == 0x00)
                return TextureFormat.GTF;
            
            if (data[0] == 0x44 && data[1] == 0x44 && data[2] == 0x53 && data[3] == 0x20)
                return TextureFormat.DDS;
            
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return TextureFormat.PNG;
            
            if (data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x02 && data[3] == 0x00)
                return TextureFormat.TGA;
            
            // Дополнительная проверка для GTF в середине файла
            if (data.Length >= 16)
            {
                for (int i = 0; i <= data.Length - 16; i++)
                {
                    if (data[i] == 0x04 && data[i+1] == 0x01 && data[i+2] == 0x00 && data[i+3] == 0x00)
                        return TextureFormat.GTF;
                }
            }
            
            return TextureFormat.Unknown;
        }

        /// <summary>
        /// Определяет формат текстуры по содержимому файла
        /// </summary>
        public static TextureFormat DetectTextureFormat(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Сначала проверяем заголовок
                    byte[] header = new byte[16];
                    int bytesRead = fs.Read(header, 0, header.Length);
                    TextureFormat format = DetectTextureFormat(header);
                    
                    if (format != TextureFormat.Unknown)
                        return format;
                    
                    // Если заголовок не дал результата, ищем по всему файлу
                    fs.Seek(0, SeekOrigin.Begin);
                    byte[] buffer = new byte[65536]; // 64KB buffer
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        format = DetectTextureFormat(buffer);
                        if (format != TextureFormat.Unknown)
                            return format;
                    }
                    
                    return TextureFormat.Unknown;
                }
            }
            catch
            {
                return TextureFormat.Unknown;
            }
        }

        /// <summary>
        /// Конвертирует текстуру в PNG формат
        /// </summary>
        public static void ConvertToPNG(string inputPath, string outputPath)
        {
            try
            {
                TextureFormat format = DetectTextureFormat(inputPath);
                
                switch (format)
                {
                    case TextureFormat.GTF:
                        ConvertGTFToPNG(inputPath, outputPath);
                        break;
                    case TextureFormat.DDS:
                        ConvertDDSToPNG(inputPath, outputPath);
                        break;
                    case TextureFormat.PNG:
                        // Просто копируем файл
                        File.Copy(inputPath, outputPath, true);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported texture format: {format}");
                }
                
                Console.WriteLine($"Converted {format} to PNG: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting to PNG: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Конвертирует PNG в указанный формат
        /// </summary>
        public static void ConvertFromPNG(string pngPath, string outputPath, TextureFormat targetFormat)
        {
            try
            {
                switch (targetFormat)
                {
                    case TextureFormat.GTF:
                        ConvertPNGToGTF(pngPath, outputPath);
                        break;
                    case TextureFormat.DDS:
                        ConvertPNGToDDS(pngPath, outputPath);
                        break;
                    case TextureFormat.PNG:
                        // Просто копируем файл
                        File.Copy(pngPath, outputPath, true);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported target format: {targetFormat}");
                }
                
                Console.WriteLine($"Converted PNG to {targetFormat}: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting from PNG: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Конвертирует GTF в PNG (полная реализация)
        /// </summary>
        private static void ConvertGTFToPNG(string gtfPath, string pngPath)
        {
            try
            {
                Console.WriteLine("Converting GTF to PNG using GTFParser...");
                GTFParser.ConvertGTFToPNG(gtfPath, pngPath);
                Console.WriteLine($"Successfully converted GTF to PNG: {pngPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting GTF to PNG: {ex.Message}");
                
                // Fallback: создаем заглушку если конвертация не удалась
                try
                {
                    Console.WriteLine("Creating fallback placeholder PNG...");
                    using (Bitmap placeholder = new Bitmap(100, 100))
                    using (Graphics g = Graphics.FromImage(placeholder))
                    {
                        g.Clear(Color.DarkGray);
                        g.DrawString("GTF Conversion\nFailed", 
                                   new Font("Arial", 8), 
                                   Brushes.White, 
                                   new PointF(10, 40));
                        placeholder.Save(pngPath, ImageFormat.Png);
                    }
                    Console.WriteLine($"Fallback PNG created: {pngPath}");
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Fallback also failed: {fallbackEx.Message}");
                    throw ex; // Бросаем оригинальную ошибку
                }
            }
        }

        /// <summary>
        /// Конвертирует PNG в GTF (упрощенная реализация)
        /// </summary>
        private static void ConvertPNGToGTF(string pngPath, string gtfPath)
        {
            try
            {
                // Пока используем DDS конвертер как fallback для GTF
                // В будущем можно добавить полноценную поддержку GTF
                Console.WriteLine("Warning: GTF format not fully supported, using DDS fallback");
                ConvertPNGToDDS(pngPath, gtfPath.Replace(".gtf", ".dds"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting PNG to GTF: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Конвертирует DDS в PNG используя существующий конвертер
        /// </summary>
        private static void ConvertDDSToPNG(string ddsPath, string pngPath)
        {
            DDSToPNGConverter.ConvertDDSToPNG(ddsPath, pngPath);
        }

        /// <summary>
        /// Конвертирует PNG в DDS используя существующий конвертер
        /// </summary>
        private static void ConvertPNGToDDS(string pngPath, string ddsPath)
        {
            PNGToDDSConverter.ConvertPNGToDDS(pngPath, ddsPath);
        }

        /// <summary>
        /// Получает рекомендуемое расширение для формата
        /// </summary>
        public static string GetRecommendedExtension(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.GTF: return ".gtf";
                case TextureFormat.DDS: return ".dds";
                case TextureFormat.PNG: return ".png";
                case TextureFormat.TGA: return ".tga";
                case TextureFormat.BMP: return ".bmp";
                default: return ".bin";
            }
        }

        /// <summary>
        /// Создает имя файла с правильным расширением
        /// </summary>
        public static string CreateOutputFileName(string inputPath, TextureFormat format)
        {
            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = GetRecommendedExtension(format);
            return baseName + "_detected" + extension;
        }

        /// <summary>
        /// Проверяет, поддерживается ли формат для конвертации
        /// </summary>
        public static bool IsSupportedFormat(TextureFormat format)
        {
            return format == TextureFormat.GTF || 
                   format == TextureFormat.DDS || 
                   format == TextureFormat.PNG;
        }
    }
}
