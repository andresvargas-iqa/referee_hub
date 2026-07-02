using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagementHub.Models.Domain.Tournament;

namespace ManagementHub.Serialization.Identifiers;

public class TournamentParticipantIdentifierJsonConverter : JsonConverter<TournamentParticipantIdentifier>
{
	public override bool HandleNull => false;

	public override TournamentParticipantIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string? currentElement = reader.GetString();
		if (currentElement is null || !TournamentParticipantIdentifier.TryParse(currentElement, out var participantId))
		{
			throw new JsonException($"Could not read a {nameof(TournamentParticipantIdentifier)}.");
		}

		return participantId;
	}

	public override void Write(Utf8JsonWriter writer, TournamentParticipantIdentifier value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}