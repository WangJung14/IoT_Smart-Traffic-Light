namespace SmartTrafficLight_Domain.ValueObjects
{
    public record TimingConfig
    {
        public int GreenDuration { get; init; }
        public int YellowDuration { get; init; }
        public int RedDuration { get; init; }
        public TimingConfig(int greenDuration, int yellowDuration, int redDuration)
        {
            // validate thoi gian khong duoc am
            if(greenDuration < 0 || yellowDuration < 0 || redDuration < 0)
            {
                throw new ArgumentException("Durations must be non-negative"); 
            }

            GreenDuration = greenDuration;
            YellowDuration = yellowDuration;
            RedDuration = redDuration;
        }
    }
}
