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
    public class TrafficDataConfiguration : IEntityTypeConfiguration<TrafficData>
    {
        public void Configure(EntityTypeBuilder<TrafficData> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Intersection)
                   .WithMany(i => i.TrafficDatas)
                   .HasForeignKey(x => x.IntersectionId);

            builder.Property(x => x.Direction).HasConversion<string>();

            // Tạo Index cho cột Timestamp để tăng tốc độ query biểu đồ sau này
            builder.HasIndex(x => x.Timestamp);
        }
    }
}
