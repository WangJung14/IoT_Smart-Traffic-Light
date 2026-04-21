

using SmartTrafficLight_Domain.Entities;

namespace SmartTrafficLight_Domain.ValueObjects
{
    public class PredictionResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IntersectionId { get; set; }

        public TimingConfig SuggestedTiming { get; set; } = null!;

        public double ConfidenceScore { get; set; } // do tin cay . example : 90.32

        public DateTime PredictAt { get; set; } = DateTime.UtcNow;
        
        //navigation property

        public Intersection? Intersection { get; set; }
    }
}
