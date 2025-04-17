using Tesseract;
using invoiceOCR.Models;
using System.Text.RegularExpressions;
using System.Drawing;

namespace invoiceOCR.Services
{
    public class OCRProcess
    {
        public static async Task<InvoiceModel> ExtractInvoiceData(string imagePath)
        {
            var resultModel = new InvoiceModel();

            await Task.Run(() =>
            {

                using var engine = new TesseractEngine(Path.Combine(Directory.GetCurrentDirectory(), "tessdata-main"), "tur", EngineMode.Default);
                using var original = new Bitmap(imagePath);

                int width = original.Width / 2;
                int height = original.Height;

                var leftRect = new Rectangle(0, 0, width, height);
                var rightRect = new Rectangle(width, 0, width, height);

                var leftBmp = original.Clone(leftRect, original.PixelFormat);
                var rightBmp = original.Clone(rightRect, original.PixelFormat);

                using var leftPix = BitmapToPix(leftBmp);
                using var rightPix = BitmapToPix(rightBmp);
                string leftText, rightText;

                using (var leftPage = engine.Process(leftPix))
                {
                    leftText = leftPage.GetText();
                }

                using (var rightPage = engine.Process(rightPix))
                {
                    rightText = rightPage.GetText();
                }

                var text = leftText + "\n" + rightText;

                var lines = text.Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                var documentType = DetectDocumentType(text);
                if (documentType.Contains("Fatura"))
                {

                    resultModel.SellerTitle = lines.FirstOrDefault();

                    resultModel.SellerAddress = string.Join(" ", lines.Skip(1).TakeWhile(line =>
                        !Regex.IsMatch(line, @"\d{3} \d{3} \d{2} \d{2}") &&
                        !line.Contains("Vergi", StringComparison.OrdinalIgnoreCase) &&
                        !line.Contains("Mersis", StringComparison.OrdinalIgnoreCase)
                    ));

                    resultModel.SellerPhone = (lines.Select(line => Regex.Match(line, @"0\d{3} \d{3} \d{2} \d{2}"))
                          .FirstOrDefault(match => match.Success).Value);

                    resultModel.SellerVKN = GetLineValue(lines.FirstOrDefault(l => l.Contains("Vergi Kimlik", StringComparison.OrdinalIgnoreCase)));
                    resultModel.SellerMersisNo = GetLineValue(lines.FirstOrDefault(l => l.Contains("Mersis", StringComparison.OrdinalIgnoreCase)));
                    resultModel.StoreInfo = lines
                        .FirstOrDefault(l => l.Contains("Mağaza Adı", StringComparison.OrdinalIgnoreCase))
                        ?.Replace("Mağaza Adı:", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();
                    resultModel.BuyerCustomerNo = GetLineValue(lines.FirstOrDefault(l => l.Contains("Müşteri No", StringComparison.OrdinalIgnoreCase)));
                    resultModel.BuyerBranch = GetLineValue(lines.FirstOrDefault(l => l.Contains("Semt Adı", StringComparison.OrdinalIgnoreCase)));

                    resultModel.InvoiceNumber = FindCustomCode(text);

                    var sayinIndex = lines.FindIndex(l => l.StartsWith("SAYIN", StringComparison.OrdinalIgnoreCase));
                    if (sayinIndex >= 0 && sayinIndex + 1 < lines.Count)
                    {
                        resultModel.BuyerName = lines[sayinIndex + 1];
                    }

                    if (sayinIndex >= 0)
                    {
                        resultModel.BuyerAddress = string.Join(" ", lines.Skip(sayinIndex + 2).TakeWhile(line =>
                            !line.Contains("Vergi", StringComparison.OrdinalIgnoreCase) &&
                            !line.Contains("Müşteri", StringComparison.OrdinalIgnoreCase)
                        ));
                    }
                    resultModel.GrandTotal = ConvertTurkishWordsToDecimal(lines.FirstOrDefault(l =>
        l.ToUpperInvariant().Contains("YALNIZ") ||
        l.ToUpperInvariant().Contains("TÜRK LİRASI"))).ToString();
                }
            });

            return resultModel;
        }
        private static Pix BitmapToPix(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            return Pix.LoadFromMemory(ms.ToArray());
        }
        private static (string liraPart, string kurusPart) SplitLiraAndKurus(string fullText)
        {
            fullText = fullText.ToUpperInvariant()
                               .Replace("YALNIZ", "")
                               .Replace("YALNıZ", "")
                               .Trim();

            var lira = "";
            var kurus = "";

            if (fullText.Contains("TÜRK LİRASI"))
            {
                var parts = fullText.Split("TÜRK LİRASI", StringSplitOptions.RemoveEmptyEntries);
                lira = parts.ElementAtOrDefault(0)?.Trim() ?? "";
                kurus = parts.ElementAtOrDefault(1)?.Replace("KURUŞ", "").Trim() ?? "";
            }
            else
            {
                lira = fullText;
            }

            return (lira, kurus);
        }
        private static string FindCustomCode(string text)
        {
            var pattern = @"^[A-Za-z0-9]{3}\d{9}\d{4}$";
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (Regex.IsMatch(trimmed, pattern))
                    return trimmed;
            }

            return null;
        }
        private static string GetLineValue(string fullLine)
        {
            if (string.IsNullOrWhiteSpace(fullLine)) return "";
            var parts = fullLine.Split(new[] { ':', '|', '—', '-' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts.Last().Trim() : fullLine.Trim();
        }

        static readonly Dictionary<string, int> numberWords = new Dictionary<string, int>
{
    { "SIFIR", 0 }, { "BİR", 1 }, { "İKİ", 2 }, { "ÜÇ", 3 }, { "DÖRT", 4 },
    { "BEŞ", 5 }, { "ALTI", 6 }, { "YEDİ", 7 }, { "SEKİZ", 8 }, { "DOKUZ", 9 },
    { "ON", 10 }, { "YİRMİ", 20 }, { "OTUZ", 30 }, { "KIRK", 40 }, { "ELLİ", 50 },
    { "ALTMIŞ", 60 }, { "YETMİŞ", 70 }, { "SEKSEN", 80 }, { "DOKSAN", 90 },
    { "YÜZ", 100 }, { "BİN", 1000 }, { "MİLYON", 1000000 }
};
        private static decimal ConvertTurkishWordsToDecimal(string fullText)
        {
            var (liraText, kurusText) = SplitLiraAndKurus(fullText);

            int lira = ConvertTurkishWordsToInt(liraText);
            int kurus = ConvertTurkishWordsToInt(kurusText);

            return lira + (kurus / 100m);
        }

        private static List<string> ExtractAllNumberWords(string fullText)
        {
            var cleanText = fullText.ToUpperInvariant()
                .Replace("YALNIZ", "")
                .Replace("YALNıZ", "")
                .Replace("TÜRK LİRASI", "")
                .Replace("KURUŞ", "")
                .Replace("TL", "")
                .Replace("₺", "")
                .Trim();

            var logicalWords = Regex.Matches(cleanText, @"[A-ZÇĞİÖŞÜ]+")
                                    .Select(m => m.Value)
                                    .ToList();

            var result = new List<string>();

            foreach (var word in logicalWords)
            {
                var parts = SplitTurkishNumberWord(word);
                result.AddRange(parts);
            }

            return result;
        }

        private static List<string> SplitTurkishNumberWord(string input)
        {
            var result = new List<string>();
            var keys = numberWords.Keys.OrderByDescending(k => k.Length); 
            input = input.ToUpperInvariant();

            while (!string.IsNullOrEmpty(input))
            {
                var match = keys.FirstOrDefault(k => input.StartsWith(k));
                if (match != null)
                {
                    result.Add(match);
                    input = input.Substring(match.Length);
                }
                else
                {
                    break;
                }
            }

            return result;
        }
        private static int ConvertTurkishWordsToInt(string text)
        {
            var words = ExtractAllNumberWords(text);
            int total = 0, current = 0;

            foreach (var word in words)
            {
                if (!numberWords.ContainsKey(word)) continue;

                int val = numberWords[word];

                if (val == 100)
                    current = current == 0 ? 100 : current * 100;
                else if (val == 1000 || val == 1000000)
                {
                    current = current == 0 ? val : current * val;
                    total += current;
                    current = 0;
                }
                else
                {
                    current += val;
                }
            }

            total += current;
            return total;
        }
        private static string DetectDocumentType(string ocrText)
        {
            ocrText = ocrText.ToUpperInvariant();

            if (ocrText.Contains("FATURA") || ocrText.Contains("E-ARŞİV") || ocrText.Contains("ETTN"))
                return "Fatura";

            if (ocrText.Contains("ADİSYON") || ocrText.Contains("GARSON") || ocrText.Contains("MASA"))
                return "Adisyon";

            if (ocrText.Contains("SATIŞ FİŞİ") || ocrText.Contains("YAZAR KASA") || ocrText.Contains("FİŞ NO"))
                return "Fiş";

            if (ocrText.Contains("MAKBUZ") || ocrText.Contains("SERBEST MESLEK") || ocrText.Contains("TAHSİLAT"))
                return "Makbuz";

            if (ocrText.Contains("İRSALİYE") || ocrText.Contains("SEVK") || ocrText.Contains("TESLİM"))
                return "İrsaliye";

            return "Bilinmeyen";
        }
    }
}
