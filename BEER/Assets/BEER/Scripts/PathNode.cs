using System;

namespace BEERPath
{
    public enum PathNodeState
    {
        Possible, // possible step
        Included, // already in a path
        NotTested // not yet tested to be possible or not
    }
    public class PathNode
    {
        private PathNode parent;
        public Point Location { get; private set; }
        public bool Walkable { get; set; }
        public Object GameObject { get; private set; }

        public float CostFromStart { get; private set; }
        public float CostToEnd { get; private set; }

        public float CostTotal
        {
            get { return this.CostFromStart + this.CostToEnd; }
        }

        public PathNode ParentNode
        {
            get { return this.parent; }
            set
            {
                this.parent = value;
                this.CostFromStart = this.parent.CostFromStart + GetCost(this.Location, this.parent.Location);
            }
        }

        internal static float GetCost(Point source, Point destination)
        {
            float deltaX = destination.X - source.X;
            float deltaY = destination.Y - source.Y;

            return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public PathNodeState State { get; set; }


        public PathNode(int x, int y, bool walkable, Point destination, Object gameObject)
        {
            this.Location = new Point(x, y);
            this.State = PathNodeState.NotTested;
            this.Walkable = walkable;
            this.CostToEnd = GetCost(this.Location, destination);
            this.CostFromStart = 0;
            this.GameObject = gameObject;
        }
    }
}
