using Microsoft.AspNetCore.Mvc;
using SupplyNetworkManagement.Data;
using System.Text;
using System.Text.Json;

namespace SupplyNetworkManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly MyDbContext m_db;
        private readonly HttpClient _httpClient;
        private readonly string _iiBaseUrl = "http://134.122.40.121:5180/api/inventory_intelligence";

        public VendorController(MyDbContext db, IHttpClientFactory httpClientFactory)
        {
            m_db = db;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        public ActionResult<IEnumerable<Vendor>> Get()
        {
            var vendors = m_db.Vendors.ToList();
            return Ok(vendors);
        }

        [HttpGet("{id}")]
        public ActionResult<Vendor> Get(int id)
        {
            var v = m_db.Vendors.Where(v => v.VendorId == id).SingleOrDefault();
            return Ok(v);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Vendor v)
        {
            // Save vendor to DB
            m_db.Vendors.Add(v);
            m_db.SaveChanges();

            // Register vendor with Inventory Intelligence
            var iiPayload = new
            {
                id = v.VendorId,
                name = v.VendorName,
                email = v.Email,
                dominant_product = ""
            };

            var json = JsonSerializer.Serialize(iiPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_iiBaseUrl}/vender_inventory/register_vendor", content);

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { status = "error", message = "Vendor saved but failed to register with Inventory Intelligence" });

            return Ok(new { status = "success", message = "Vendor registered successfully" });
        }

        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value) { }

        [HttpDelete("{id}")]
        public void Delete(int id) { }

        [HttpPost("login")]
        public IActionResult Login([FromBody] Vendor login)
        {
            var vendor = m_db.Vendors.FirstOrDefault(v => v.Email == login.Email);
            if (vendor == null)
                return NotFound("Vendor not found");
            if (vendor.Password != login.Password)
                return Unauthorized("Invalid password");
            HttpContext.Session.SetInt32("VendorId", vendor.VendorId);
            return Ok("Login successful");
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok("Logged out successfully");
        }
    }
}