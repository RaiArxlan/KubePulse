using Microsoft.EntityFrameworkCore;
namespace ProcessorApi.Models
{
    public class RequestDbContext : DbContext
    {
        public RequestDbContext(DbContextOptions<RequestDbContext> options) : base(options) {}
        public DbSet<RequestLog> RequestLogs { get; set; }
    }
}
