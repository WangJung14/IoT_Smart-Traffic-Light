using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Domain.Enums;
using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight_Domain.Entities
{
    public class TrafficLight
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IntersectionId { get; set; }
        public Direction Direction { get; set; }

        public LightState CurrentState { get; set; }

        public TimingConfig CurrentTiming { get; set; } = new TimingConfig(30, 3, 20); // Default Value

        // Navigation property
        public Intersection? Intersection { get; set; }
    }
}
