namespace SupplyNetworkManagement.Models
{
    public class ProductRequest
    {
        public string ProductName { get; set; }
        public string CategoryL1 { get; set; }
        public string CategoryL2 { get; set; }
        public string CategoryL3 { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class UpdateProductRequest
    {
        public string? ProductName { get; set; }
        public string? CategoryL1 { get; set; }
        public string? CategoryL2 { get; set; }
        public string? CategoryL3 { get; set; }
        public string? Unit { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
    }
}