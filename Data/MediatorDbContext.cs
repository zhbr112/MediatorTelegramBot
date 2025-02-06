using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediatorTelegramBot.Data;

public class MediatorDbContext:DbContext
{
    public DbSet<Mediator> Mediators { get; set; }

    public MediatorDbContext(DbContextOptions options) : base(options)
    {
        Database.EnsureCreated();
    }
}
