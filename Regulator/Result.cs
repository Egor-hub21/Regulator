using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Regulator
{
    public struct Result(
        string name,
        double factor,
        (double, double)[] diagram,
        double openReserve,
        double lockedReserve
    )
    {
        public string Name { get; } = name;
        public double Factor { get; } = factor;
        public double OpenReserve { get; } = openReserve;
        public double LockedReserve { get; } = lockedReserve;
        public  DiagramPoint[] Diagram { get; } 
            = diagram
                .Select(r => new DiagramPoint(r.Item1, r.Item2))
                .ToArray();
    }
    public struct DiagramPoint
    {
        public double X { get; }
        public double Y { get; }

        public DiagramPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
