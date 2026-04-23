namespace SmartTrafficLight_Domain.Entities
{
    public class Intersection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int NumberOfLanes { get; set; }

        // Navigation properties
        public ICollection<TrafficLight> TrafficLights { get; set; } = new List<TrafficLight>();
        public ICollection<TrafficData> TrafficDatas { get; set; } = new List<TrafficData>();
    }
}
