using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(webrtc_dotnetframework.Startup))]

namespace webrtc_dotnetframework
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure SignalR
            app.MapSignalR();
        }
    }
}
