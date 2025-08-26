using Microsoft.AspNetCore.Mvc;
using SmartHome.Slices.Devices.Services;
using SmartHome.Shared;

namespace SmartHome.Api;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase {
    private readonly IDevicesService _service;

    public DevicesController(IDevicesService service) {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Device>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDevices() {
        var result = await _service.GetAllDevices();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceById([FromRoute]string id) {
        var result = await _service.GetDeviceById(id);
        if(result == null) {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SearchDeviceByName([FromQuery]string name) {
        var result = await _service.SearchDeviceByName(name);
        if(result == null) {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Device), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDevice([FromBody] Device device) {
        try {
            var result = await _service.CreateDevice(device);

            return CreatedAtAction(nameof(CreateDevice), result);
        } catch (ArgumentException ex) {
            return BadRequest(ex.Message);
        }
    }

    //[HttpPatch("status/{id}/{active}")]
    //[ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> SetDeviceActiveStatus([FromRoute]string id, [FromRoute]bool active) {
        
    //}
}
