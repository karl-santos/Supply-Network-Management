using Microsoft.AspNetCore.Mvc;
using SupplyNetworkManagement.Data;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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

        // GET: api/Vendor
        [HttpGet]
        public ActionResult<IEnumerable<Vendor>> Get()
        {
            
            var vendors = m_db.Vendors.ToList();
            return Ok(vendors);
        }

        // GET api/Vendor/id#
        [HttpGet("{id}")]
        public ActionResult<Vendor> Get(int id)
        {
            var v = m_db.Vendors.Where(v => v.VendorId == id).SingleOrDefault();
            return Ok(v);
        }

        // POST api/Vendor
        [HttpPost]
        public void Post([FromBody]Vendor v)
        {
        
            m_db.Vendors.Add(v);
            m_db.SaveChanges();
        }

        // PUT api/Vendor/
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/Vendor/
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
