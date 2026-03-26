using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class SlotConfiguration : IEntityTypeConfiguration<Slot>
    {
        public void Configure(EntityTypeBuilder<Slot> builder)
        {
            builder.ToTable("Slots");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.DayOfWeek).IsRequired();
            builder.Property(e => e.StartTime).IsRequired().HasColumnType("time");
            builder.Property(e => e.EndTime).IsRequired().HasColumnType("time");

            // Seeding standard slots (8 AM - 10 PM, 1-hour intervals)
            var slots = new List<Slot>();
            int id = 1;
            for (int day = 0; day <= 6; day++) // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
            {
                for (int hour = 8; hour <= 21; hour++)
                {
                    slots.Add(new Slot
                    {
                        Id = id++,
                        DayOfWeek = day,
                        StartTime = new System.TimeOnly(hour, 0),
                        EndTime = new System.TimeOnly(hour + 1, 0)
                    });
                }
            }
            builder.HasData(slots);
        }
    }
}
