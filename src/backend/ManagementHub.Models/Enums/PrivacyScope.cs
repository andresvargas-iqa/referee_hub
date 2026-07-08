using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ManagementHub.Models.Enums;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum PrivacyScope
{
	[EnumMember(Value = "global")]
	Global = 0,

	[EnumMember(Value = "european_economic_area")]
	EuropeanEconomicArea = 1,

	[EnumMember(Value = "united_kingdom")]
	UnitedKingdom = 2,

	[EnumMember(Value = "switzerland")]
	Switzerland = 3,
}