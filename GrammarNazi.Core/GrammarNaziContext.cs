using GrammarNazi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrammarNazi.Core
{
    public class GrammarNaziContext : DbContext
    {
        public GrammarNaziContext(DbContextOptions<GrammarNaziContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatConfiguration>()
                .HasKey(v => v.ChatId);
        }
    }
}