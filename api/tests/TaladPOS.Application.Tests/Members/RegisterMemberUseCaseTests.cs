using FluentAssertions;
using TaladPOS.Application.Members;
using TaladPOS.Domain.Members;
using Xunit;

namespace TaladPOS.Application.Tests.Members;

/// <summary>tasks.md T046 - US4: registration rejects a PhoneNumber already used by another member (FR-011).</summary>
public class RegisterMemberUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithPhoneNumberAlreadyRegistered_ThrowsDuplicatePhoneNumberException()
    {
        var members = new FakeMemberRepository(new Member("สมชาย ใจดี", "0812345678"));
        var useCase = new RegisterMemberUseCase(members);

        var request = new RegisterMemberRequest("สมหญิง ใจดี", "0812345678");

        var act = async () => await useCase.ExecuteAsync(request);

        await act.Should().ThrowAsync<DuplicatePhoneNumberException>();
        members.Added.Should().BeEmpty("a rejected registration must not persist the member");
    }

    [Fact]
    public async Task ExecuteAsync_WithNewPhoneNumber_RegistersMemberWithZeroAccumulatedTotal()
    {
        var members = new FakeMemberRepository();
        var useCase = new RegisterMemberUseCase(members);

        var request = new RegisterMemberRequest("สมชาย ใจดี", "0812345678");

        var member = await useCase.ExecuteAsync(request);

        member.AccumulatedPurchaseTotal.Should().Be(0m);
        members.Added.Should().ContainSingle().Which.Should().BeSameAs(member);
    }

    private sealed class FakeMemberRepository : IMemberRepository
    {
        private readonly Dictionary<Guid, Member> _members;

        public FakeMemberRepository(params Member[] members) => _members = members.ToDictionary(m => m.Id);

        public List<Member> Added { get; } = [];

        public Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_members.GetValueOrDefault(id));

        public Task<IReadOnlyList<Member>> SearchAsync(string? search, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Member>>(_members.Values.ToList());

        public Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken ct = default) =>
            Task.FromResult(_members.Values.Any(m => m.PhoneNumber == phoneNumber));

        public Task AddAsync(Member member, CancellationToken ct = default)
        {
            _members[member.Id] = member;
            Added.Add(member);
            return Task.CompletedTask;
        }

        public Task IncreaseAccumulatedPurchaseTotalAsync(Guid memberId, decimal amount, CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");
    }
}
