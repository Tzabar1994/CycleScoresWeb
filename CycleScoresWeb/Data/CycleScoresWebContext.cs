using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CycleScoresWeb.Models;

namespace CycleScoresWeb.Data
{
    public class CycleScoresWebContext : DbContext
    {
        public CycleScoresWebContext (DbContextOptions<CycleScoresWebContext> options)
            : base(options)
        {
        }

        public DbSet<CycleScoresWeb.Models.CycleEvent> Events { get; set; } = default!;
        public DbSet<CycleScoresWeb.Models.Race> Race { get; set; } = default!;
    }
}
