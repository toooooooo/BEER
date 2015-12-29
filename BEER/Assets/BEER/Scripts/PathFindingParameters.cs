using System;

namespace BEERPath
{
    public class PathFindingParameters
    {
        public Point StartLocation { get; set; }
        public Point EndLocation { get; set; }
        public bool[,] Map { get; set; }

        public Object[,] GameObjects { get; set; }

        public PathFindingParameters(Point startLocation, Point endLocation, bool[,] map, Object[,] gameObjects)
        {
            this.StartLocation = startLocation;
            this.EndLocation = endLocation;
            this.Map = map;
            this.GameObjects = gameObjects;
        }
    }
}
