using Microsoft.EntityFrameworkCore;
namespace ProcessorApi.Models
{
	public class RequestDbContext : DbContext
	{
		public RequestDbContext(DbContextOptions<RequestDbContext> options) : base(options)
		{
		}
		public DbSet<RequestLog> RequestLogs { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<RequestLog>()
				.HasIndex(r => r.StartTime)
				.HasDatabaseName("INDX_RequestLogs_StartTime_Desc")
				.IsDescending(true);
		}
	}
}
