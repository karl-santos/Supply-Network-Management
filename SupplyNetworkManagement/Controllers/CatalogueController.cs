using Microsoft.AspNetCore.Mvc;
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
        private readonly string _iiBaseUrl = "http://localhost:5180/api/inventory_intelligence";



        public CatalogueController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // Helper to get vendorId from session
        private int? GetVendorId()
        {
            return HttpContext.Session.GetInt32("VendorId");
        }


        // POST /api/catalogue/add
        [HttpPost("add")]
        public async Task<IActionResult> AddProduct([FromBody] ProductRequest request)
        {
            var vendorId = GetVendorId();
            if (vendorId == null)
                return Unauthorized(new { status = "error", message = "Please log in first" });

            // Validate unit
            if (request.Unit != "kg" && request.Unit != "l")
                return BadRequest(new { status = "error", message = "Unit must be kg or l" });

            // Validate quantity
            if (request.Quantity <= 0)
                return BadRequest(new { status = "error", message = "Quantity must be greater than 0" });

            // Validate price
            if (request.Price < 0)
                return BadRequest(new { status = "error", message = "Price cannot be negative" });

            // Generate productId
            var productId = "PROD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // Build II payload
            var iiPayload = new
            {
                vendorId = vendorId.ToString(),
                productId = productId,
                productName = request.ProductName,
                categoryL1 = request.CategoryL1,
                categoryL2 = request.CategoryL2,
                categoryL3 = request.CategoryL3,
                unit = request.Unit,
                quantity = request.Quantity,
                price = request.Price
            };

            // Push to II
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
            var vendorId = GetVendorId();
            if (vendorId == null)
                return Unauthorized(new { status = "error", message = "Please log in first" });

            // Pull from II
            var response = await _httpClient.GetAsync($"{_iiBaseUrl}/vendor_inventory/vendor_records?vendorId={vendorId}");

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { status = "error", message = "Failed to fetch from Inventory Intelligence" });

            var data = await response.Content.ReadAsStringAsync();
            return Ok(data);
        }

        // GET /api/catalogue/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProduct(string productId)
        {
            var vendorId = GetVendorId();
            if (vendorId == null)
                return Unauthorized(new { status = "error", message = "Please log in first" });

            var response = await _httpClient.GetAsync($"{_iiBaseUrl}/vendor_inventory/vendor_records?productId={productId}");

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { status = "error", message = "Failed to fetch product from Inventory Intelligence" });

            var data = await response.Content.ReadAsStringAsync();
            return Ok(data);
        }





    }

}
