using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;

namespace BDORootBot.Controllers
{
    /// <summary>
    /// A controller that handles skill replies to the bot.
    /// This example uses the <see cref="CloudSkillHandler"/> that is registered as a <see cref="ChannelServiceHandlerBase"/> in startup.cs.
    /// </summary>
    [ApiController]
    [Route("api/skills")]
    public class SkillController : ChannelServiceController
    {
        public SkillController(ChannelServiceHandlerBase handler)
            : base(handler)
        {
        }
    }
}
