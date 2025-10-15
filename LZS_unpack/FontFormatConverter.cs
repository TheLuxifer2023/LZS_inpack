using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace LZS_unpack
{
    /// <summary>
    /// Структура данных символа шрифта
    /// </summary>
    public class BitmapFontChar
    {
        public int CharCode;
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float OffsetX;
        public float OffsetY;
        public float AdvanceX;
        public int Page;
    }

    /// <summary>
    /// Конвертер между Raw Binary Font Format и BMFont (.fnt) форматами
    /// Обеспечивает сохранение оригинальной структуры Phyre файлов
    /// </summary>
    public class FontFormatConverter
    {
        /// <summary>
        /// Конвертирует Raw Binary Font Format в BMFont (.fnt) текстовый формат
        /// </summary>
        public static void ConvertRawToBMFont(List<BitmapFontChar> chars, string outputPath, string textureFileName)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(outputPath, false, Encoding.UTF8))
                {
                    NumberFormatInfo nfi = new NumberFormatInfo();
                    nfi.NumberDecimalSeparator = ".";

                    // Заголовок BMFont
                    sw.WriteLine("info face=\"PhyreFont\" size=32 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0");
                    sw.WriteLine("common lineHeight=32 base=26 scaleW=2048 scaleH=2048 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4");
                    sw.WriteLine($"page id=0 file=\"{textureFileName}\"");
                    sw.WriteLine($"chars count={chars.Count}");

                    // Записываем данные символов
                    foreach (var ch in chars)
                    {
                        string charDisplay = GetCharDisplay(ch.CharCode);
                        sw.WriteLine(string.Format(nfi,
                            "char id={0} x={1} y={2} width={3} height={4} xoffset={5} yoffset={6} xadvance={7} page={8} chnl=15",
                            ch.CharCode, (int)ch.X, (int)ch.Y, (int)ch.Width, (int)ch.Height,
                            (int)ch.OffsetX, (int)ch.OffsetY, (int)ch.AdvanceX, ch.Page
                        ));
                    }
                }

                Console.WriteLine($"Converted raw font data to BMFont: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting raw to BMFont: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Конвертирует BMFont (.fnt) текстовый формат в Raw Binary Font Format
        /// </summary>
        public static List<BitmapFontChar> ConvertBMFontToRaw(string fntPath)
        {
            List<BitmapFontChar> chars = new List<BitmapFontChar>();

            try
            {
                using (StreamReader sr = new StreamReader(fntPath, Encoding.UTF8))
                {
                    string line;
                    bool inCharsSection = false;

                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.StartsWith("chars count="))
                        {
                            inCharsSection = true;
                            continue;
                        }

                        if (line.StartsWith("char id=") && inCharsSection)
                        {
                            var charData = ParseCharLine(line);
                            if (charData != null)
                            {
                                chars.Add(charData);
                            }
                        }
                    }
                }

                Console.WriteLine($"Converted BMFont to raw font data: {chars.Count} characters");
                return chars;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting BMFont to raw: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Парсит строку char из BMFont файла
        /// </summary>
        private static BitmapFontChar ParseCharLine(string line)
        {
            try
            {
                // Формат: char id=32 x=507 y=0 width=0 height=0 xoffset=0 yoffset=14 xadvance=0 page=0 chnl=15
                var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length < 9) return null;

                int charCode = 0;
                float x = 0, y = 0, width = 0, height = 0;
                float offsetX = 0, offsetY = 0, advanceX = 0;
                int page = 0;

                foreach (var part in parts)
                {
                    if (part.StartsWith("id="))
                        int.TryParse(part.Substring(3), out charCode);
                    else if (part.StartsWith("x="))
                        float.TryParse(part.Substring(2), NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                    else if (part.StartsWith("y="))
                        float.TryParse(part.Substring(2), NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                    else if (part.StartsWith("width="))
                        float.TryParse(part.Substring(6), NumberStyles.Float, CultureInfo.InvariantCulture, out width);
                    else if (part.StartsWith("height="))
                        float.TryParse(part.Substring(7), NumberStyles.Float, CultureInfo.InvariantCulture, out height);
                    else if (part.StartsWith("xoffset="))
                        float.TryParse(part.Substring(8), NumberStyles.Float, CultureInfo.InvariantCulture, out offsetX);
                    else if (part.StartsWith("yoffset="))
                        float.TryParse(part.Substring(8), NumberStyles.Float, CultureInfo.InvariantCulture, out offsetY);
                    else if (part.StartsWith("xadvance="))
                        float.TryParse(part.Substring(9), NumberStyles.Float, CultureInfo.InvariantCulture, out advanceX);
                    else if (part.StartsWith("page="))
                        int.TryParse(part.Substring(5), out page);
                }

                return new BitmapFontChar
                {
                    CharCode = charCode,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                    OffsetX = offsetX,
                    OffsetY = offsetY,
                    AdvanceX = advanceX,
                    Page = page
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing char line: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Получает отображаемый символ для Unicode кода
        /// </summary>
        private static string GetCharDisplay(int charCode)
        {
            try
            {
                if (charCode >= 32 && charCode <= 126)
                {
                    // ASCII символы
                    return ((char)charCode).ToString();
                }
                else if (charCode > 126 && charCode <= 0x10FFFF)
                {
                    // Unicode символы
                    return char.ConvertFromUtf32(charCode);
                }
                else
                {
                    // Специальные символы или недопустимые коды
                    return "?";
                }
            }
            catch
            {
                return "?";
            }
        }

        /// <summary>
        /// Определяет, является ли файл BMFont форматом
        /// </summary>
        public static bool IsBMFontFile(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filePath, Encoding.UTF8))
                {
                    string firstLine = sr.ReadLine();
                    return firstLine != null && firstLine.StartsWith("info face=");
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Определяет, является ли файл Raw Binary Font Format
        /// </summary>
        public static bool IsRawBinaryFontFile(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Проверяем первые несколько байт на наличие ASCII кодов символов
                    byte[] buffer = new byte[Math.Min(1024, (int)fs.Length)];
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);

                    int asciiCount = 0;
                    for (int i = 0; i < bytesRead - 4; i += 4)
                    {
                        int charCode = BitConverter.ToInt32(buffer, i);
                        if (charCode >= 32 && charCode <= 126)
                        {
                            asciiCount++;
                        }
                    }

                    // Если много ASCII кодов, вероятно это raw binary font
                    return asciiCount > 10;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
