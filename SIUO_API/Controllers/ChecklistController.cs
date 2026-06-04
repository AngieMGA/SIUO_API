using Microsoft.AspNetCore.Mvc;

namespace SIUO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChecklistController : ControllerBase
    {
        [HttpPost]
        public IActionResult GuardarChecklist([FromBody] object checklist)
        {
            return Ok(new
            {
                mensaje = "Checklist recibido correctamente",
                data = checklist
            });
        }
    }
}