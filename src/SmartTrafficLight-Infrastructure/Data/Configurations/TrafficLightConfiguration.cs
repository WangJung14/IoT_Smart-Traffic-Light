using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrafficLight_Domain.Entities;

namespace SmartTrafficLight_Infrastructure.Data.Configurations
{
    public class TrafficLightConfiguration : IEntityTypeConfiguration<TrafficLight>
    {
        public void Configure(EntityTypeBuilder<TrafficLight> builder)
        {
            builder.HasKey(x => x.Id);

            // lien ket nhieu - mot voi Intersection
            builder.HasOne(x => x.Intersection)
                .WithMany(i => i.TrafficLights)
                .HasForeignKey(x => x.IntersectionId);
            // Luu enum Direction va LightState duoi dang string
            builder.Property(x => x.Direction).HasConversion<string>().IsRequired();
            builder.Property(x => x.CurrentState).HasConversion<string>().IsRequired();
            // Xu ly Value object TimingConfig
            builder.OwnsOne(x => x.CurrentTiming, timing =>
            {
                timing.Property(t => t.GreenDuration).HasColumnName("GreenDuration").IsRequired();
                timing.Property(t => t.RedDuration).HasColumnName("RedDuration").IsRequired();
                timing.Property(t => t.YellowDuration).HasColumnName("YellowDuration").IsRequired();
            }); 
        }
    }
}
