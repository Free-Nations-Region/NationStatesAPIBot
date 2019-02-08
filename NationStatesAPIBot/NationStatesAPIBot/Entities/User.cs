using System;
using System.Collections.Generic;
using System.Text;

namespace NationStatesAPIBot.Entities
{
    public class User
    {
        public long Id { get; set; }
        public string DiscordUserId { get; set; }
        public List<UserPermissions> Permissions {get; set;}
    }
}
