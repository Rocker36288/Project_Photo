using Microsoft.EntityFrameworkCore;

namespace Project_Photo.Areas.Videos.Models;

public partial class VideosDbContext : DbContext
{

    public VideosDbContext()
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
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("AA"));
        }
    }



}
