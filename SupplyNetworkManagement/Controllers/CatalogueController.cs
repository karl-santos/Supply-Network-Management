using Microsoft.AspNetCore.Mvc;
using SupplyNetworkManagement.Data;
using SupplyNetworkManagement.Models;
using System.Text;
using System.Text.Json;

namespace SupplyNetworkManagement.Controllers
{
    [ApiController]
    [Route("api/catalogue")]
    public class CatalogueController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly MyDbContext _db;
        private readonly string _iiBaseUrl = "http://134.122.40.121:5180/api/inventory_intelligence";

        public CatalogueController(IHttpClientFactory httpClientFactory, MyDbContext db)
        {
            _httpClient = httpClientFactory.CreateClient();
            _db = db;
        }

        // Resolves vendorName from session VendorId via DB
        private string? GetVendorName()
        {
            var vendorId = HttpContext.Session.GetInt32("VendorId");
            if (vendorId == null) return null;

            var vendor = _db.Vendors.FirstOrDefault(v => v.VendorId == vendorId);
            return vendor?.VendorName;
        }

        private static readonly string[] ValidUnits = { "kg", "lbs", "bundle", "punnet", "bag", "l", "ml" };

        // POST /api/catalogue/add
        [HttpPost("add")]
        public async Task<IActionResult> AddProduct([FromBody] ProductRequest request)
        {
            var vendorName = GetVendorName();
            if (vendorName == null)
                return Unauthorized(new { status = "error", message = "Please log in first" });

            if (!ValidUnits.Contains(request.Unit))
                return BadRequest(new { status = "error", message = $"Unit must be one of: {string.Join(", ", ValidUnits)}" });

            if (request.Quantity <= 0)
                return BadRequest(new { status = "error", message = "Quantity must be greater than 0" });

            if (request.Price < 0)
                return BadRequest(new { status = "error", message = "Price cannot be negative" });

            var productId = "PROD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            var iiPayload = new
            {
                vendorName = vendorName,
                productId = productId,
                productName = request.ProductName,
                categoryL1 = request.CategoryL1,
                categoryL2 = request.CategoryL2,
                categoryL3 = request.CategoryL3,
                unit = request.Unit,
                quantity = request.Quantity,
                price = request.Price
            };

            var json = JsonSerializer.Serialize(iiPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_iiBaseUrl}/vendor_inventory/add_item", content);

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { status = "error", message = "Failed to push to Inventory Intelligence" });

            return Ok(new
            {
                status = "success",
                productId = productId,
                message = "Product added successfully"
            });
        }

        // GET /api/catalogue
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var vendorName = GetVendorName();
            if (vendorName == null)
                return Unauthorized(new { status = "error", message = "Please log in first" });

            var response = await _httpClient.GetAsync($"{_iiBaseUrl}/vendor_inventory/vendor_records?vendorName={Uri.EscapeDataString(vendorName)}");

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { status = "error", message = "Failed to fetch from Inventory Intelligence" });

            var data = await response.Content.ReadAsStringAsync();
            return Ok(data);
        }

        // GET /api/catalogue/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProduct(string productId)
        {
            var vendorName = GetVendorName();
            if (vendorName == null)
                return Unauthorized(new { status = "error", message = "Please log in first" });

            var response = await _httpClient.GetAsync(
                $"{_iiBaseUrl}/vendor_inventory/vendor_records?vendorName={Uri.EscapeDataString(vendorName)}&productId={Uri.EscapeDataString(productId)}");

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { status = "error", message = "Failed to fetch product from Inventory Intelligence" });

            var data = await response.Content.ReadAsStringAsync();
            return Ok(data);
        }

        // PATCH /api/catalogue/update/{productId}
        [HttpPatch("update/{productId}")]
        public async Task<IActionResult> UpdateProduct(string productId, [FromBody] UpdateProductRequest request)
        {
            var vendorName = GetVendorName();
            if (vendorName == null)
                return Unauthorized(new { status = "error", message = "Please log in first" });

            if (request.Unit != null && !ValidUnits.Contains(request.Unit))
                return BadRequest(new { status = "error", message = $"Unit must be one of: {string.Join(", ", ValidUnits)}" });

            if (request.Quantity != null && request.Quantity <= 0)
                return BadRequest(new { status = "error", message = "Quantity must be greater than 0" });

            if (request.Price != null && request.Price < 0)
                return BadRequest(new { status = "error", message = "Price cannot be negative" });

            var iiPayload = new
            {
                vendorName = vendorName,
                productId = productId,
                productName = request.ProductName,
                categoryL1 = request.CategoryL1,
                categoryL2 = request.CategoryL2,
                categoryL3 = request.CategoryL3,
                unit = request.Unit,
                quantity = request.Quantity,
                price = request.Price
            };

            var json = JsonSerializer.Serialize(iiPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"{_iiBaseUrl}/vendor_inventory/update_item", content);

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { status = "error", message = "Failed to update in Inventory Intelligence" });

            return Ok(new { status = "success", message = "Product updated successfully" });
        }

        // DELETE /api/catalogue/remove/{productId}
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveProduct(string productId)
        {
            var vendorName = GetVendorName();
            if (vendorName == null)
                return Unauthorized(new { status = "error", message = "Please log in first" });

            var response = await _httpClient.DeleteAsync(
                $"{_iiBaseUrl}/vendor_inventory/remove_item?productId={Uri.EscapeDataString(productId)}&vendorName={Uri.EscapeDataString(vendorName)}");

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { status = "error", message = "Failed to remove from Inventory Intelligence" });

            return Ok(new { status = "success", message = "Product removed successfully" });
        }
    }
}