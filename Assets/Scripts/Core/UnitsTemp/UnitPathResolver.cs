using System.Collections.Generic;
using UnityEngine;

public class UnitPathResolver
{
    private readonly UnitPathing _unitPathing;

    public UnitPathResolver(UnitPathing unitPathing)
    {
        _unitPathing = unitPathing;
    }

    /// <summary>
    /// Computes a trimmable TileScript path from current to target tile
    ///
    /// Author: Nico
    ///
    /// Return semantics:
    /// - returns <b>false</b> if no path exists; <paramref name="path"/> is null.
    /// - returns <b>true</b> and <paramref name="path"/> == null if already in range.
    /// - returns <b>true</b> and <paramref name="path"/>.Count > 0 if movement path.
    /// </summary>
    /// <param name="current">
    /// Current TileScript position
    /// </param>
    /// <param name="target">
    /// Target TileScript position
    /// </param>
    /// <param name="rangeTrim">
    /// How many tiles away from target pathing should stop
    /// Must be >= 0, else bail and returns false
    /// </param>
    /// <param name="path">
    /// Computed movement path.  
    /// - null means "already in range".  
    /// - empty list means target was the start.  
    /// - non-empty list means movement path.
    /// </param>
    /// <returns>
    /// True if pathing succeeded (even if path out is null or empty)
    /// False if not path found or invalid params
    /// </returns>
    public bool TryGetPath(
        TileScript current,
        TileScript target,
        int rangeTrim,
        out List<TileScript> path)
    {
        path = null;
        
        // safety bail if current or target in null
        if (current == null || target == null)
        {
            Debug.LogError("[UnitPathResolver] current or target for path was null");
            return false;
        }
        
        // safety bail if rangeTrim < 0
        if (rangeTrim < 0)
        {
            Debug.LogError("[UnitPathResolver] rangeTrim requested was < 0");
            return false;
        }
        
        // set values in pathing algorithm
        _unitPathing.setPosition(current.x, current.y, current.z);
        _unitPathing.setTarget(target.x, target.y, target.z);
        
        // retrieve raw path from pathing algorithm
        List<int[]> rawPath = _unitPathing.findPath();
        
        // if the algorithm return null, or count is 0, bail
        if (rawPath == null || rawPath.Count == 0)
        {
            Debug.LogError("[UnitPathResolver] No path returned from UnitPathing");
            return false;
        }
        
        // create the final path
        List<TileScript> finalPath = new(rawPath.Count);
        
        // convert from raw coordinates into TileScript list
        foreach (int[] coords in rawPath)
        {
            GameObject hexObj = MapGenerateScript.getHex(coords[0], coords[1], coords[2]);
            if (hexObj != null && hexObj.TryGetComponent(out TileScript tile))
            {
                // skip the tile we are standing on
                if (tile == current)
                    continue;

                finalPath.Add(tile);
            }
        }

        // start = goal
        // unlikely, but technically a successful path result
        if (finalPath.Count == 0)
        {
            path = new List<TileScript>();
            return true;   
        }
        
        // trim path based on rangeTrim value
        int fullDistance = finalPath.Count;

        // path is already in range
        if (fullDistance <= rangeTrim)
        {
            // returns null, so the unit does not use the path
            // return true, so unit knows pathing was successful,
            // and you are in range of the requested trim
            path = null;
            return true;
        }

        // trim the path
        int stopIndex = fullDistance - 1 - rangeTrim;

        if (stopIndex < finalPath.Count - 1)
            finalPath.RemoveRange(stopIndex + 1, finalPath.Count - (stopIndex + 1));

        // return path and true
        path = finalPath;
        return true;
    }
}
