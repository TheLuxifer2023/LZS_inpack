using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace LZS_unpack
{
    /// <summary>
    /// Автоматическое распознавание форматов файлов по magic bytes и сигнатурам
    /// </summary>
    public class FormatDetector
    {
        public enum FileFormat
        {
            Unknown,
            Phyre,
            PhyreFont,
            PhyreModel,
            PhyreTexture,
            BMFont,
            BMFontBinary,
            DDS,
            GTF,
            PNG,
            JPEG,
            TGA,
            CustomFont,
            CustomTexture
        }

        public class FormatInfo
        {
            public FileFormat Format { get; set; }
            public string Description { get; set; }
            public string RecommendedExtension { get; set; }
            public bool IsValid { get; set; }
            public byte[] MagicBytes { get; set; }
        }

        /// <summary>
        /// Распознает формат файла по его содержимому
        /// </summary>
        public static FormatInfo DetectFormat(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new FormatInfo 
                { 
                    Format = FileFormat.Unknown, 
                    Description = "File not found",
                    IsValid = false 
                };
            }

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return DetectFormat(fs);
            }
        }

        /// <summary>
        /// Распознает формат по потоку данных
        /// </summary>
        public static FormatInfo DetectFormat(Stream stream)
        {
            long originalPosition = stream.Position;
            FormatInfo result = new FormatInfo();

            try
            {
                // Читаем первые 64 байта для анализа
                byte[] header = new byte[64];
                int bytesRead = stream.Read(header, 0, header.Length);
                
                if (bytesRead < 4)
                {
                    result.Format = FileFormat.Unknown;
                    result.Description = "File too small";
                    result.IsValid = false;
                    return result;
                }

                result.MagicBytes = new byte[Math.Min(bytesRead, 16)];
                Array.Copy(header, result.MagicBytes, result.MagicBytes.Length);

                // Проверяем различные форматы
                result = DetectByMagicBytes(header, bytesRead);
                
                // Если это Phyre файл, анализируем его содержимое для определения типа
                if (result.Format == FileFormat.Phyre)
                {
                    result = AnalyzePhyreContent(stream, header, bytesRead);
                }
                
                // Если не определили по magic bytes, анализируем содержимое
                if (result.Format == FileFormat.Unknown)
                {
                    result = AnalyzeContent(stream, header, bytesRead);
                }

                return result;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Распознавание по magic bytes
        /// </summary>
        private static FormatInfo DetectByMagicBytes(byte[] header, int length)
        {
            FormatInfo info = new FormatInfo();

            // Проверяем известные magic bytes
            if (length >= 4)
            {
                uint magic32 = BitConverter.ToUInt32(header, 0);
                string magicStr = Encoding.ASCII.GetString(header, 0, Math.Min(8, length));

                switch (magic32)
                {
                    case 0x50485952: // "PHYR"
                        info.Format = FileFormat.Phyre;
                        info.Description = "Phyre Engine Archive";
                        info.RecommendedExtension = ".phyre";
                        info.IsValid = true;
                        break;

                    case 0x44445320: // "DDS "
                        info.Format = FileFormat.DDS;
                        info.Description = "DirectDraw Surface Texture";
                        info.RecommendedExtension = ".dds";
                        info.IsValid = true;
                        break;

                    case 0x47494638: // "GIF8"
                        if (length >= 6 && header[4] == 0x39 && header[5] == 0x61) // "GIF89a"
                        {
                            info.Format = FileFormat.Unknown; // Не поддерживаем GIF для шрифтов
                            info.Description = "GIF Image (not supported for fonts)";
                            info.IsValid = false;
                        }
                        break;

                    case 0x89504E47: // PNG
                        info.Format = FileFormat.PNG;
                        info.Description = "Portable Network Graphics";
                        info.RecommendedExtension = ".png";
                        info.IsValid = true;
                        break;

                    case 0xFFD8FFE0: // JPEG
                    case 0xFFD8FFE1:
                        info.Format = FileFormat.JPEG;
                        info.Description = "JPEG Image";
                        info.RecommendedExtension = ".jpg";
                        info.IsValid = false; // JPEG не подходит для шрифтов
                        break;
                }

                // Проверяем текстовые сигнатуры
                if (magicStr.StartsWith("BMF"))
                {
                    info.Format = FileFormat.BMFontBinary;
                    info.Description = "Binary BMFont Format";
                    info.RecommendedExtension = ".fnt";
                    info.IsValid = true;
                }
                else if (magicStr.StartsWith("info "))
                {
                    info.Format = FileFormat.BMFont;
                    info.Description = "Text BMFont Format";
                    info.RecommendedExtension = ".fnt";
                    info.IsValid = true;
                }
                else if (magicStr.StartsWith("GTF"))
                {
                    info.Format = FileFormat.GTF;
                    info.Description = "Sony GTF Texture Format";
                    info.RecommendedExtension = ".gtf";
                    info.IsValid = true;
                }
            }

            return info;
        }

        /// <summary>
        /// Анализ содержимого Phyre файла для определения реальных форматов данных внутри
        /// Ищет сигнатуры FNT, BMF, DDS, GTF внутри архива
        /// </summary>
        private static FormatInfo AnalyzePhyreContent(Stream stream, byte[] header, int length)
        {
            FormatInfo info = new FormatInfo();
            info.Format = FileFormat.Phyre; // По умолчанию
            info.Description = "Phyre Engine Archive";
            info.RecommendedExtension = ".phyre";
            info.IsValid = true;

            try
            {
                // Сканируем весь файл на предмет сигнатур форматов
                List<string> foundSignatures = new List<string>();
                List<string> foundFormats = new List<string>();
                
                // Читаем файл блоками для поиска сигнатур
                byte[] buffer = new byte[4096];
                long fileSize = stream.Length;
                long position = 0;
                
                while (position < fileSize)
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        // Ищем сигнатуры в буфере
                        for (int i = 0; i < bytesRead - 4; i++)
                        {
                            // Проверяем 4-байтовые сигнатуры
                            string signature = Encoding.ASCII.GetString(buffer, i, 4);
                            
                            switch (signature)
                            {
                                case "FNT ":
                                    foundSignatures.Add("FNT ");
                                    foundFormats.Add("BMFont Text Format (.fnt)");
                                    break;
                                    
                                case "BMF\x03":
                                case "BMF\x02":
                                case "BMF\x01":
                                    foundSignatures.Add("BMF");
                                    foundFormats.Add("BMFont Binary Format (.fnt)");
                                    break;
                                    
                                case "DDS ":
                                    foundSignatures.Add("DDS ");
                                    foundFormats.Add("DirectDraw Surface (.dds)");
                                    break;
                                    
                                case "\x04\x01\x00\x00": // GTF signature
                                    foundSignatures.Add("GTF");
                                    foundFormats.Add("Sony GTF Texture (.gtf)");
                                    break;
                                    
                                case "PNG\x0D\x0A\x1A\x0A": // PNG signature (если есть)
                                    foundSignatures.Add("PNG");
                                    foundFormats.Add("Portable Network Graphics (.png)");
                                    break;
                            }
                            
                            // Дополнительная проверка на BMFont бинарные сигнатуры
                            if (i < bytesRead - 8)
                            {
                                // Проверяем 8-байтовую сигнатуру BMFont
                                string bmfSignature = Encoding.ASCII.GetString(buffer, i, 4);
                                if (bmfSignature == "BMF" && i + 4 < bytesRead)
                                {
                                    byte version = buffer[i + 3];
                                    if (version >= 1 && version <= 3)
                                    {
                                        foundSignatures.Add("BMF");
                                        foundFormats.Add($"BMFont Binary Format v{version} (.fnt)");
                                    }
                                }
                            }
                        }
                    }
                    
                    position += bytesRead;
                }
                
                // Дополнительно ищем структуры данных шрифтов (PBitmapFontCharInfo)
                bool foundFontStructures = SearchForFontStructures(stream);
                
                // Анализируем найденные сигнатуры
                if (foundSignatures.Count > 0 || foundFontStructures)
                {
                    bool hasFont = foundSignatures.Any(s => s.Contains("FNT") || s.Contains("BMF")) || foundFontStructures;
                    bool hasTexture = foundSignatures.Any(s => s.Contains("DDS") || s.Contains("GTF") || s.Contains("PNG"));
                    
                    // Определяем тип и описание
                    if (hasFont && hasTexture)
                    {
                        info.Format = FileFormat.PhyreFont;
                        info.Description = $"Phyre Engine Font Archive (Contains: {string.Join(", ", foundFormats.Distinct())})";
                        info.RecommendedExtension = ".font.phyre";
                    }
                    else if (hasFont)
                    {
                        info.Format = FileFormat.PhyreFont;
                        info.Description = $"Phyre Engine Font Archive (Contains: {string.Join(", ", foundFormats.Distinct())})";
                        info.RecommendedExtension = ".font.phyre";
                    }
                    else if (hasTexture)
                    {
                        // Если нашли только текстуру, но есть структуры шрифтов - это все равно шрифт
                        if (foundFontStructures)
                        {
                            info.Format = FileFormat.PhyreFont;
                            info.Description = $"Phyre Engine Font Archive (Contains: {string.Join(", ", foundFormats.Distinct())} + Font Data Structures)";
                            info.RecommendedExtension = ".font.phyre";
                        }
                        else
                        {
                            info.Format = FileFormat.PhyreTexture;
                            info.Description = $"Phyre Engine Texture Archive (Contains: {string.Join(", ", foundFormats.Distinct())})";
                            info.RecommendedExtension = ".texture.phyre";
                        }
                    }
                    
                    // Добавляем информацию о найденных форматах
                    Console.WriteLine($"Found signatures: {string.Join(", ", foundSignatures.Distinct())}");
                    Console.WriteLine($"Detected formats: {string.Join(", ", foundFormats.Distinct())}");
                    if (foundFontStructures)
                    {
                        Console.WriteLine("Found font data structures (PBitmapFontCharInfo) - Raw binary font format");
                    }
                    
                    // Дополнительная информация о типе содержимого
                    if (hasFont && hasTexture)
                    {
                        Console.WriteLine("Content Type: Font Archive with embedded texture");
                    }
                    else if (hasFont)
                    {
                        Console.WriteLine("Content Type: Font Archive (no embedded texture)");
                    }
                    else if (hasTexture && foundFontStructures)
                    {
                        Console.WriteLine("Content Type: Font Archive with raw font data + embedded texture");
                    }
                    else if (hasTexture)
                    {
                        Console.WriteLine("Content Type: Texture Archive");
                    }
                }
                else
                {
                    // Если сигнатуры не найдены, используем анализ классов как fallback
                    info = AnalyzePhyreClasses(stream, header, length);
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки возвращаем базовую информацию о Phyre
                Console.WriteLine("Warning: Error analyzing Phyre content: " + ex.Message);
            }
            
            return info;
        }

        /// <summary>
        /// Поиск структур данных шрифтов (PBitmapFontCharInfo) внутри файла
        /// Ищет как сигнатуры форматов, так и структуры данных
        /// </summary>
        private static bool SearchForFontStructures(Stream stream)
        {
            try
            {
                // Сначала ищем сигнатуры шрифтовых форматов
                byte[] buffer = new byte[4096];
                long fileSize = stream.Length;
                long position = 0;
                bool foundFontSignatures = false;
                int foundSequences = 0;
                
                while (position < fileSize)
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        // Ищем сигнатуры шрифтовых форматов
                        for (int i = 0; i < bytesRead - 4; i++)
                        {
                            string signature = Encoding.ASCII.GetString(buffer, i, 4);
                            
                            // Проверяем на BMFont сигнатуры
                            if (signature == "FNT " || signature.StartsWith("BMF"))
                            {
                                foundFontSignatures = true;
                                Console.WriteLine($"Found font signature: {signature} at offset {position + i}");
                            }
                        }
                        
                        // Также ищем структуры данных шрифтов - последовательность ASCII кодов символов
                        for (int i = 0; i < bytesRead - 45; i += 4) // Проверяем каждые 4 байта (структура 45 байт)
                        {
                            // Читаем предполагаемый код символа (4 байта)
                            int charCode = BitConverter.ToInt32(buffer, i);
                            
                            // Проверяем, является ли это валидным ASCII кодом
                            if (charCode >= 32 && charCode <= 126)
                            {
                                // Проверяем следующие несколько символов подряд
                                int sequentialCount = 1;
                                for (int j = i + 45; j < Math.Min(i + 45 * 10, bytesRead - 45); j += 45)
                                {
                                    int nextCharCode = BitConverter.ToInt32(buffer, j);
                                    if (nextCharCode >= 32 && nextCharCode <= 126 && nextCharCode == charCode + sequentialCount)
                                    {
                                        sequentialCount++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                
                                // Если нашли последовательность из 5+ символов подряд
                                if (sequentialCount >= 5)
                                {
                                    foundSequences++;
                                    if (foundSequences >= 3) // Нашли достаточно последовательностей
                                    {
                                        Console.WriteLine($"Found font data structures: {foundSequences} sequences at offset {position + i}");
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    
                    position += bytesRead;
                }
                
                // Если нашли сигнатуры шрифтовых форматов, возвращаем true
                if (foundFontSignatures)
                {
                    Console.WriteLine("Found font format signatures inside archive");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Error searching for font structures: " + ex.Message);
            }
            
            return false;
        }

        /// <summary>
        /// Fallback анализ по классам Phyre (старый метод)
        /// </summary>
        private static FormatInfo AnalyzePhyreClasses(Stream stream, byte[] header, int length)
        {
            FormatInfo info = new FormatInfo();
            info.Format = FileFormat.Phyre;
            info.Description = "Phyre Engine Archive";
            info.RecommendedExtension = ".phyre";
            info.IsValid = true;

            try
            {
                if (length >= 72)
                {
                    uint magic = BitConverter.ToUInt32(header, 0);
                    if (magic != 0x50485952) return info;

                    int offset1 = BitConverter.ToInt32(header, 4);
                    int offset2 = BitConverter.ToInt32(header, 8);
                    
                    if (offset1 > 0 && offset1 < stream.Length && offset2 > 0 && offset2 < stream.Length)
                    {
                        stream.Seek((long)(offset1 + 8), SeekOrigin.Begin);
                        byte[] classHeader = new byte[12];
                        int bytesRead = stream.Read(classHeader, 0, 12);
                        
                        if (bytesRead >= 12)
                        {
                            int num12 = BitConverter.ToInt32(classHeader, 0);
                            int numClasses = BitConverter.ToInt32(classHeader, 4);
                            int num14 = BitConverter.ToInt32(classHeader, 8);
                            
                            stream.Seek((long)(num12 * 4 + 12), SeekOrigin.Current);
                            long stringTableStart = stream.Position + (long)(num14 * 24);
                            
                            bool foundFont = false;
                            bool foundTexture = false;
                            
                            for (int i = 0; i < Math.Min(numClasses, 20); i++)
                            {
                                byte[] classDef = new byte[32];
                                int defRead = stream.Read(classDef, 0, 32);
                                
                                if (defRead >= 32)
                                {
                                    int classNameOffset = BitConverter.ToInt32(classDef, 8);
                                    
                                    long currentPos = stream.Position;
                                    stream.Seek(stringTableStart + (long)classNameOffset, SeekOrigin.Begin);
                                    
                                    StringBuilder className = new StringBuilder();
                                    byte b;
                                    int maxRead = 0;
                                    while ((b = (byte)stream.ReadByte()) > 0 && maxRead < 50)
                                    {
                                        className.Append((char)b);
                                        maxRead++;
                                    }
                                    
                                    string classNameStr = className.ToString();
                                    
                                    if (classNameStr == "PBitmapFont" || classNameStr == "PBitmapFontCharInfo")
                                    {
                                        foundFont = true;
                                    }
                                    else if (classNameStr == "PTexture2D" || classNameStr.Contains("Texture"))
                                    {
                                        foundTexture = true;
                                    }
                                    
                                    stream.Seek(currentPos, SeekOrigin.Begin);
                                }
                            }
                            
                            if (foundFont)
                            {
                                info.Format = FileFormat.PhyreFont;
                                info.Description = "Phyre Engine Font Archive (Class Analysis)";
                                info.RecommendedExtension = ".font.phyre";
                            }
                            else if (foundTexture)
                            {
                                info.Format = FileFormat.PhyreTexture;
                                info.Description = "Phyre Engine Texture Archive (Class Analysis)";
                                info.RecommendedExtension = ".texture.phyre";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Error in class analysis: " + ex.Message);
            }
            
            return info;
        }

        /// <summary>
        /// Анализ содержимого для определения формата
        /// </summary>
        private static FormatInfo AnalyzeContent(Stream stream, byte[] header, int length)
        {
            FormatInfo info = new FormatInfo();

            // Если это текстовый файл
            if (IsTextFile(header, length))
            {
                string content = Encoding.ASCII.GetString(header, 0, length);
                
                if (content.Contains("info ") && content.Contains("common ") && content.Contains("char "))
                {
                    info.Format = FileFormat.BMFont;
                    info.Description = "Text BMFont Format (detected by content)";
                    info.RecommendedExtension = ".fnt";
                    info.IsValid = true;
                }
                else
                {
                    info.Format = FileFormat.Unknown;
                    info.Description = "Text file (unknown format)";
                    info.IsValid = false;
                }
            }
            else
            {
                // Анализируем бинарные данные
                info = AnalyzeBinaryContent(stream, header, length);
            }

            return info;
        }

        /// <summary>
        /// Проверяет, является ли файл текстовым
        /// </summary>
        private static bool IsTextFile(byte[] data, int length)
        {
            int textBytes = 0;
            int totalBytes = Math.Min(length, 512);

            for (int i = 0; i < totalBytes; i++)
            {
                byte b = data[i];
                // Проверяем на печатные ASCII символы, пробелы, табы, переводы строк
                if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
                {
                    textBytes++;
                }
            }

            // Если более 80% байтов - печатные символы, считаем текстовым
            return (textBytes * 100 / totalBytes) > 80;
        }

        /// <summary>
        /// Анализ бинарного содержимого
        /// </summary>
        private static FormatInfo AnalyzeBinaryContent(Stream stream, byte[] header, int length)
        {
            FormatInfo info = new FormatInfo();

            // Проверяем на Phyre Engine структуру
            if (length >= 72)
            {
                try
                {
                    uint magic = BitConverter.ToUInt32(header, 0);
                    if (magic == 0x50485952) // "PHYR"
                    {
                        // Дополнительная проверка структуры Phyre
                        int offset1 = BitConverter.ToInt32(header, 4);
                        int offset2 = BitConverter.ToInt32(header, 8);
                        
                        if (offset1 > 0 && offset1 < stream.Length && 
                            offset2 > 0 && offset2 < stream.Length)
                        {
                            info.Format = FileFormat.Phyre;
                            info.Description = "Phyre Engine Archive (structure verified)";
                            info.RecommendedExtension = ".phyre";
                            info.IsValid = true;
                            return info;
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибки при анализе
                }
            }

            // Проверяем на текстуру по энтропии данных
            if (IsLikelyTexture(header, length))
            {
                info.Format = FileFormat.CustomTexture;
                info.Description = "Likely texture data (by entropy analysis)";
                info.RecommendedExtension = ".tex";
                info.IsValid = true;
            }
            else if (IsLikelyFontData(header, length))
            {
                info.Format = FileFormat.CustomFont;
                info.Description = "Likely font data (by structure analysis)";
                info.RecommendedExtension = ".font";
                info.IsValid = true;
            }
            else
            {
                info.Format = FileFormat.Unknown;
                info.Description = "Unknown binary format";
                info.IsValid = false;
            }

            return info;
        }

        /// <summary>
        /// Проверяет, похожи ли данные на текстуру по энтропии
        /// </summary>
        private static bool IsLikelyTexture(byte[] data, int length)
        {
            if (length < 256) return false;

            // Подсчитываем уникальные байты
            bool[] bytePresent = new bool[256];
            int uniqueBytes = 0;

            for (int i = 0; i < Math.Min(length, 1024); i++)
            {
                if (!bytePresent[data[i]])
                {
                    bytePresent[data[i]] = true;
                    uniqueBytes++;
                }
            }

            // Текстуры обычно имеют высокую энтропию (много уникальных байтов)
            return uniqueBytes > 100;
        }

        /// <summary>
        /// Проверяет, похожи ли данные на font data по структуре
        /// </summary>
        private static bool IsLikelyFontData(byte[] data, int length)
        {
            if (length < 100) return false;

            // Ищем паттерны, характерные для font data
            int sequentialCodes = 0;
            int validCoordinates = 0;

            for (int i = 0; i < length - 16; i += 4)
            {
                try
                {
                    int code = BitConverter.ToInt32(data, i);
                    float x = BitConverter.ToSingle(data, i + 16);
                    float y = BitConverter.ToSingle(data, i + 20);

                    // Проверяем на валидные Unicode коды
                    if (code >= 32 && code <= 1114111) // Unicode range
                    {
                        sequentialCodes++;
                    }

                    // Проверяем на валидные координаты
                    if (x >= 0 && x < 4096 && y >= 0 && y < 4096)
                    {
                        validCoordinates++;
                    }
                }
                catch
                {
                    // Игнорируем ошибки
                }
            }

            // Если много валидных кодов и координат, вероятно это font data
            return sequentialCodes > 10 && validCoordinates > 10;
        }

        /// <summary>
        /// Получает расширение файла на основе распознанного формата
        /// </summary>
        public static string GetRecommendedExtension(FileFormat format)
        {
            switch (format)
            {
                case FileFormat.Phyre: return ".phyre";
                case FileFormat.PhyreFont: return ".font.phyre";
                case FileFormat.PhyreModel: return ".model.phyre";
                case FileFormat.PhyreTexture: return ".texture.phyre";
                case FileFormat.BMFont:
                case FileFormat.BMFontBinary: return ".fnt";
                case FileFormat.DDS: return ".dds";
                case FileFormat.GTF: return ".gtf";
                case FileFormat.PNG: return ".png";
                case FileFormat.JPEG: return ".jpg";
                case FileFormat.TGA: return ".tga";
                case FileFormat.CustomFont: return ".font";
                case FileFormat.CustomTexture: return ".tex";
                default: return ".bin";
            }
        }

        /// <summary>
        /// Создает имя файла с правильным расширением
        /// </summary>
        public static string CreateOutputFileName(string inputPath, FormatInfo formatInfo)
        {
            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = formatInfo.RecommendedExtension ?? ".bin";
            
            // Убираем существующие расширения и добавляем правильное
            return baseName + "_detected" + extension;
        }
    }
}
