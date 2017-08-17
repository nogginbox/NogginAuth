using Microsoft.EntityFrameworkCore;

namespace Noggin.SampleSite.Data
{
    public interface ISimpleDbContext
    {
        DbSet<User> Users { get; set; }
        int SaveChanges();
    }
}