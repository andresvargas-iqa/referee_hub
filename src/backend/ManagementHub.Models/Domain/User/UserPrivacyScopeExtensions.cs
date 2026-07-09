using System.Collections.Generic;
using System.Linq;
using ManagementHub.Models.Abstraction.Contexts;
using ManagementHub.Models.Domain.Ngb;
using ManagementHub.Models.Enums;

namespace ManagementHub.Models.Domain.User;

public static class UserPrivacyScopeExtensions
{
	public static bool CanAccessNgb(this IUserContext context, NgbIdentifier ngb)
	{
		var requiredScope = NgbPrivacyScopeClassifier.GetScope(ngb);
		return context.PrivacyScopes.Contains(requiredScope);
	}

	public static bool CanAccessUser(this IUserContext context, IUserContext targetUser)
	{
		var currentScopes = context.PrivacyScopes.ToHashSet();
		return targetUser.PrivacyScopes.All(currentScopes.Contains);
	}

	public static IReadOnlyCollection<PrivacyScope> AllScopes() =>
		new[]
		{
			PrivacyScope.Global,
			PrivacyScope.EuropeanEconomicArea,
			PrivacyScope.UnitedKingdom,
			PrivacyScope.Switzerland,
		};
}