using Microsoft.EntityFrameworkCore;
using NationStatesAPIBot.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NationStatesAPIBot
{
    public class BotDbContext: DbContext
    {
        public DbSet<Nation> Nations { get; set; }
        public DbSet<NationStatus> NationStatuses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("File=bot.db");
        }
    }
}
