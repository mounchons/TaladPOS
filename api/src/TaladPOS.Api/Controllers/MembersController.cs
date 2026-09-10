using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Members;
using TaladPOS.Domain.Members;

namespace TaladPOS.Api.Controllers;

[ApiController]
[Route("api/v1/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberRepository _memberRepository;
    private readonly RegisterMemberUseCase _registerMemberUseCase;

    public MembersController(IMemberRepository memberRepository, RegisterMemberUseCase registerMemberUseCase)
    {
        _memberRepository = memberRepository;
        _registerMemberUseCase = registerMemberUseCase;
    }

    public record RegisterMemberRequestDto(string Name, string PhoneNumber);

    public record MemberDto(Guid Id, string Name, string PhoneNumber, decimal AccumulatedPurchaseTotal);

    /// <summary>contracts/members.md - GET /api/v1/members (FR-012)</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> Search([FromQuery] string? search, CancellationToken ct)
    {
        var members = await _memberRepository.SearchAsync(search, ct);
        return Ok(members.Select(ToDto).ToList());
    }

    /// <summary>contracts/members.md - GET /api/v1/members/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemberDto>> GetById(Guid id, CancellationToken ct)
    {
        var member = await _memberRepository.GetByIdAsync(id, ct);
        return member is null ? NotFound() : Ok(ToDto(member));
    }

    /// <summary>contracts/members.md - POST /api/v1/members (FR-010, FR-011)</summary>
    [HttpPost]
    public async Task<ActionResult<MemberDto>> Register(RegisterMemberRequestDto request, CancellationToken ct)
    {
        var member = await _registerMemberUseCase.ExecuteAsync(
            new RegisterMemberRequest(request.Name, request.PhoneNumber), ct);

        return CreatedAtAction(nameof(GetById), new { id = member.Id }, ToDto(member));
    }

    private static MemberDto ToDto(Member member) =>
        new(member.Id, member.Name, member.PhoneNumber, member.AccumulatedPurchaseTotal);
}
