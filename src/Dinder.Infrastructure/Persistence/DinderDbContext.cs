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
            });
        }

        public DbSet<User> Users => Set<User>();
    }
}
