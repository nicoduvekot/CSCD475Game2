using UnityEngine;
using System.Threading.Tasks; //used for the await testing functionality
using System.Collections.Generic; //used for the imports for lists

public class UnitPathing : MonoBehaviour
{
    [SerializeField] private int[] position = new int[3];  // cube coords
    [SerializeField] private int[] target = new int[3];    // cube coords

    private static readonly int[][] directionVectors = new int[][]
    {
        new int[] { 1, 0, -1 },
        new int[] { 1, 1, 0 },
        new int[] { 0, 1, 1 },
        new int[] { -1, 0, 1 },
        new int[] { -1, -1, 0 },
        new int[] { 0, -1, -1 }
    };

    // sets the position of the start point
    public void setPosition(int x, int y, int z)
    {
        position = new int[3] { x, y, z };
    }

    // sets the target for the travel, does not initiate the travel
    public void setTarget(int x, int y, int z)
    {
        target = new int[3] { x, y, z };
    }

    // Gets the neighbor for any given pos
    public static int[] hexNeighbor(int[] pos, int direction)
    {
        int[] dir = hexDirection(direction);
        return new int[] { pos[0] + dir[0], pos[1] + dir[1], pos[2] + dir[2] };
    }

    public static int getDirection(int[] cur, int[] nextPos){
        int[] diff = {nextPos[0] - cur[0],nextPos[1] - cur[1],nextPos[2] - cur[2]};
        if(diff[0] == 1 && diff[2] == -1){
            return 1;
        }else if(diff[0] == 1 && diff[1] == 1){
            return 2;
        }else if(diff[1] == 1 && diff[2] == 1){
            return 3;
        }else if(diff[0] == -1 && diff[2] == 1){
            return 4;
        }else if(diff[0] == -1 && diff[1] == -1){
            return 5;
        }else if(diff[1] == -1 && diff[2] == -1){
            return 6;
        }else{
            print("invalid getDirection");
            return -1;
        }
    }

    // Return direction vector
    private static int[] hexDirection(int direction)
    {
        if (direction < 1 || direction > 6)
        {
            Debug.LogError("Direction must be 1�6");
            return new int[] { 0, 0, 0 };
        }

        return directionVectors[direction - 1];
    }

    // Cube distance between current position and target
    private int hexDistance(int[] a, int[] b)
    {
        return (Mathf.Abs(a[0] - b[0]) + Mathf.Abs(a[1] - b[1]) + Mathf.Abs(a[2] - b[2])) / 2;
    }

    //helper method if pos and target are already set
    public List<int[]> findPath()
    {
        return findPath(position, target);
    }

    // finds the path for the unit
    // start array sends the coordinates for the starting hex
    // goal array sends the coordinates for the ending hex
    // 
    // returns the list of coordinates for the hexs to travel to including the srating hex to the ending hex
    public List<int[]> findPath(int[] start, int[] goal)
    {
        // Priority queue for frontier
        var frontier = new simplePriorityQueue<int[]>();
        frontier.enqueue(start, 0);

        var cameFrom = new Dictionary<string, int[]>();
        var costSoFar = new Dictionary<string, int>();

        string StartKey = key(start);
        string GoalKey = key(goal);

        cameFrom[StartKey] = null;
        costSoFar[StartKey] = 0;

        while (frontier.Count > 0)
        {
            int[] current = frontier.dequeue();
            string currentKey = key(current);

            if (currentKey == GoalKey)
                break;

            // Loop through 6 hex neighbors
            for (int dir = 1; dir <= 6; dir++)
            {
                int[] next = hexNeighbor(current, dir);
                string nextKey = key(next);

                // This is the version currently being used
                //if (MapGenerateScript.getHex(next[0], next[1], next[2]) == null || MapGenerateScript.getHex(next[0], next[1], next[2]).GetComponent<TileScript>().getMovement() < 0) continue;
                
                TileScript nextTile = MapGenerateScript.getHex(next[0], next[1], next[2])?.GetComponent<TileScript>();
                
                // Skip null tiles
                if (nextTile == null)
                    continue;
                
                // Added: if this is en-route tile, and it is occupied, skip it
                if (nextKey != GoalKey && nextTile.OccupyingUnit != null)
                    continue;
                
                // if movement less than 0 skip it
                if (nextTile.getMovement() < 0)
                    continue;

                int newCost = costSoFar[currentKey] + nextTile.getMovement();

                if (!costSoFar.ContainsKey(nextKey) || newCost < costSoFar[nextKey])
                {
                    costSoFar[nextKey] = newCost;
                    int priority = newCost + hexDistance(goal, next);
                    frontier.enqueue(next, priority);
                    cameFrom[nextKey] = current;
                }
            }
        }

        // Reconstruct path
        return reconstructPath(cameFrom, start, goal);
    }

    //helper method for directory setup
    private string key(int[] c)
    {
        return $"{c[0]},{c[1]},{c[2]}";
    }

    //reconstructs the path for the goal
    private List<int[]> reconstructPath(Dictionary<string, int[]> cameFrom, int[] start, int[] goal)
    {
        List<int[]> path = new List<int[]>();

        string currentKey = key(goal);

        if (!cameFrom.ContainsKey(currentKey))
            return path; // no path found

        int[] current = goal;

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[key(current)];
        }

        path.Reverse();
        return path;
    }

    //simple queue for the code to work best
    public class simplePriorityQueue<T>
    {
        private List<(T item, int priority)> elements = new List<(T, int)>();

        public int Count => elements.Count;

        public void enqueue(T item, int priority)
        {
            elements.Add((item, priority));
        }

        public T dequeue()
        {
            int bestIndex = 0;

            for (int i = 1; i < elements.Count; i++)
            {
                if (elements[i].priority < elements[bestIndex].priority)
                    bestIndex = i;
            }

            T bestItem = elements[bestIndex].item;
            elements.RemoveAt(bestIndex);
            return bestItem;
        }
    }

    //used for testing
    //public bool debugToTarget()
    //{
    //    List<int[]> path = findPath(position, target);
        
    //    //this code needs to be changed to fit the acurate movement script
    //    for(int i = 0;  i < path.Count; i++)
    //    {
    //        int[] temp = path[i];

    //        Debug.Log("The coordintes for step " + i + " is " + temp[0] + "," + temp[1] + "," + temp[2]);
    //    }

    //    return true;
    //}
}