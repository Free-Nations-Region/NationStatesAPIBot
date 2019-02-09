using System;
using System.Collections.Generic;
using System.Text;

namespace NationStatesAPIBot.Entities
{
    public class Role
    {
        public long Id { get; set; }
        public string DiscordUserId { get; set; }
        public List<RolePermissions> RolePermissions { get; set; }
        public List<UserRoles> Users { get; set; }
    }
}
