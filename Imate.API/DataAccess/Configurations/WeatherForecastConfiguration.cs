using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Imate.API.Models;

namespace Imate.API.DataAccess.Configurations
{
    public class WeatherForecastConfiguration : IEntityTypeConfiguration<WeatherForecast>
    {
        public void Configure(EntityTypeBuilder<WeatherForecast> builder)
        {
            builder.HasKey(e => e.Date); // Assuming Date is key for example
            builder.Property(e => e.Summary).HasMaxLength(200);
        }
    }
}
