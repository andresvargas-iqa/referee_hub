using System.Collections.Generic;
using ManagementHub.Models.Abstraction;
using ManagementHub.Models.Abstraction.Contexts;
using ManagementHub.Models.Domain.General;
using ManagementHub.Models.Domain.Ngb;
using ManagementHub.Models.Domain.User;
using ManagementHub.Models.Enums;
using Xunit;

namespace ManagementHub.UnitTests.Domain;

public class UserPrivacyScopeAccessTests
{
	[Fact]
	public void CanAccessNgb_ReturnsTrue_WhenUserHasRequiredScope()
	{
		var user = CreateUserContext([PrivacyScope.EuropeanEconomicArea]);

		var canAccess = user.CanAccessNgb(NgbIdentifier.Parse("FRA"));

		Assert.True(canAccess);
	}

	[Fact]
	public void CanAccessNgb_ReturnsFalse_WhenUserDoesNotHaveRequiredScope()
	{
		var user = CreateUserContext([PrivacyScope.Global]);

		var canAccess = user.CanAccessNgb(NgbIdentifier.Parse("GBR"));

		Assert.False(canAccess);
	}

	[Fact]
	public void CanAccessUser_ReturnsTrue_WhenTargetScopesAreSubset()
	{
		var currentUser = CreateUserContext([PrivacyScope.Global, PrivacyScope.EuropeanEconomicArea, PrivacyScope.UnitedKingdom]);
		var targetUser = CreateUserContext([PrivacyScope.EuropeanEconomicArea]);

		var canAccess = currentUser.CanAccessUser(targetUser);

		Assert.True(canAccess);
	}

	[Fact]
	public void CanAccessUser_ReturnsFalse_WhenTargetIncludesMissingScope()
	{
		var currentUser = CreateUserContext([PrivacyScope.EuropeanEconomicArea]);
		var targetUser = CreateUserContext([PrivacyScope.EuropeanEconomicArea, PrivacyScope.UnitedKingdom]);

		var canAccess = currentUser.CanAccessUser(targetUser);

		Assert.False(canAccess);
	}

	private static IUserContext CreateUserContext(IEnumerable<PrivacyScope> scopes) =>
		new TestUserContext(
			UserIdentifier.FromLegacyUserId(1),
			new UserData(new Email("scope.test@example.com"), "Scope", "Tester"),
			[],
			scopes);

	private sealed record TestUserContext(
		UserIdentifier UserId,
		UserData UserData,
		IEnumerable<IUserRole> Roles,
		IEnumerable<PrivacyScope> PrivacyScopes) : IUserContext;
}