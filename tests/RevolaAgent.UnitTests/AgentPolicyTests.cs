using RevolaAgent.Application.Agents;
using RevolaAgent.Infrastructure.Agents;
using System.Text.Json;
using Xunit;

namespace RevolaAgent.UnitTests;

public sealed class AgentPolicyTests
{
    [Theory]
    [InlineData("publish")]
    [InlineData("send_email")]
    [InlineData("invite_friends")]
    [InlineData("delete_account")]
    [InlineData("get_company_profile;publish")]
    public void External_or_injected_tools_are_not_allowed(string tool) => Assert.False(AgentPolicy.IsAllowedTool(tool));

    [Fact]
    public void Draft_contract_rejects_extra_commands_and_missing_fields()
    {
        Assert.Throws<JsonException>(() => OpenAiResponsesContract.ParseDraft("{\"title\":\"Test\",\"text\":\"Test\",\"imageBrief\":\"Test\",\"altText\":\"Test\",\"publish\":true}"));
        Assert.Throws<JsonException>(() => OpenAiResponsesContract.ParseDraft("{}"));
        var result = OpenAiResponsesContract.ParseDraft("{\"title\":\"Test\",\"text\":\"Test\",\"imageBrief\":\"Test\",\"altText\":\"Test\"}");
        Assert.Equal("Test", result.Title);
    }
}
