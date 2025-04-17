namespace invoiceOCR.Models
{
    public class InvoiceItem
    {
        public int RowNumber { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string KdvRate { get; set; }
        public string Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}