using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace LZS_unpack
{
    /// <summary>
    /// Верификатор упакованных Phyre файлов
    /// Проверяет качество упаковки и сравнивает с оригиналом
    /// </summary>
    public class PhyrePackVerifier
    {
        /// <summary>
        /// Полная верификация упакованного файла
        /// </summary>
        public static void VerifyPackedFile(string packedPath, string originalPath = null)
        {
            try
            {
                Console.WriteLine("=== Phyre Pack Verification ===");
                Console.WriteLine($"Packed file: {packedPath}");
                if (originalPath != null)
                    Console.WriteLine($"Original file: {originalPath}");
                Console.WriteLine();

                // Основная верификация
                byte[] packedData = File.ReadAllBytes(packedPath);
                Console.WriteLine($"Packed size: {packedData.Length:N0} bytes ({packedData.Length / 1024.0 / 1024.0:F2} MB)");

                // Проверяем основные сигнатуры
                CheckSignatures(packedData);

                // Детальный анализ заголовка Phyre
                AnalyzePhyreHeader(packedData);

                // Проверяем структуру
                CheckStructure(packedData);

                // Поиск известных структур Phyre Engine
                SearchKnownStructures(packedData);

                // Валидация структур шрифта
                ValidateFontStructures(packedData);

                // Проверяем содержимое
                CheckContent(packedData);

                // Сравнение с оригиналом (если предоставлен)
                if (originalPath != null && File.Exists(originalPath))
                {
                    CompareWithOriginal(packedPath, originalPath);
                }

                Console.WriteLine();
                Console.WriteLine("=== Verification Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Verification failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет основные сигнатуры файла
        /// </summary>
        private static void CheckSignatures(byte[] data)
        {
            Console.WriteLine("--- Signature Check ---");

            // Проверяем Phyre заголовок
            if (data.Length >= 4)
            {
                string magic = Encoding.ASCII.GetString(data, 0, 4);
                Console.WriteLine($"Phyre magic: '{magic}' {(magic == "PHYR" ? "✅" : "❌")}");
                
                // Дополнительная проверка на little-endian
                if (magic != "PHYR")
                {
                    Console.WriteLine("  Note: Magic bytes may be byte-swapped (endianness issue)");
                }
            }

            // Ищем сигнатуры шрифта
            SearchForFontSignatures(data);

            // Ищем сигнатуры текстуры
            SearchForTextureSignatures(data);

            Console.WriteLine();
        }

        /// <summary>
        /// Ищет сигнатуры шрифтов в данных
        /// </summary>
        private static void SearchForFontSignatures(byte[] data)
        {
            int fontStructuresFound = 0;
            int validFontChars = 0;

            // Ищем структуры PBitmapFontCharInfo (45 байт) только в разумных областях
            // Обычно данные шрифтов находятся после заголовка, но не в самом конце
            int startSearch = 1000; // Начинаем поиск после заголовка
            int endSearch = Math.Min(data.Length - 45, data.Length - 1000000); // Оставляем место для текстур
            
            for (int i = startSearch; i < endSearch; i += 1) // Проверяем каждый байт
            {
                if (IsLikelyFontStructure(data, i))
                {
                    fontStructuresFound++;
                    
                    // Проверяем валидность символа
                    int charCode = BitConverter.ToInt32(data, i);
                    if (charCode >= 32 && charCode <= 126) // ASCII printable
                    {
                        validFontChars++;
                    }
                }
                
                // Ограничиваем количество проверок для производительности
                if (fontStructuresFound > 10000) break;
            }

            Console.WriteLine($"Font structures found: {fontStructuresFound}");
            Console.WriteLine($"Valid ASCII characters: {validFontChars}");

            // Ищем специфичные сигнатуры шрифтов
            SearchForSpecificFontSignatures(data);
        }

        /// <summary>
        /// Ищет специфичные сигнатуры шрифтов
        /// </summary>
        private static void SearchForSpecificFontSignatures(byte[] data)
        {
            int fntSignatures = 0;
            int bmfSignatures = 0;

            // Ищем "FNT " (4 байта)
            for (int i = 0; i < data.Length - 4; i++)
            {
                if (data[i] == 0x46 && data[i+1] == 0x4E && data[i+2] == 0x54 && data[i+3] == 0x20)
                {
                    fntSignatures++;
                }
            }

            // Ищем "BMF" (3 байта)
            for (int i = 0; i < data.Length - 3; i++)
            {
                if (data[i] == 0x42 && data[i+1] == 0x4D && data[i+2] == 0x46)
                {
                    bmfSignatures++;
                }
            }

            Console.WriteLine($"FNT signatures found: {fntSignatures}");
            Console.WriteLine($"BMF signatures found: {bmfSignatures}");
        }

        /// <summary>
        /// Ищет сигнатуры текстур в данных
        /// </summary>
        private static void SearchForTextureSignatures(byte[] data)
        {
            int gtfSignatures = 0;
            int ddsSignatures = 0;
            int pngSignatures = 0;

            // Ищем GTF сигнатуру (0x04 0x01 0x00 0x00)
            for (int i = 0; i < data.Length - 4; i++)
            {
                if (data[i] == 0x04 && data[i+1] == 0x01 && data[i+2] == 0x00 && data[i+3] == 0x00)
                {
                    gtfSignatures++;
                }
            }

            // Ищем DDS сигнатуру ("DDS ")
            for (int i = 0; i < data.Length - 4; i++)
            {
                if (data[i] == 0x44 && data[i+1] == 0x44 && data[i+2] == 0x53 && data[i+3] == 0x20)
                {
                    ddsSignatures++;
                }
            }

            // Ищем PNG сигнатуру (0x89 0x50 0x4E 0x47)
            for (int i = 0; i < data.Length - 4; i++)
            {
                if (data[i] == 0x89 && data[i+1] == 0x50 && data[i+2] == 0x4E && data[i+3] == 0x47)
                {
                    pngSignatures++;
                }
            }

            Console.WriteLine($"GTF signatures found: {gtfSignatures}");
            Console.WriteLine($"DDS signatures found: {ddsSignatures}");
            Console.WriteLine($"PNG signatures found: {pngSignatures}");
        }

        /// <summary>
        /// Проверяет, похожа ли структура на PBitmapFontCharInfo
        /// </summary>
        private static bool IsLikelyFontStructure(byte[] data, int offset)
        {
            if (offset + 45 > data.Length) return false;

            try
            {
                // Читаем char code (первые 4 байта)
                int charCode = BitConverter.ToInt32(data, offset);
                
                // Проверяем, что это разумный Unicode код (ASCII printable или основные Unicode)
                if (charCode < 32 || charCode > 0x10FFFF) return false;

                // Читаем координаты (байты 16-31) - более строгие проверки
                float x = BitConverter.ToSingle(data, offset + 16);
                float y = BitConverter.ToSingle(data, offset + 20);
                float w = BitConverter.ToSingle(data, offset + 24);
                float h = BitConverter.ToSingle(data, offset + 28);

                // Проверяем разумность координат (более строгие ограничения)
                if (x < 0 || x > 5000 || y < 0 || y > 5000) return false;
                if (w < 0 || w > 500 || h < 0 || h > 500) return false;
                
                // Проверяем, что координаты не являются NaN или Infinity
                if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(w) || float.IsNaN(h)) return false;
                if (float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(w) || float.IsInfinity(h)) return false;

                // Дополнительная проверка: char code должен быть в разумном диапазоне для шрифтов
                if (charCode >= 32 && charCode <= 126) // ASCII printable - очень вероятно
                    return true;
                if (charCode >= 0x80 && charCode <= 0xFF) // Extended ASCII
                    return true;
                if (charCode >= 0x100 && charCode <= 0xFFFF) // Basic Multilingual Plane
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Проверяет структуру файла
        /// </summary>
        private static void CheckStructure(byte[] data)
        {
            Console.WriteLine("--- Structure Check ---");

            if (data.Length < 72)
            {
                Console.WriteLine("❌ File too small for Phyre format");
                return;
            }

            // Читаем заголовок Phyre
            try
            {
                // Используем правильный порядок байтов
                int magic = BitConverter.ToInt32(data, 0);
                int offset1 = BitConverter.ToInt32(data, 4);
                int offset2 = BitConverter.ToInt32(data, 8);
                int count1 = BitConverter.ToInt32(data, 12);
                int size1 = BitConverter.ToInt32(data, 16);
                int size2 = BitConverter.ToInt32(data, 20);
                int count2 = BitConverter.ToInt32(data, 28);
                int count3 = BitConverter.ToInt32(data, 32);

                // Проверяем magic (0x50485952 = "PHYR" в little-endian)
                string magicStr = Encoding.ASCII.GetString(BitConverter.GetBytes(magic));
                bool magicValid = magic == 0x50485952 || magicStr == "PHYR";
                
                Console.WriteLine($"Magic: 0x{magic:X8} ({magicStr}) {(magicValid ? "✅" : "❌")}");
                
                // Если magic неверный, попробуем big-endian
                if (!magicValid)
                {
                    // Меняем порядок байтов
                    Array.Reverse(data, 0, 4);
                    magic = BitConverter.ToInt32(data, 0);
                    magicStr = Encoding.ASCII.GetString(BitConverter.GetBytes(magic));
                    magicValid = magic == 0x50485952 || magicStr == "PHYR";
                    
                    if (magicValid)
                    {
                        Console.WriteLine($"  Fixed with big-endian: 0x{magic:X8} ({magicStr}) ✅");
                        // Восстанавливаем оригинальные данные
                        Array.Reverse(data, 0, 4);
                    }
                    else
                    {
                        // Восстанавливаем оригинальные данные
                        Array.Reverse(data, 0, 4);
                    }
                }
                
                // Проверяем разумность значений (более мягкие ограничения)
                bool offsetsValid = offset1 >= 0 && offset2 >= offset1 && offset1 < data.Length && offset2 < data.Length;
                Console.WriteLine($"Header offsets: {offset1}, {offset2} {(offsetsValid ? "✅" : "❌")}");
                
                // Более мягкие проверки для object counts
                bool countsValid = count1 >= 0 && count2 >= 0 && count3 >= 0;
                Console.WriteLine($"Object counts: {count1}, {count2}, {count3} {(countsValid ? "✅" : "❌")}");

                // Проверяем размеры
                bool sizesValid = size1 > 0 && size2 > 0;
                Console.WriteLine($"Structure sizes: {size1}, {size2} {(sizesValid ? "✅" : "❌")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading structure: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Проверяет содержимое файла
        /// </summary>
        private static void CheckContent(byte[] data)
        {
            Console.WriteLine("--- Content Check ---");

            // Анализируем энтропию данных
            var entropy = CalculateEntropy(data);
            Console.WriteLine($"Data entropy: {entropy:F2} (0-8, higher = more random)");

            // Ищем большие блоки нулей (возможные пустые области)
            int zeroBlocks = CountZeroBlocks(data);
            Console.WriteLine($"Large zero blocks: {zeroBlocks}");

            // Проверяем наличие текстовых строк
            var textStrings = FindTextStrings(data);
            Console.WriteLine($"Text strings found: {textStrings}");

            Console.WriteLine();
        }

        /// <summary>
        /// Вычисляет энтропию данных
        /// </summary>
        private static double CalculateEntropy(byte[] data)
        {
            var frequencies = new int[256];
            foreach (byte b in data)
            {
                frequencies[b]++;
            }

            double entropy = 0;
            int length = data.Length;

            for (int i = 0; i < 256; i++)
            {
                if (frequencies[i] > 0)
                {
                    double probability = (double)frequencies[i] / length;
                    entropy -= probability * Math.Log(probability, 2);
                }
            }

            return entropy;
        }

        /// <summary>
        /// Подсчитывает большие блоки нулей
        /// </summary>
        private static int CountZeroBlocks(byte[] data)
        {
            int zeroBlocks = 0;
            int currentZeroCount = 0;

            foreach (byte b in data)
            {
                if (b == 0)
                {
                    currentZeroCount++;
                }
                else
                {
                    if (currentZeroCount >= 1024) // Блоки больше 1KB
                    {
                        zeroBlocks++;
                    }
                    currentZeroCount = 0;
                }
            }

            return zeroBlocks;
        }

        /// <summary>
        /// Ищет текстовые строки в данных
        /// </summary>
        private static int FindTextStrings(byte[] data)
        {
            int textStrings = 0;
            int currentLength = 0;

            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                
                // Проверяем ASCII printable символы
                if (b >= 32 && b <= 126)
                {
                    currentLength++;
                }
                else
                {
                    if (currentLength >= 4) // Строки длиной 4+ символов
                    {
                        textStrings++;
                    }
                    currentLength = 0;
                }
            }

            return textStrings;
        }

        /// <summary>
        /// Сравнивает с оригинальным файлом
        /// </summary>
        public static void CompareWithOriginal(string packedPath, string originalPath)
        {
            Console.WriteLine("--- Comparison with Original ---");

            try
            {
                byte[] packedData = File.ReadAllBytes(packedPath);
                byte[] originalData = File.ReadAllBytes(originalPath);

                Console.WriteLine($"Original size: {originalData.Length:N0} bytes ({originalData.Length / 1024.0 / 1024.0:F2} MB)");
                Console.WriteLine($"Packed size:   {packedData.Length:N0} bytes ({packedData.Length / 1024.0 / 1024.0:F2} MB)");
                
                long sizeDiff = packedData.Length - originalData.Length;
                Console.WriteLine($"Size difference: {sizeDiff:N0} bytes ({sizeDiff / 1024.0 / 1024.0:F2} MB)");
                
                if (sizeDiff == 0)
                {
                    Console.WriteLine("✅ Perfect size match!");
                }
                else if (Math.Abs(sizeDiff) < 1024)
                {
                    Console.WriteLine("✅ Excellent size match (within 1KB)");
                }
                else if (Math.Abs(sizeDiff) < 1024 * 1024)
                {
                    Console.WriteLine("⚠️  Good size match (within 1MB)");
                }
                else
                {
                    Console.WriteLine("❌ Significant size difference");
                }

                // Сравниваем форматы
                var packedInfo = FormatDetector.DetectFormat(packedPath);
                var originalInfo = FormatDetector.DetectFormat(originalPath);

                Console.WriteLine($"Original format: {originalInfo.Description}");
                Console.WriteLine($"Packed format:   {packedInfo.Description}");
                
                if (packedInfo.Format == originalInfo.Format)
                {
                    Console.WriteLine("✅ Format match");
                }
                else
                {
                    Console.WriteLine("❌ Format mismatch");
                }

                // Проверяем основные заголовки
                if (packedData.Length >= 72 && originalData.Length >= 72)
                {
                    bool headersMatch = true;
                    for (int i = 0; i < 72; i++)
                    {
                        if (packedData[i] != originalData[i])
                        {
                            headersMatch = false;
                            break;
                        }
                    }
                    Console.WriteLine($"Header match: {(headersMatch ? "✅" : "❌")}");
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Comparison failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Детальный анализ заголовка Phyre с диагностикой endianness
        /// </summary>
        private static void AnalyzePhyreHeader(byte[] data)
        {
            Console.WriteLine("--- Phyre Header Analysis ---");
            
            if (data.Length < 72)
            {
                Console.WriteLine("❌ File too small for Phyre header analysis");
                return;
            }

            try
            {
                // Анализируем magic bytes разными способами
                byte[] magicBytes = new byte[4];
                Array.Copy(data, 0, magicBytes, 0, 4);
                
                string magicAsString = Encoding.ASCII.GetString(magicBytes);
                uint magicAsUint = BitConverter.ToUInt32(magicBytes, 0);
                
                Console.WriteLine($"Magic as string: '{magicAsString}'");
                Console.WriteLine($"Magic as uint: 0x{magicAsUint:X8}");
                
                // Проверяем все возможные представления
                bool isPhyrDirect = magicAsString == "PHYR";
                bool isPhyrReversed = magicAsString == "RYHP";
                bool isPhyrUint = magicAsUint == 0x50485952; // "PHYR" as uint
                
                Console.WriteLine($"Is 'PHYR' (direct): {isPhyrDirect}");
                Console.WriteLine($"Is 'RYHP' (reversed): {isPhyrReversed}");
                Console.WriteLine($"Is 0x50485952 (as uint): {isPhyrUint}");
                
                if (isPhyrReversed && !isPhyrDirect)
                {
                    Console.WriteLine("🔍 DIAGNOSIS: Byte order is reversed (big-endian vs little-endian)");
                    Console.WriteLine("   This is common when reading/writing binary data with different endianness");
                }

                // Анализируем остальные поля заголовка
                AnalyzeHeaderFields(data, magicBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Header analysis failed: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        private static void AnalyzeHeaderFields(byte[] data, byte[] magicBytes)
        {
            // Читаем поля с разными предположениями о endianness
            int offset1_le = BitConverter.ToInt32(data, 4);
            int offset2_le = BitConverter.ToInt32(data, 8);
            
            // Пробуем big-endian чтение
            byte[] offset1_be_bytes = new byte[4];
            Array.Copy(data, 4, offset1_be_bytes, 0, 4);
            Array.Reverse(offset1_be_bytes);
            int offset1_be = BitConverter.ToInt32(offset1_be_bytes, 0);
            
            byte[] offset2_be_bytes = new byte[4];
            Array.Copy(data, 8, offset2_be_bytes, 0, 4);
            Array.Reverse(offset2_be_bytes);
            int offset2_be = BitConverter.ToInt32(offset2_be_bytes, 0);
            
            Console.WriteLine($"Offset1 - LE: 0x{offset1_le:X8} ({offset1_le}), BE: 0x{offset1_be:X8} ({offset1_be})");
            Console.WriteLine($"Offset2 - LE: 0x{offset2_le:X8} ({offset2_le}), BE: 0x{offset2_be:X8} ({offset2_be})");
            
            // Определяем какой вариант более правдоподобен
            bool leReasonable = IsReasonableOffset(offset1_le) && IsReasonableOffset(offset2_le);
            bool beReasonable = IsReasonableOffset(offset1_be) && IsReasonableOffset(offset2_be);
            
            Console.WriteLine($"Little-endian reasonable: {leReasonable}");
            Console.WriteLine($"Big-endian reasonable: {beReasonable}");
            
            if (leReasonable && !beReasonable)
                Console.WriteLine("✅ LIKELY: Little-endian format");
            else if (beReasonable && !leReasonable)
                Console.WriteLine("✅ LIKELY: Big-endian format");
            else if (leReasonable && beReasonable)
                Console.WriteLine("⚠️  AMBIGUOUS: Both endianness seem reasonable");
            else
                Console.WriteLine("❌ UNLIKELY: Neither endianness produces reasonable offsets");
        }

        private static bool IsReasonableOffset(int offset)
        {
            return offset >= 0 && offset < 10 * 1024 * 1024; // До 10MB
        }

        /// <summary>
        /// Поиск известных структур Phyre Engine в файле
        /// </summary>
        private static void SearchKnownStructures(byte[] data)
        {
            Console.WriteLine("--- Known Phyre Structures ---");
            
            // Ищем PTexture2D структуры
            FindTextureStructures(data);
            
            // Ищем PBitmapFont структуры  
            FindFontStructures(data);
            
            Console.WriteLine();
        }

        private static void FindTextureStructures(byte[] data)
        {
            int textureStructs = 0;
            
            // Ищем PTexture2D сигнатуры или известные паттерны
            for (int i = 0; i < data.Length - 100; i++)
            {
                // Проверяем возможные сигнатуры текстур
                if (IsLikelyTextureDescriptor(data, i))
                {
                    textureStructs++;
                    
                    if (textureStructs == 1) // Выводим детали первой найденной
                    {
                        Console.WriteLine($"First texture struct at: 0x{i:X}");
                        AnalyzeTextureDescriptor(data, i);
                    }
                }
            }
            
            Console.WriteLine($"PTexture2D structures found: {textureStructs}");
        }

        private static bool IsLikelyTextureDescriptor(byte[] data, int offset)
        {
            // Эвристики для определения PTexture2D структур
            try
            {
                // Проверяем размеры текстуры (должны быть степенью двойки)
                int width = BitConverter.ToInt32(data, offset + 8);
                int height = BitConverter.ToInt32(data, offset + 12);
                
                bool powerOfTwo = IsPowerOfTwo(width) && IsPowerOfTwo(height);
                bool reasonableSize = width > 0 && width <= 8192 && height > 0 && height <= 8192;
                
                return powerOfTwo && reasonableSize;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        private static void AnalyzeTextureDescriptor(byte[] data, int offset)
        {
            try
            {
                int width = BitConverter.ToInt32(data, offset + 8);
                int height = BitConverter.ToInt32(data, offset + 12);
                int format = BitConverter.ToInt32(data, offset + 16);
                
                Console.WriteLine($"  Texture dimensions: {width}x{height}");
                Console.WriteLine($"  Format: 0x{format:X8}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error analyzing texture: {ex.Message}");
            }
        }

        private static void FindFontStructures(byte[] data)
        {
            int fontStructs = 0;
            
            // Ищем PBitmapFont структуры
            for (int i = 1000; i < data.Length - 100; i++)
            {
                if (IsLikelyFontDescriptor(data, i))
                {
                    fontStructs++;
                    
                    if (fontStructs == 1) // Выводим детали первой найденной
                    {
                        Console.WriteLine($"First font struct at: 0x{i:X}");
                    }
                }
            }
            
            Console.WriteLine($"PBitmapFont structures found: {fontStructs}");
        }

        private static bool IsLikelyFontDescriptor(byte[] data, int offset)
        {
            try
            {
                // Проверяем возможные поля PBitmapFont
                int charCount = BitConverter.ToInt32(data, offset + 4);
                int textureRef = BitConverter.ToInt32(data, offset + 8);
                
                // Эвристики для определения шрифта
                bool reasonableCharCount = charCount > 0 && charCount < 100000;
                bool reasonableTextureRef = textureRef >= 0 && textureRef < 1000;
                
                return reasonableCharCount && reasonableTextureRef;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Валидация структур шрифта с более строгими проверками
        /// </summary>
        private static void ValidateFontStructures(byte[] data)
        {
            Console.WriteLine("--- Font Structure Validation ---");
            
            int validStructures = 0;
            int totalStructures = 0;
            var charCodes = new HashSet<int>();
            
            // Ищем начало структур шрифта более умным способом
            int[] possibleOffsets = FindPossibleFontOffsets(data);
            
            Console.WriteLine($"Found {possibleOffsets.Length} possible font data regions");
            
            foreach (int startOffset in possibleOffsets)
            {
                Console.WriteLine($"Checking font data at offset 0x{startOffset:X}...");
                
                int regionValidStructures = 0;
                int regionTotalStructures = 0;
                
                for (int i = startOffset; i < data.Length - 45 && regionTotalStructures < 1000; i += 45)
                {
                    regionTotalStructures++;
                    
                    if (IsValidFontStructureStrict(data, i))
                    {
                        regionValidStructures++;
                        int charCode = BitConverter.ToInt32(data, i);
                        charCodes.Add(charCode);
                    }
                    else
                    {
                        // Если нашли несколько валидных структур, но потом идут невалидные,
                        // это может быть конец области шрифтов
                        if (regionValidStructures > 10)
                            break;
                    }
                }
                
                Console.WriteLine($"  Region 0x{startOffset:X}: {regionValidStructures}/{regionTotalStructures} valid structures");
                
                validStructures += regionValidStructures;
                totalStructures += regionTotalStructures;
                
                // Если нашли много валидных структур, это скорее всего правильная область
                if (regionValidStructures > 100)
                {
                    Console.WriteLine($"  ✅ Found main font data region at 0x{startOffset:X}");
                    break;
                }
            }
            
            Console.WriteLine($"Total valid font structures: {validStructures}/{totalStructures}");
            Console.WriteLine($"Unique character codes: {charCodes.Count}");
            
            if (charCodes.Count > 0)
            {
                Console.WriteLine($"Character range: {charCodes.Min()} to {charCodes.Max()}");
                
                // Проверяем покрытие символов
                CheckCharacterCoverage(charCodes);
            }
            else
            {
                Console.WriteLine("⚠️  No valid font structures found - checking with relaxed criteria...");
                ValidateFontStructuresRelaxed(data);
            }
            
            Console.WriteLine();
        }

        private static bool IsValidFontStructureStrict(byte[] data, int offset)
        {
            try
            {
                int charCode = BitConverter.ToInt32(data, offset);
                
                // Более строгие проверки координат
                float x = BitConverter.ToSingle(data, offset + 16);
                float y = BitConverter.ToSingle(data, offset + 20);
                float width = BitConverter.ToSingle(data, offset + 24);
                float height = BitConverter.ToSingle(data, offset + 28);
                
                // Проверяем что координаты в пределах текстуры 2048x2048
                bool coordsValid = x >= 0 && x < 2048 && y >= 0 && y < 2048;
                bool sizeValid = width >= 0 && width <= 256 && height >= 0 && height <= 256;
                bool charValid = charCode >= 0 && charCode <= 0x10FFFF;
                
                // Проверяем что это не мусорные данные
                bool notZero = !(x == 0 && y == 0 && width == 0 && height == 0);
                
                return coordsValid && sizeValid && charValid && notZero;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Поиск возможных offset'ов для данных шрифта
        /// </summary>
        private static int[] FindPossibleFontOffsets(byte[] data)
        {
            var offsets = new List<int>();
            
            // Ищем области с последовательными ASCII кодами (32-126)
            for (int i = 1000; i < data.Length - 45 * 10; i += 4)
            {
                if (HasConsecutiveASCIICharacters(data, i, 10))
                {
                    offsets.Add(i);
                }
            }
            
            // Также проверяем известные offset'ы из предыдущих анализов
            int[] knownOffsets = { 0x4256, 0x1000, 0x2000, 0x3000, 0x4000 };
            foreach (int offset in knownOffsets)
            {
                if (offset < data.Length - 45 && !offsets.Contains(offset))
                {
                    offsets.Add(offset);
                }
            }
            
            return offsets.ToArray();
        }

        /// <summary>
        /// Проверяет есть ли последовательные ASCII символы начиная с offset
        /// </summary>
        private static bool HasConsecutiveASCIICharacters(byte[] data, int offset, int minCount)
        {
            try
            {
                int consecutiveCount = 0;
                
                for (int i = offset; i < data.Length - 45 && consecutiveCount < minCount; i += 45)
                {
                    int charCode = BitConverter.ToInt32(data, i);
                    
                    if (charCode >= 32 && charCode <= 126)
                    {
                        consecutiveCount++;
                    }
                    else
                    {
                        break;
                    }
                }
                
                return consecutiveCount >= minCount;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Валидация структур шрифта с расслабленными критериями
        /// </summary>
        private static void ValidateFontStructuresRelaxed(byte[] data)
        {
            Console.WriteLine("--- Relaxed Font Structure Validation ---");
            
            int validStructures = 0;
            var charCodes = new HashSet<int>();
            
            // Ищем по всему файлу с расслабленными критериями
            for (int i = 1000; i < data.Length - 45; i += 4)
            {
                if (IsValidFontStructureRelaxed(data, i))
                {
                    validStructures++;
                    int charCode = BitConverter.ToInt32(data, i);
                    charCodes.Add(charCode);
                    
                    if (validStructures % 1000 == 0)
                    {
                        Console.WriteLine($"  Found {validStructures} structures so far...");
                    }
                    
                    if (validStructures > 10000) break; // Ограничение для производительности
                }
            }
            
            Console.WriteLine($"Relaxed validation found: {validStructures} font structures");
            Console.WriteLine($"Unique character codes: {charCodes.Count}");
            
            if (charCodes.Count > 0)
            {
                Console.WriteLine($"Character range: {charCodes.Min()} to {charCodes.Max()}");
                
                // Показываем несколько примеров найденных символов
                var sampleChars = charCodes.OrderBy(c => c).Take(20);
                Console.WriteLine($"Sample characters: {string.Join(", ", sampleChars.Select(c => $"{(char)c}({c})"))}");
            }
        }

        /// <summary>
        /// Расслабленная валидация структуры шрифта
        /// </summary>
        private static bool IsValidFontStructureRelaxed(byte[] data, int offset)
        {
            try
            {
                int charCode = BitConverter.ToInt32(data, offset);
                
                // Расслабленные проверки координат
                float x = BitConverter.ToSingle(data, offset + 16);
                float y = BitConverter.ToSingle(data, offset + 20);
                float width = BitConverter.ToSingle(data, offset + 24);
                float height = BitConverter.ToSingle(data, offset + 28);
                
                // Более мягкие проверки
                bool coordsValid = x >= 0 && x < 10000 && y >= 0 && y < 10000;
                bool sizeValid = width >= 0 && width <= 1000 && height >= 0 && height <= 1000;
                bool charValid = charCode >= 0 && charCode <= 0x10FFFF;
                
                // Проверяем что это не мусорные данные (не все нули)
                bool notAllZero = !(x == 0 && y == 0 && width == 0 && height == 0);
                
                // Проверяем что координаты не NaN или Infinity
                bool notNaN = !float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(width) && !float.IsNaN(height);
                bool notInfinity = !float.IsInfinity(x) && !float.IsInfinity(y) && !float.IsInfinity(width) && !float.IsInfinity(height);
                
                return coordsValid && sizeValid && charValid && notAllZero && notNaN && notInfinity;
            }
            catch
            {
                return false;
            }
        }

        private static void CheckCharacterCoverage(HashSet<int> charCodes)
        {
            int asciiCount = charCodes.Count(code => code >= 32 && code <= 126);
            int extendedAsciiCount = charCodes.Count(code => code >= 128 && code <= 255);
            int unicodeCount = charCodes.Count(code => code >= 256 && code <= 65535);
            
            Console.WriteLine($"ASCII coverage: {asciiCount} characters");
            Console.WriteLine($"Extended ASCII coverage: {extendedAsciiCount} characters");
            Console.WriteLine($"Unicode coverage: {unicodeCount} characters");
            
            // Проверяем основные диапазоны
            bool hasBasicLatin = charCodes.Any(code => code >= 32 && code <= 126);
            bool hasLatinExtended = charCodes.Any(code => code >= 128 && code <= 255);
            bool hasCyrillic = charCodes.Any(code => code >= 0x0400 && code <= 0x04FF);
            bool hasGreek = charCodes.Any(code => code >= 0x0370 && code <= 0x03FF);
            
            Console.WriteLine($"Basic Latin: {(hasBasicLatin ? "✅" : "❌")}");
            Console.WriteLine($"Latin Extended: {(hasLatinExtended ? "✅" : "❌")}");
            Console.WriteLine($"Cyrillic: {(hasCyrillic ? "✅" : "❌")}");
            Console.WriteLine($"Greek: {(hasGreek ? "✅" : "❌")}");
        }
    }
}
