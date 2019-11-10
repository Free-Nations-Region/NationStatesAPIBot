using MongoDB.Bson;
using System.Collections.Generic;

namespace CyborgianStates.Models
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Sammlungseigenschaften müssen schreibgeschützt sein", Justification = "Necessary, because MongoDB needs to write it on serializing.")]
    public class User
    {
        public ObjectId Id { get; set; }
        public ulong DiscordUserId { get; set; }
        public List<ObjectId> Nations { get; set; } = new List<ObjectId>();
        public List<Permission> Permissions { get; set; } = new List<Permission>();
        public List<ObjectId> Roles { get; set; } = new List<ObjectId>();
    }
}
