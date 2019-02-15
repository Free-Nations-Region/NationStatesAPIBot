using System;
using System.Collections.Generic;
using System.Text;

namespace NationStatesAPIBot.Entities
{
    public class Role
    {
        public long Id { get; set; }
        public string DiscordRoleId { get; set; }
        public string Description { get; set; }
        public List<RolePermissions> RolePermissions { get; set; }
        public List<UserRoles> Users { get; set; }
    }
}
