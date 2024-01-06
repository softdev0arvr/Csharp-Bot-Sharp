using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.HttpHandler;

public class HttpHandlerPlugin : IBotSharpPlugin
{
    public string Name => "HTTP Handler";
    public string Description => "Empower agent to handle HTTP request in RESTful API or GraphQL";
    public string IconUrl => "https://lirp.cdn-website.com/6f8d6d8a/dms3rep/multi/opt/API_Icon-640w.png";
    public bool WithAgent => true;

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        
    }
}
