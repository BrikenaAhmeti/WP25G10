using Microsoft.EntityFrameworkCore;
using System;
using WP25G10.Data;

namespace WP25G10.Tests.Helpers
{
    public static class DbContextFactory
    {
        public static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}