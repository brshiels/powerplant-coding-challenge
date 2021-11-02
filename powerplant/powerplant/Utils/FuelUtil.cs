using powerplant.Dtos;
using System.Collections.Generic;

namespace powerplant.Utils
{
    /// <summary>
    /// Merit Order is based on fuel price and efficiency :
    /// 1. Wind
    /// 2. Gas
    /// 3. Kerosine
    /// </summary>
    public class FuelUtil
    {
        public const string Wind = "Wind";
        public const string windturbine = "windturbine";

        public const string Gas = "Gas";
        public const string gasfired = "gasfired";
        public const decimal tonOfCo2PerMwh = 0.3m;

        public const string Kerosine = "Kerosine";
        public const string turbojet = "turbojet";

        private static readonly IDictionary<string, string> FuelForType = new Dictionary<string, string>() { { windturbine, Wind }, { gasfired, Gas }, { turbojet, Kerosine } };

        public static string GetFuelFor(string type)
        {
            try
            {
                return FuelForType[type];
            }
            catch (KeyNotFoundException)
            {
                return default;
            }
        }

        public class PowerTemplate
        {
            public decimal Power { get; set; }
            public ICollection<Reply> Replies { get; set; }
        }

        public ICollection<Reply> ComputeFuel(Request request)
        {
            IList<Powerplant> windPowerplant = new List<Powerplant>();
            IList<Powerplant> gasPowerplant = new List<Powerplant>();
            IList<Powerplant> kerosinePowerplant = new List<Powerplant>();

            foreach (Powerplant powerplant in request.Powerplants)
            {
                string fuel = GetFuelFor(powerplant.Type);

                switch (fuel)
                {
                    case Wind:
                        windPowerplant.Add(powerplant);
                        break;
                    case Gas:
                        gasPowerplant.Add(powerplant);
                        break;
                    case Kerosine:
                        kerosinePowerplant.Add(powerplant);
                        break;
                    default:
                        break;
                }
            }

            PowerTemplate powerTemplateWind;
            PowerTemplate powerTemplateGas;
            PowerTemplate powerTemplateKerosine;
            List<Reply> replies = new List<Reply>();

            powerTemplateWind = ComputeWind(windPowerplant, request.Fuels.Wind, 0, request.Load);
            replies.AddRange(powerTemplateWind.Replies);
            powerTemplateGas = ComputeGas(gasPowerplant, request.Fuels.Gas, request.Fuels.Co2, powerTemplateWind.Power, request.Load);
            replies.AddRange(powerTemplateGas.Replies);
            powerTemplateKerosine = ComputeKerosine(kerosinePowerplant, request.Fuels.Kerosine, powerTemplateGas.Power, request.Load);
            replies.AddRange(powerTemplateKerosine.Replies);

            return replies;
        }

        public PowerTemplate ComputeWind(IList<Powerplant> windPowerplant, decimal wind, decimal current, decimal load)
        {
            decimal realWind = wind / 100;
            ICollection<Reply> replies = new List<Reply>();

            for (int i = 0; i < windPowerplant.Count; ++i)
            {
                Reply reply = new Reply();
                reply.Name = windPowerplant[i].Name;

                if (!CheckLoadFull(current, load))
                {
                    decimal pmin = windPowerplant[i].Pmin * realWind;
                    decimal pmax = windPowerplant[i].Pmax * realWind;
                    decimal nextPmin = GetNextPmin(windPowerplant, i) * realWind;
                    decimal powerNeeded = ComputeOptimalPower(current, load, pmin, pmax, nextPmin);

                    reply.P = powerNeeded;
                    current += powerNeeded;
                    reply.Cost = 0;
                }

                replies.Add(reply);
            }

            PowerTemplate powerTemplate = new PowerTemplate();
            powerTemplate.Power = current;
            powerTemplate.Replies = replies;

            return powerTemplate;
        }

        public PowerTemplate ComputeGas(IList<Powerplant> gasPowerplant, decimal gas, decimal co2, decimal current, decimal load)
        {
            ICollection<Reply> replies = new List<Reply>();

            for (int i = 0; i < gasPowerplant.Count; ++i)
            {
                Reply reply = new Reply();
                reply.Name = gasPowerplant[i].Name;

                if (!CheckLoadFull(current, load))
                {
                    decimal pmin = gasPowerplant[i].Pmin;
                    decimal pmax = gasPowerplant[i].Pmax;
                    decimal nextPmin = GetNextPmin(gasPowerplant, i);
                    decimal powerNeeded = ComputeOptimalPower(current, load, pmin, pmax, nextPmin);
                    decimal oneMwh = ComputeOneMwhElectricity(gasPowerplant[i].Efficiency);
                    decimal realPower = oneMwh * powerNeeded;
                    decimal cost = (gas + (co2 * tonOfCo2PerMwh)) * realPower;

                    reply.P = powerNeeded;
                    current += powerNeeded;
                    reply.Cost = cost;
                }

                replies.Add(reply);
            }

            PowerTemplate powerTemplate = new PowerTemplate();
            powerTemplate.Power = current;
            powerTemplate.Replies = replies;

            return powerTemplate;
        }

        public PowerTemplate ComputeKerosine(IList<Powerplant> kerosinePowerplant, decimal kerosine, decimal current, decimal load)
        {
            ICollection<Reply> replies = new List<Reply>();

            for (int i = 0; i < kerosinePowerplant.Count; ++i)
            {
                Reply reply = new Reply();
                reply.Name = kerosinePowerplant[i].Name;

                if (!CheckLoadFull(current, load))
                {
                    decimal pmin = kerosinePowerplant[i].Pmin;
                    decimal pmax = kerosinePowerplant[i].Pmax;
                    decimal nextPmin = GetNextPmin(kerosinePowerplant, i);
                    decimal powerNeeded = ComputeOptimalPower(current, load, pmin, pmax, nextPmin);
                    decimal oneMwh = ComputeOneMwhElectricity(kerosinePowerplant[i].Efficiency);
                    decimal realPower = oneMwh * powerNeeded;
                    decimal cost = kerosine * realPower;

                    reply.P = powerNeeded;
                    current += powerNeeded;
                    reply.Cost = cost;
                }

                replies.Add(reply);
            }

            PowerTemplate powerTemplate = new PowerTemplate();
            powerTemplate.Power = current;
            powerTemplate.Replies = replies;

            return powerTemplate;
        }

        public decimal ComputeOneMwhElectricity(decimal efficiency)
        {
            return 1 / efficiency;
        }

        public decimal GetNextPmin(IList<Powerplant> powerplant, int i)
        {
            return i + 1 < powerplant.Count ? powerplant[i + 1].Pmin : 0;
        }

        public decimal ComputeOptimalPower(decimal current, decimal load, decimal pmin, decimal pmax, decimal nextPmin)
        {
            decimal powerNeeded = load - current;
            decimal powerNeededRemaining = load - current - nextPmin;
            return powerNeeded > pmax ? powerNeededRemaining > pmax ? pmax : powerNeededRemaining : powerNeeded < pmin ? 0 : powerNeeded;
        }

        public bool CheckLoadFull(decimal current, decimal load)
        {
            return current == load;
        }
    }
}