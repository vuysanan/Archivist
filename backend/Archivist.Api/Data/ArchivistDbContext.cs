using Archivist.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Archivist.Api.Data
{
    public class ArchivistDbContext : DbContext
    {
        public ArchivistDbContext(DbContextOptions<ArchivistDbContext> options) : base(options) { }

        public DbSet<SampleSuggestion> Suggestions => Set<SampleSuggestion>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SampleSuggestion>(e =>
            {
                e.Property(x => x.Status).HasConversion<string>(); // stored as "Pending", "Approved", ...
                e.HasIndex(x => x.TrackKey);
            });
        }
    }
}