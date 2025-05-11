using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;
    
    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public IActionResult Post([FromBody] Warehouse warehouse)
    {
        if (warehouse == null)
            return BadRequest();
        
        if (warehouse.Amount <= 0)
            return BadRequest();
        
        int id = _dbService.addProduct(warehouse).Result;
        
        return Created("Created ",new { Id = id });
        
    }
}