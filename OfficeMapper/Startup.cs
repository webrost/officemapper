using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(OfficeMapper.Startup))]
namespace OfficeMapper
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
