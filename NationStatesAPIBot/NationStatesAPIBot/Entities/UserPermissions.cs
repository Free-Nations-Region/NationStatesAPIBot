using System;
using System.Collections.Generic;
using System.Text;

namespace NationStatesAPIBot.Entities
{
    public class UserPermissions
    {
        public long UserId { get; set; }
        public User User { get; set; }

        public long PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
}
