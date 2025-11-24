using Microsoft.EntityFrameworkCore;

namespace Project_Photo.Areas.PhotographerBooking
{
    public partial class AAContext : DbContext
    {


        public AAContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfiguration Config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();
                optionsBuilder.UseSqlServer(Config.GetConnectionString("AA"));
            }
        }

    }

}
