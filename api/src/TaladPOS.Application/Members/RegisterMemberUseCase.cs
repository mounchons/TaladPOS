using TaladPOS.Domain.Members;

namespace TaladPOS.Application.Members;

public sealed record RegisterMemberRequest(string Name, string PhoneNumber);

/// <summary>contracts/members.md - POST /api/members (FR-010, FR-011)</summary>
public sealed class RegisterMemberUseCase
{
    private readonly IMemberRepository _members;

    public RegisterMemberUseCase(IMemberRepository members) => _members = members;

    public async Task<Member> ExecuteAsync(RegisterMemberRequest request, CancellationToken ct = default)
    {
        if (await _members.PhoneNumberExistsAsync(request.PhoneNumber, ct))
        {
            throw new DuplicatePhoneNumberException(request.PhoneNumber);
        }

        var member = new Member(request.Name, request.PhoneNumber);
        await _members.AddAsync(member, ct);
        return member;
    }
}
