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
        var devices = await _service.GetAllDevices();
        return Ok(devices);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceById([FromRoute]int id) {
        var device = await _service.GetDeviceById(id);
        if(device == null) {
            return NotFound();
        }
        return Ok(device);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SearchDeviceByName([FromQuery] string name) {
        var device = await _service.SearchDeviceByName(name);
        if(device == null) {
            return NotFound();
        }
        return Ok(device);
    }
}
