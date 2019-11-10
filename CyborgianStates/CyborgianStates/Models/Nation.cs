﻿using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace CyborgianStates.Models
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Sammlungseigenschaften müssen schreibgeschützt sein", Justification = "Necessary, because MongoDB needs to write it on serializing.")]
    public class Nation
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public List<Status> Status { get; set; } = new List<Status>();
    }
}
