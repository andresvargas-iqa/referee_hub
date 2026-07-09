using System.Collections.Generic;
using System.Text.Json;
using ManagementHub.Models.Domain.User;
using ManagementHub.Models.Enums;

namespace ManagementHub.Models.Abstraction.Contexts;

/// <summary>
/// The context of the currently logged in user.
/// This class should serve as a basis for access control and is expected to be instantiated for every request.
/// </summary>
public interface IUserContext
{
	UserIdentifier UserId { get; }

	UserData UserData { get; }

	IEnumerable<IUserRole> Roles { get; }

	IEnumerable<PrivacyScope> PrivacyScopes { get; }
}
