using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight_Infrastructure.Persistence
{
    public class PredictionResultConfiguration : IEntityTypeConfiguration<PredictionResult>
    {
        public void Configure(EntityTypeBuilder<PredictionResult> builder)
        {
            builder.HasKey(x => x.Id);

            // Liên kết khóa ngoại với bảng Giao lộ
            builder.HasOne(x => x.Intersection)
                   .WithMany() // Không cần List<PredictionResult> ở entity Intersection để tránh phình data
                   .HasForeignKey(x => x.IntersectionId);

            // Quan trọng nhất: Hướng dẫn EF Core cách map TimingConfig
            builder.OwnsOne(x => x.SuggestedTiming, timing =>
            {
                timing.Property(t => t.GreenDuration).HasColumnName("SuggestedGreen");
                timing.Property(t => t.YellowDuration).HasColumnName("SuggestedYellow");
                timing.Property(t => t.RedDuration).HasColumnName("SuggestedRed");
            });
        }
    }
}
