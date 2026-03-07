
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.UseCases;
using Sovereign.Application.DTOs;

namespace Sovereign.API.Controllers;

[ApiController]
[Route("api/relationships")]
public class RelationshipController : ControllerBase
{
    private readonly CreateRelationshipUseCase _createRelationship;

    public RelationshipController(CreateRelationshipUseCase createRelationship)
    {
        _createRelationship = createRelationship;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRelationshipRequest request)
    {
        var result = await _createRelationship.ExecuteAsync(request);
        return Ok(result);
    }
}
