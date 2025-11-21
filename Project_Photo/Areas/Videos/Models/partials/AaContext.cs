using Microsoft.EntityFrameworkCore;

namespace Project_Photo.Areas.Videos.Models;

public partial class AaContext : DbContext
{

    public AaContext()
    {
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                 .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                 .AddJsonFile("appsettings.json")
                 .Build();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("Aa"));
        }
    }



}
