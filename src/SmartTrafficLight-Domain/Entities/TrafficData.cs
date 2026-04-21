using SmartTrafficLight_Domain.Enums;

namespace SmartTrafficLight_Domain.Entities
{
    public class TrafficData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IntersectionId { get; set; }
        public Direction Direction{ get; set; }

        public int VehicleCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // navigation property
        public Intersection? Intersection { get; set; }
    }
}