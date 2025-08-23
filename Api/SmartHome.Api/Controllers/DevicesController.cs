using Microsoft.AspNetCore.Mvc;
using SmartHome.Slices.Devices.EntryPoints;
using SmartHome.Shared;

namespace SmartHome.Api;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase {
    private readonly IDevicesEntryPoint _entryPoint;

    public DevicesController(IDevicesEntryPoint entryPoint) {
        _entryPoint = entryPoint;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Device>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDevices() {
        var devices = await _entryPoint.GetDevicesAsync();
        return Ok(devices);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceById([FromRoute]int id) {
        var device = await _entryPoint.GetDeviceByIdAsync(id);
        if(device == null) {
            return NotFound();
        }
        return Ok(device);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SearchDeviceByName([FromRoute] string search) {
        var device = await _entryPoint.SearchDeviceByNameAsync(search);
        if(device == null) {
            return NotFound();
        }
        return Ok(device);
    }
}
