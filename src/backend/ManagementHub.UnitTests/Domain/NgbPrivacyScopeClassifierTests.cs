using ManagementHub.Models.Domain.Ngb;
using ManagementHub.Models.Enums;
using Xunit;

namespace ManagementHub.UnitTests.Domain;

public class NgbPrivacyScopeClassifierTests
{
	[Theory]
	[InlineData("FRA", PrivacyScope.EuropeanEconomicArea)]
	[InlineData("DEU", PrivacyScope.EuropeanEconomicArea)]
	[InlineData("NOR", PrivacyScope.EuropeanEconomicArea)]
	[InlineData("GBR", PrivacyScope.UnitedKingdom)]
	[InlineData("CHE", PrivacyScope.Switzerland)]
	[InlineData("USA", PrivacyScope.Global)]
	[InlineData("ARG", PrivacyScope.Global)]
	public void GetScope_ClassifiesRepresentativeJurisdictions(string ngbCode, PrivacyScope expected)
	{
		var scope = NgbPrivacyScopeClassifier.GetScope(NgbIdentifier.Parse(ngbCode));

		Assert.Equal(expected, scope);
	}

	[Theory]
	[InlineData("FRA", true)]
	[InlineData("GBR", true)]
	[InlineData("CHE", true)]
	[InlineData("USA", false)]
	public void RequiresRestrictedHandling_ReturnsExpectedValue(string ngbCode, bool expected)
	{
		var requiresRestrictedHandling = NgbPrivacyScopeClassifier.RequiresRestrictedHandling(NgbIdentifier.Parse(ngbCode));

		Assert.Equal(expected, requiresRestrictedHandling);
	}
}