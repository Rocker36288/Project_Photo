using Microsoft.EntityFrameworkCore;

namespace Project_Photo.Models
{
    public partial class AaContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                                                      .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                                      .AddJsonFile("appsettings.json")
                                                      .Build();

                optionsBuilder.UseSqlServer(configuration.GetConnectionString("AA"));
            }
        }
    }
}
