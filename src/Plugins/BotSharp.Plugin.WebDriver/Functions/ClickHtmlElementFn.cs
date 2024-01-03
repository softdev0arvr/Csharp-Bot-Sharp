using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.WebDriver.Functions;

public class ClickHtmlElementFn : IFunctionCallback
{
    public string Name => "click_html_element";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public ClickHtmlElementFn(IServiceProvider services,
        PlaywrightWebDriver driver)
    {
        _services = services;
        _driver = driver;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        await _driver.ClickElement(agent, args, message.MessageId);

        message.Content = "Executed successfully.";

        return true;
    }
}
