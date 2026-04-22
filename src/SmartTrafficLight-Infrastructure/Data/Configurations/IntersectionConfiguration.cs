using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrafficLight_Domain.Entities;

namespace SmartTrafficLight_Infrastructure.Data.Configurations
{
    public class IntersectionConfiguration : IEntityTypeConfiguration<Intersection>
    {
        public void Configure(EntityTypeBuilder<Intersection> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);
            builder.Property(x => x.Location)
                .IsRequired()
                .HasMaxLength(200);

            // Seed data example
            builder.HasData(new Intersection
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Nga Tu Hang Xanh",
                Location = "Ho Chi Minh City",
                NumberOfLanes = 4
            });

        }
    }
}
