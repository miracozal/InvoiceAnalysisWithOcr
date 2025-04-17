using invoiceOCR.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace invoiceOCR.Services
{
    public class OcrAIProcess
    {
        private static readonly string apiKey = "AIzaSyC1L8zPLx_uEK9Q4taWWrLQ0CnnFDbXOJw";
        private static readonly string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

        public  async Task<InvoiceModel> ExtractWithAI(string imagePath)
        {
            var base64Image = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath));

            var payload = new
            {
                contents = new[]
                {
            new
            {
                parts = new object[]
                {
                    new
                    {
                        text = @"Aşağıda bir fatura görseli bulunmaktadır. Bu fatura görselinden aşağıdaki alanları JSON formatında çıkarmanı istiyorum:
{
  ""SellerTitle"": ""Satıcının unvanı"",
  ""SellerAddress"": ""Satıcının adresi"",
  ""SellerPhone"": ""Satıcının telefon numarası"",
  ""SellerVKN"": ""Satıcının vergi kimlik numarası"",
  ""SellerMersisNo"": ""Satıcının mersis numarası"",
  ""StoreInfo"": ""Mağaza adı"",
  ""BuyerName"": ""Alıcının adı soyadı"",
  ""BuyerAddress"": ""Alıcının adresi"",
  ""BuyerCustomerNo"": ""Alıcının müşteri numarası"",
  ""BuyerBranch"": ""Alıcının bağlı şubesi"",
  ""InvoiceNumber"": ""Fatura numarası"",
  ""InvoiceDate"": ""Fatura tarihi (GG/AA/YYYY şeklinde)"",
  ""GrandTotal"": ""Toplam tutar (nokta ile ondalık ayrılmış)""
}"
                    },
                    new
                    {
                        inlineData = new
                        {
                            mimeType = "image/png",
                            data = base64Image
                        }
                    }
                }
            }
        }
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);
            var responseText = await response.Content.ReadAsStringAsync();

            var root = JsonConvert.DeserializeObject<GeminiResponse>(responseText);
            if (root?.Candidates == null || !root.Candidates.Any()) return null;

            var rawText = root.Candidates[0].Content?.Parts?[0]?.Text;
            var cleanJson = rawText
    .Replace("```json", "")
    .Replace("```", "")
    .Trim();

            return JsonConvert.DeserializeObject<InvoiceModel>(cleanJson ?? "");
        }

        private class GeminiResponse
        {
            public List<GeminiCandidate> Candidates { get; set; }
        }

        private class GeminiCandidate
        {
            public GeminiContent Content { get; set; }
        }

        private class GeminiContent
        {
            public List<GeminiPart> Parts { get; set; }
        }

        private class GeminiPart
        {
            public string Text { get; set; }
        }
    }
}
