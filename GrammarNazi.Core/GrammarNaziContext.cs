using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

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

            modelBuilder.Entity<TwitterLog>()
                .HasKey(v => v.TweetId);

            modelBuilder.Entity<ChatConfiguration>()
                .Property(e => e.WhiteListWords)
                .HasConversion(v => v.Join(","), v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        }
    }
}