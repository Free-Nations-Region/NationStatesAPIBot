﻿using System.Collections.Generic;

namespace NationStatesAPIBot.Entities
{
    public class Permission
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<UserPermissions> UserPermissions { get; set; } = new List<UserPermissions>();
        public List<RolePermissions> RolePermissions { get; set; } = new List<RolePermissions>();
    }
}
