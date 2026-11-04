using System.Text.Json;
using FluentAssertions;
using ManagementHub.Models.Domain.Tournament;
using ManagementHub.Models.Domain.User;
using ManagementHub.Models.Enums;
using ManagementHub.Service.Areas.Tournaments;
using Xunit;

namespace ManagementHub.UnitTests;

public class CreateInviteModelJsonConverterTests
{
	[Fact]
	public void SerializeAndDeserialize_PlayerInvite_PreservesParticipantType()
	{
		var request = new CreateInviteModel
		{
			ParticipantType = ParticipantType.Player,
			ParticipantId = TournamentParticipantIdentifier.ForPlayer(UserIdentifier.NewUserId())
		};
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

		var json = JsonSerializer.Serialize(request, options);
		using var document = JsonDocument.Parse(json);
		document.RootElement.GetProperty("participantType").GetString().Should().Be("player");
		document.RootElement.GetProperty("participantId").GetString().Should().Be(request.ParticipantId.ToString());

		var deserialized = JsonSerializer.Deserialize<CreateInviteModel>(json, options);
		deserialized.Should().NotBeNull();
		deserialized!.ParticipantType.Should().Be(ParticipantType.Player);
		deserialized.ParticipantId.Should().Be(request.ParticipantId);
	}
}