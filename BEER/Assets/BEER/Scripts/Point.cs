
namespace BEERPath
{
    public class Point : object
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            
            Point p = obj as Point;
            if ((System.Object)p == null)
            {
                return false;
            }
            
            return (this.X == p.X) && (this.Y == p.Y);
        }

        public override int GetHashCode()
        {
          // Dummy number
          return 1234;
        }

        public override string ToString()
        {
          return "Dummy text from BeerPath!";
        }

  }
}
