using System.Collections.Generic;

namespace powerplant.Dtos
{
    public class Request
    {
        public decimal Load { get; set; }
        public Fuels Fuels { get; set; }
        public ICollection<Powerplant> Powerplants { get; set; }
    }
}