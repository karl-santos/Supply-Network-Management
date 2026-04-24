using Microsoft.AspNetCore.Mvc;
using SupplyNetworkManagement.Data;

namespace SupplyNetworkManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly MyDbContext m_db;
        public VendorController(MyDbContext db)
        {
            m_db = db;
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
        public void Post([FromBody] Vendor v)
        {
            m_db.Vendors.Add(v);
            m_db.SaveChanges();
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