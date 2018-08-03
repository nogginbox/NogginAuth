using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Noggin.SampleSite.Data
{
    /// <summary>
    /// Context for a simple database of users
    /// </summary>
    /// <remarks>
    /// This sample project uses the SQLite EF Core Provider.
    /// A nice provider for samples, but there are some limitations:
    /// https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations
    /// You might want to use a different provider for your project, but this code will not change, just the Nuget packages.
    /// </remarks>
    public class SampleSimpleDbContext: DbContext, ISimpleDbContext
    {
		public SampleSimpleDbContext()
		{
		}

        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlite("Data Source=DataContent/noggin.db");
            }
        }
    }
}