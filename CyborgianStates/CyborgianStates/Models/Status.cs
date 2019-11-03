using System;
using System.Collections.Generic;
using System.Text;

namespace CyborgianStates.Models
{
    public class Status
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public bool Active { get; set; }
    }
}
