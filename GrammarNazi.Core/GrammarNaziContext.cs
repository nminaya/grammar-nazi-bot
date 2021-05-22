using GrammarNazi.Core.Extensions;
using GrammarNazi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
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

            modelBuilder.Entity<ScheduledTweet>()
                .HasKey(v => v.Id);

            modelBuilder.Entity<DiscordChannelConfig>()
                .HasKey(v => v.ChannelId);

            var valueComparerWhiteListWords = new ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            modelBuilder.Entity<ChatConfiguration>()
                .Property(e => e.WhiteListWords)
                .HasConversion(v => v.Join(","), v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .HasField("_whiteListWords")
                .UsePropertyAccessMode(PropertyAccessMode.Property)
                .Metadata.SetValueComparer(valueComparerWhiteListWords);

            modelBuilder.Entity<DiscordChannelConfig>()
                .Property(e => e.WhiteListWords)
                .HasConversion(v => string.Join(",", v), v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .HasField("_whiteListWords")
                .UsePropertyAccessMode(PropertyAccessMode.Property)
                .Metadata.SetValueComparer(valueComparerWhiteListWords);
        }
    }
}