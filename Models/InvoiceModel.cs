
using System.Globalization;

namespace invoiceOCR.Models {
public class InvoiceModel
{
    // Firma Bilgileri
    public string SellerTitle { get; set; }
    public string SellerAddress { get; set; }
    public string SellerPhone { get; set; }
    public string SellerVKN { get; set; }
    public string SellerMersisNo { get; set; }
    public string StoreInfo { get; set; }

    // Alıcı Bilgileri
    public string BuyerName { get; set; }
    public string BuyerAddress { get; set; }
    public string BuyerVKN { get; set; }
    public string BuyerTCKN { get; set; }
    public string BuyerCustomerNo { get; set; }
    public string BuyerBranch { get; set; }

    // Fatura Bilgileri
    public string InvoiceNumber { get; set; }
    public string InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string DispatchNoteNo { get; set; }
    public DateTime DispatchDate { get; set; }
    public string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime EditDate { get; set; }
    public string SystemDocNo { get; set; }

    // Satır Ürünleri
    public List<invoiceOCR.Models.InvoiceItem> Items { get; set; } = new List<invoiceOCR.Models.InvoiceItem>();

    // Toplamlar
    public decimal TotalWithoutTax { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Kdv10 { get; set; }
    public decimal Kdv20 { get; set; }
    public string GrandTotal { get; set; }
    public decimal GrandTotalDecimal => decimal.TryParse(GrandTotal, NumberStyles.Any, new CultureInfo("tr-TR"), out var value) ? value : 0;

    }
}