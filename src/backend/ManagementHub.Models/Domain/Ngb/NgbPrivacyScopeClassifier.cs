using System;
using System.Collections.Generic;
using ManagementHub.Models.Enums;

namespace ManagementHub.Models.Domain.Ngb;

public static class NgbPrivacyScopeClassifier
{
	private static readonly HashSet<string> EuropeanEconomicAreaCountryCodes = new(StringComparer.OrdinalIgnoreCase)
	{
		"AUT",
		"BEL",
		"BGR",
		"HRV",
		"CYP",
		"CZE",
		"DNK",
		"EST",
		"FIN",
		"FRA",
		"DEU",
		"GRC",
		"HUN",
		"IRL",
		"ITA",
		"LVA",
		"LTU",
		"LUX",
		"MLT",
		"NLD",
		"POL",
		"PRT",
		"ROU",
		"SVK",
		"SVN",
		"ESP",
		"SWE",
		"ISL",
		"LIE",
		"NOR",
	};

	public static PrivacyScope GetScope(NgbIdentifier ngb) => GetScope(ngb.NgbCode);

	public static PrivacyScope GetScope(string ngbCode)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(ngbCode);

		if (EuropeanEconomicAreaCountryCodes.Contains(ngbCode))
		{
			return PrivacyScope.EuropeanEconomicArea;
		}

		return ngbCode.ToUpperInvariant() switch
		{
			"GBR" => PrivacyScope.UnitedKingdom,
			"CHE" => PrivacyScope.Switzerland,
			_ => PrivacyScope.Global,
		};
	}

	public static bool RequiresRestrictedHandling(NgbIdentifier ngb) => GetScope(ngb) != PrivacyScope.Global;
}