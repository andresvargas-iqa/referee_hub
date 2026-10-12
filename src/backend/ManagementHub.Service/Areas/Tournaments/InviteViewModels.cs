using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagementHub.Models.Domain.Team;
using ManagementHub.Models.Domain.Tournament;
using ManagementHub.Models.Domain.User;
using ManagementHub.Models.Enums;

namespace ManagementHub.Service.Areas.Tournaments;

public class TournamentInviteViewModel
{
	public required ParticipantType ParticipantType { get; set; }
	public required TournamentParticipantIdentifier ParticipantId { get; set; }
	public required string ParticipantName { get; set; }
	public string? Observations { get; set; }
	public Uri? LogoUri { get; set; }
	public required InviteStatus Status { get; set; }
	public required UserIdentifier InitiatorUserId { get; set; }
	public required DateTime CreatedAt { get; set; }
	public required ApprovalStatusViewModel TournamentManagerApproval { get; set; }
	public required ApprovalStatusViewModel ParticipantApproval { get; set; }
}

public class ApprovalStatusViewModel
{
	public required ApprovalStatus Status { get; set; }
	public DateTime? Date { get; set; }
}

[JsonConverter(typeof(CreateInviteModelJsonConverter))]
public class CreateInviteModel
{
	public required ParticipantType ParticipantType { get; set; }
	public required TournamentParticipantIdentifier ParticipantId { get; set; }
	public string? Observations { get; set; }
}

public sealed class CreateInviteModelJsonConverter : JsonConverter<CreateInviteModel>
{
	public override CreateInviteModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		var root = document.RootElement;
		var participantType = ReadParticipantType(GetProperty(root, "participantType"));
		var participantIdElement = GetProperty(root, "participantId");
		TournamentParticipantIdentifier participantId;

		if (participantIdElement.ValueKind == JsonValueKind.String)
		{
			var participantIdValue = participantIdElement.GetString();
			if (participantIdValue is null ||
				!TournamentParticipantIdentifier.TryParse(participantType, participantIdValue, out participantId))
			{
				throw new JsonException("Could not read the tournament participant identifier.");
			}
		}
		else if (participantIdElement.ValueKind == JsonValueKind.Object)
		{
			participantId = JsonSerializer.Deserialize<TournamentParticipantIdentifier>(participantIdElement.GetRawText(), options);
			if (participantId.ParticipantType != participantType)
			{
				throw new JsonException("Participant type does not match participant ID.");
			}
		}
		else
		{
			throw new JsonException("Could not read the tournament participant identifier.");
		}

		var observations = TryGetProperty(root, "observations", out var observationsElement) &&
			observationsElement.ValueKind == JsonValueKind.String
			? observationsElement.GetString()
			: null;

		return new CreateInviteModel
		{
			ParticipantType = participantType,
			ParticipantId = participantId,
			Observations = observations
		};
	}

	public override void Write(Utf8JsonWriter writer, CreateInviteModel value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("participantType", GetParticipantTypeValue(value.ParticipantType));
		writer.WriteString("participantId", value.ParticipantId.ToString());
		if (value.Observations is not null)
		{
			writer.WriteString("observations", value.Observations);
		}

		writer.WriteEndObject();
	}

	private static JsonElement GetProperty(JsonElement root, string propertyName)
	{
		if (TryGetProperty(root, propertyName, out var property))
		{
			return property;
		}

		throw new JsonException($"Missing required property '{propertyName}'.");
	}

	private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
	{
		foreach (var property in root.EnumerateObject())
		{
			if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
			{
				value = property.Value;
				return true;
			}
		}

		value = default;
		return false;
	}

	private static ParticipantType ReadParticipantType(JsonElement element)
	{
		if (element.ValueKind == JsonValueKind.String &&
			Enum.TryParse(element.GetString(), true, out ParticipantType participantType) &&
			Enum.IsDefined(typeof(ParticipantType), participantType))
		{
			return participantType;
		}

		if (element.ValueKind == JsonValueKind.Number &&
			element.TryGetInt32(out var participantTypeValue) &&
			Enum.IsDefined(typeof(ParticipantType), participantTypeValue))
		{
			return (ParticipantType)participantTypeValue;
		}

		throw new JsonException("Could not read the tournament participant type.");
	}

	private static string GetParticipantTypeValue(ParticipantType participantType)
		=> participantType switch
		{
			ParticipantType.Team => "team",
			ParticipantType.Player => "player",
			ParticipantType.Referee => "referee",
			_ => throw new JsonException("Invalid tournament participant type.")
		};
}

public class InviteResponseModel
{
	public bool Approved { get; set; }
}

public class TournamentParticipantViewModel
{
	public required TeamIdentifier TeamId { get; set; }
	public required string TeamName { get; set; }
	public Uri? LogoUri { get; set; }
	public required List<PlayerViewModel> Players { get; set; }
	public required List<StaffViewModel> Coaches { get; set; }
	public required List<StaffViewModel> Staff { get; set; }
}

public class StaffViewModel
{
	public required UserIdentifier UserId { get; set; }
	public required string UserName { get; set; }
}

public class PlayerViewModel : StaffViewModel
{
	public required string Number { get; set; }
	public string? Gender { get; set; }
}

public class RosterEntryViewModel
{
	public string? Name { get; set; }
	public string? Pronouns { get; set; }
	public string? Gender { get; set; }
	public string? JerseyNumber { get; set; }
	public RosterRole Role { get; set; }
	public string? MaxCertification { get; set; }
	public DateTime? MaxCertificationDate { get; set; }
}

