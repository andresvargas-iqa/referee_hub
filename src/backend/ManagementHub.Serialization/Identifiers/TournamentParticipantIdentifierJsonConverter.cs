using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagementHub.Models.Domain.Team;
using ManagementHub.Models.Domain.Tournament;
using ManagementHub.Models.Domain.User;
using ManagementHub.Models.Enums;

namespace ManagementHub.Serialization.Identifiers;

public class TournamentParticipantIdentifierJsonConverter : JsonConverter<TournamentParticipantIdentifier>
{
	public override bool HandleNull => false;

	public override TournamentParticipantIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			string? currentElement = reader.GetString();
			if (currentElement is not null && TournamentParticipantIdentifier.TryParse(currentElement, out var participantId))
			{
				return participantId;
			}

			throw new JsonException($"Could not read a {nameof(TournamentParticipantIdentifier)}.");
		}

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			using var document = JsonDocument.ParseValue(ref reader);
			var root = document.RootElement;

			if (TryReadTeamParticipant(root, out var teamParticipant))
			{
				return teamParticipant;
			}

			if (TryReadRefereeParticipant(root, out var refereeParticipant))
			{
				return refereeParticipant;
			}
		}

		throw new JsonException($"Could not read a {nameof(TournamentParticipantIdentifier)}.");
	}

	public override void Write(Utf8JsonWriter writer, TournamentParticipantIdentifier value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}

	private static bool TryReadTeamParticipant(JsonElement root, out TournamentParticipantIdentifier participantIdentifier)
	{
		participantIdentifier = default;

		if (TryReadParticipantType(root, out var participantType) && participantType != ParticipantType.Team)
		{
			return false;
		}

		if (TryReadStringProperty(root, "teamId", out var teamIdValue) && TeamIdentifier.TryParse(teamIdValue, out var teamId))
		{
			participantIdentifier = TournamentParticipantIdentifier.ForTeam(teamId);
			return true;
		}

		if (TryReadStringProperty(root, "participantId", out var participantIdValue) && TeamIdentifier.TryParse(participantIdValue, out teamId))
		{
			participantIdentifier = TournamentParticipantIdentifier.ForTeam(teamId);
			return true;
		}

		return false;
	}

	private static bool TryReadRefereeParticipant(JsonElement root, out TournamentParticipantIdentifier participantIdentifier)
	{
		participantIdentifier = default;

		if (TryReadParticipantType(root, out var participantType) && participantType != ParticipantType.Referee)
		{
			return false;
		}

		if (TryReadStringProperty(root, "userId", out var userIdValue) && UserIdentifier.TryParse(userIdValue, out var userId))
		{
			participantIdentifier = TournamentParticipantIdentifier.ForReferee(userId);
			return true;
		}

		if (TryReadStringProperty(root, "participantId", out var participantIdValue) && UserIdentifier.TryParse(participantIdValue, out userId))
		{
			participantIdentifier = TournamentParticipantIdentifier.ForReferee(userId);
			return true;
		}

		return false;
	}

	private static bool TryReadParticipantType(JsonElement root, out ParticipantType participantType)
	{
		participantType = default;

		if (root.TryGetProperty("participantType", out var participantTypeElement))
		{
			if (participantTypeElement.ValueKind == JsonValueKind.String)
			{
				var typeString = participantTypeElement.GetString();
				if (typeString is not null && Enum.TryParse(typeString, true, out participantType))
				{
					return true;
				}
			}

			if (participantTypeElement.ValueKind == JsonValueKind.Number && participantTypeElement.TryGetInt32(out var typeNumber))
			{
				participantType = (ParticipantType)typeNumber;
				return true;
			}
		}

		return false;
	}

	private static bool TryReadStringProperty(JsonElement root, string propertyName, out string value)
	{
		value = string.Empty;

		if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
		{
			return false;
		}

		var parsed = property.GetString();
		if (string.IsNullOrWhiteSpace(parsed))
		{
			return false;
		}

		value = parsed;
		return true;
	}
}