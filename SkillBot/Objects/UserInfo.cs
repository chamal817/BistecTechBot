using Newtonsoft.Json;
using System;

namespace BDO.Bot.BDOSkillBot.Objects
{
    public class UserInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Created { get; set; } = DateTime.Now;
        public string Email { get; set; }
        public string Name { get; set; }
        public string Reson { get; set; }




    }
}
