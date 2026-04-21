using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartTrafficLight_Domain.Entities;

namespace SmartTrafficLight_Domain.Interfaces
{
    public interface ITrafficDataRepository
    {
        Task AddAsync(TrafficData trafficData);
        Task<IEnumerable<TrafficData>> GetRecentDataAsync(Guid intersectionId, int minutes);
    }
}
