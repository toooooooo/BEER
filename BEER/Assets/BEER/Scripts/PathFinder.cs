using System.Collections.Generic;
using System.Collections;
using TDTK;
using UnityEngine;

namespace BEERPath
{
  public class PathFinder
  {
    private int width;
    private int heigth;
    private PathNode[,] nodes;
    private PathNode startNode;
    private PathNode destinationNode;
    private PathFindingParameters parameters;

    public PathFinder(PathFindingParameters parameters)
    {
      this.parameters = parameters;
      InitNodes(parameters.Map);

      this.startNode = this.nodes[parameters.StartLocation.X, parameters.StartLocation.Y];
      this.destinationNode = this.nodes[parameters.EndLocation.X, parameters.EndLocation.Y];

      this.startNode.State = PathNodeState.Possible;
    }

    private void InitNodes(bool[,] map)
    {
      this.width = map.GetLength(0);
      this.heigth = map.GetLength(1);
      this.nodes = new PathNode[this.width, this.heigth];

      for (int y = 0; y < this.heigth; y++)
      {
        for (int x = 0; x < this.width; x++)
        {
          this.nodes[x, y] = new PathNode(x, y, map[x, y], parameters.EndLocation, parameters.GameObjects[x, y]);
        }
      }
    }


    private List<PathNode> GetWalkableNodes(PathNode currentNode)
    {
      List<PathNode> walkables = new List<PathNode>();
      IEnumerable<Point> nextLocations;

      if (currentNode.Location.Y == 1)
      {
        nextLocations = new Point[] { new Point(destinationNode.Location.X, destinationNode.Location.Y) };
      }
      else
      {
        nextLocations = GetAdjacentLocations(currentNode.Location);
      }

      foreach (Point location in nextLocations)
      {
        int x = location.X;
        int y = location.Y;

        if (x < 0 || x >= this.width || y < 0 || y >= this.heigth)
          continue;


        PathNode node = this.nodes[x, y];


        // check if tower is not walkable or already in the path
        if (!node.Walkable || node.State == PathNodeState.Included)
          continue;

        // Check if path goes between 2 towers
        if (x != currentNode.Location.X && y != currentNode.Location.Y && y != 0)
        {
          if (!this.nodes[x, currentNode.Location.Y].Walkable || !this.nodes[currentNode.Location.X, y].Walkable)
            continue;
        }

        if (node.State == PathNodeState.Possible)
        {
          float cost = PathNode.GetCost(node.Location, node.ParentNode.Location);
          float tmp = currentNode.CostFromStart + cost;

          // Nodes are only added to the list if their cost is lower going via this route
          if (tmp < node.CostFromStart)
          {
            node.ParentNode = currentNode;
            walkables.Add(node);
          }
        }
        else
        {
          // Untested node -> set parent and consider it for the path
          node.ParentNode = currentNode;
          node.State = PathNodeState.Possible;
          walkables.Add(node);
        }
      }

      return walkables;
    }
    private static Point[] GetAdjacentLocations(Point location)
    {
      return new Point[]
      {
                new Point(location.X-1, location.Y-1),
                new Point(location.X-1, location.Y  ),
                new Point(location.X-1, location.Y+1),
                new Point(location.X,   location.Y+1),
                new Point(location.X+1, location.Y+1),
                new Point(location.X+1, location.Y  ),
                new Point(location.X+1, location.Y-1),
                new Point(location.X,   location.Y-1)
      };
    }

    private bool SearchPath(PathNode currentNode)
    {
      currentNode.State = PathNodeState.Included;
      List<PathNode> nextNodes = GetWalkableNodes(currentNode);

      nextNodes.Sort((node1, node2) => node1.CostTotal.CompareTo(node2.CostTotal));

      foreach (PathNode nextNode in nextNodes)
      {
        if (nextNode.Location == this.destinationNode.Location)
        {
          return true;
        }
        else
        {
          if (SearchPath(nextNode))
            return true;
        }
      }

      // no path found
      return false;
    }

    public List<PathNode> FindPath()
    {
      List<PathNode> path = new List<PathNode>();
      bool success = SearchPath(this.startNode);

      if (success)
      {
        PathNode node = this.destinationNode;

        while (node.ParentNode != null)
        {
          path.Add(node);
          node = node.ParentNode;
        }

        path.Reverse();
      }

      return path;
    }

    public PathTD FindPathTD(Transform currentPos, string pathName)
    {
      List<Transform> path = new List<Transform>();
      bool success = SearchPath(this.startNode);


      if (success)
      {
        PathNode node = this.destinationNode;
        GameObject platform = null;

        while (node.ParentNode != null)
        {
          platform = node.GameObject as GameObject;
          path.Add(platform.transform);
          node = node.ParentNode;
        }

        path.Add(currentPos);

        path.Reverse();
      }

      // TODO better solution
      // PathTD works only in an parent GameObject --> hard to destroy afterwards
      GameObject go = new GameObject(pathName);

      //public GameObject PathIndicator1;
      //GameObject Indicator1;
      //Indicator1 = Instantiate(PathIndicator1, new Vector3(0,0,0), Quaternion.identity);
      //Instantiate(Indicator1, new Vector3(0, 0, 0), Quaternion.identity);

      go.AddComponent<PathTD>();
      PathTD ret = go.GetComponent<PathTD>();
      ret.dynamicOffset = 0.2f;
      ret.createPathLine = false;
      ret.wpList = path;
      ret.Init();

      return ret;
    }
  }
}
