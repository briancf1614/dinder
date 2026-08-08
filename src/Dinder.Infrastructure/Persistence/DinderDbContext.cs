using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dinder.Infrastructure.Persistence
{
    public class DinderDbContext : DbContext, IApplicationDbContext
    {
        public DinderDbContext(DbContextOptions<DinderDbContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(user =>
            {
                user.HasKey(u => u.Id);
                user.Property(u => u.Email).HasMaxLength(256).IsRequired();
                user.HasIndex(u => u.Email).IsUnique();
                user.Property(u => u.PasswordHash).IsRequired();
                user.Property(u => u.DisplayName).HasMaxLength(100);
                user.Property(u => u.Bio).HasMaxLength(500);
                user.Property(u => u.BirthDate).HasColumnType("date");
                user.Property(u => u.Gender)
                    .HasConversion<string>()
                    .HasMaxLength(20);
            });
        }

        public DbSet<User> Users => Set<User>();
    }
}
