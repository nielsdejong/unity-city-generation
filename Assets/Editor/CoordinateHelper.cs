using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CoordinateHelper
{
	
    public static Vector3 getTerrainSize ()
	{
		return CityGenerator.terrain.terrainData.size;
	}

	/// <summary>
	/// Given a world coordinate, returns the 
	/// </summary>
	/// <returns>The to pop.</returns>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
    public static float worldToPop(float x, float y)
    {
		if (x < CityGenerator.terrain.terrainData.size.x && x >= 0 && y < CityGenerator.terrain.terrainData.size.z && y >= 0)
        {
			//terrain is always square
			float frac = (float)CityGenerator.popMap.GetLength(0) / (float) CityGenerator.terrain.terrainData.size.x;
			return CityGenerator.popMap[Mathf.FloorToInt(frac * x), Mathf.FloorToInt(frac * y)];
        }
        else
        {
			if(CityGeneratorUI.DebugMode)
				Debug.LogError ("Invalid coordinate: " + x + ", " + y);
            return 0f;
        }
    }

	// 0 = red, 1 = green, 2 = blue
	public static int worldToGrowth (float x, float y)
	{        
		if (x < CityGenerator.terrain.terrainData.size.x && x >= 0 && y < CityGenerator.terrain.terrainData.size.z && y >= 0)
        {
			//terrain is always square
			float frac = (float)CityGenerator.growthMap.GetLength(0) / (float) CityGenerator.terrain.terrainData.size.x;
            return (int)CityGenerator.growthMap[Mathf.FloorToInt(frac * x), Mathf.FloorToInt(frac * y)];
        }
        else
        {
			if(CityGeneratorUI.DebugMode)
				Debug.LogError ("Invalid coordinate: " + x + ", " + y);
            return 0;
        }
    }

	//Assumes terrain is at 0, 0, 0
	public static float worldToTerrainHeight (float x, float y)
	{
		float height = -10000;
		if (x < CityGenerator.terrain.terrainData.size.x && x >= 0 && y < CityGenerator.terrain.terrainData.size.z && y >= 0) {		
			//note that we should correct for the y value of the terrain	
			height = CityGenerator.terrain.SampleHeight (new Vector3 ((int)x, 0, (int)y))
				+ CityGenerator.terrain.transform.position.y;
		} else {
			if(CityGeneratorUI.DebugMode)
				Debug.LogError ("Invalid coordinate: x -> " + x + " y -> " + y);
		}
		return height;
	}

	public static float worldToTerrainHeight(Vector2 v){
		return worldToTerrainHeight (v.x, v.y);
	}

	//Assumes terrain is at 0, 0, 0
	public static float worldToTerrainHeight (Node n)
	{
		return worldToTerrainHeight (n.x, n.y);
	}

	/// <summary>
	/// Returns accurate height at position x,y (world coordinates)
	/// </summary>
	/// <returns>The accurate terrain height.</returns>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	public static float getAccurateTerrainHeight (float x, float y)
	{
        CityGenerator.terrain.gameObject.layer = LayerMask.NameToLayer ("Terrain");	//make sure terrain is at correct layer

		//we set the bounds between which we shoot the rays
		float minHeight = CityGenerator.terrain.transform.position.y - 0.5f;
		float maxHeight = CityGenerator.terrain.terrainData.size.y + 0.5f;	

		int layerMask = 1 << LayerMask.NameToLayer ("Terrain");	//we only want to collide with the terrain
		RaycastHit[] hits;
		hits = Physics.RaycastAll (new Vector3 (x, minHeight, y), Vector3.up, maxHeight, layerMask);

		if (hits.Length > 1) {
			if (CityGeneratorUI.DebugMode) {
				Debug.LogError ("Multiple hits! There are multiple terrains");
			}
			return 0;
		}else if (hits.Length == 0){
			if (CityGeneratorUI.DebugMode) {
				Debug.LogError ("No hits! The coordinate: " + x + ", " + y + " was invalid");
			}
			return 0;
		} else {
			//return the y value of the impact point
			return hits [0].point.y;
		}
	}

	public static Vector3 nodeToTerrain (Node n)
	{
		return new Vector3 (n.x, worldToTerrainHeight (n), n.y);
	}

	/// <summary>
	/// Returns a 2D position of an GO in 3D
	/// </summary>
	/// <param gameobject="go"></param>
	/// <returns>2D vector representing coordinates</returns>
	public static Vector2 threeDtoTwoD (GameObject go)
	{
		return threeDtoTwoD (go.transform.position);
	}

	public static Vector2 threeDtoTwoD (Vector3 v)
	{
		return new Vector2 (v.x, v.z);
	}

	public static bool areEqual (Vector3 v1, Vector3 v2)
	{
		return (Mathf.Approximately (v1.x, v2.x) && Mathf.Approximately (v1.y, v2.y) && Mathf.Approximately (v1.z, v2.z));
	}

	/// <summary>
	/// Given an x coordinate in terrain space, returns the corresponding x coordinate in the heightmap
	/// </summary>
	/// <returns>The x coordinate in the heightmap</returns>
	/// <param name="x">The x coordinate.</param>
	/// Assumes terrain is always at 0,y,0
	public static int terrainXtoHeightmapX (float x)
	{
		float terrainFrac = x / (float)CityGenerator.terrain.terrainData.bounds.size.x;
		if (terrainFrac > 1 || terrainFrac < 0) {Debug.LogError ("Invalid x coordinate");
		}

		return Mathf.RoundToInt (CityGenerator.terrain.terrainData.heightmapResolution * terrainFrac);
	}

	/// <summary>
	/// Given an z coordinate in terrain space, returns the corresponding z (y) coordinate in the heightmap
	/// </summary>
	/// <returns>The z (y) coordinate in the heightmap</returns>
	/// <param name="z">The z coordinate.</param>
	/// Assumes terrain is always at 0,y,0
	public static int terrainZtoHeightmapY (float z)
	{
		float terrainFrac = z / (float)CityGenerator.terrain.terrainData.bounds.size.z;
		if (terrainFrac > 1 || terrainFrac < 0) {
			if (CityGeneratorUI.DebugMode) Debug.LogError ("Invalid y coordinate");
		}

		return Mathf.RoundToInt (CityGenerator.terrain.terrainData.heightmapResolution * terrainFrac);
	}

	/// <summary>
	/// Given an x coordinate in heightmap space, returns the corresponding x coordinate in world (terrain) space
	/// </summary>
	/// <returns>The x coordinate in (terrain) world space</returns>
	/// <param name="x">The x coordinate.</param>
	/// Assumes terrain is always at 0,y,0
	public static float heightmapXtoTerrainX (int x)
	{
		float heightFrac = (float)x / (float)CityGenerator.terrain.terrainData.heightmapResolution;
		if (heightFrac > 1 || heightFrac < 0) {
			if (CityGeneratorUI.DebugMode) Debug.LogError ("Invalid x coordinate");
		}

		return (float)CityGenerator.terrain.terrainData.bounds.size.x * heightFrac;
	}

	/// <summary>
	/// Given an z (y) coordinate in heightmap space, returns the corresponding z coordinate in world (terrain) space
	/// </summary>
	/// <returns>The z coordinate in (terrain) world space</returns>
	/// <param name="z">The z coordinate.</param>
	/// Assumes terrain is always at 0,y,0
	public static float heightmapYtoTerrainZ (int y)
	{
		float heightFrac = (float)y / (float)CityGenerator.terrain.terrainData.heightmapResolution;
		if (heightFrac > 1 || heightFrac < 0) {
			if (CityGeneratorUI.DebugMode) Debug.LogError ("Invalid y coordinate");
		}

		return (float)CityGenerator.terrain.terrainData.bounds.size.z * heightFrac;
	}

	public static bool validEndPoint(Node n)
	{
		return validEndPoint(new Vector2(n.x, n.y));
	}

	public static bool validEndPoint(Vector2 v){
		float offset = (CityGenerator.highwayWidth / 2) + 1f; //compensate a bit for diagonal roads

		return (v.x >= 0 + offset && v.x < CityGenerator.terrain.terrainData.size.x - offset
			&& v.y >= 0 + offset && v.y < CityGenerator.terrain.terrainData.size.z - offset);
	}

    public static bool validRoadLength(Edge e)
    {
        if (e.getRoadType() == RoadTypes.HIGHWAY)
        {
            return ((e.n1.pos - e.n2.pos).magnitude >= CityGenerator.highwayMinLength);
        }
        else
        {
            return ((e.n1.pos - e.n2.pos).magnitude >= CityGenerator.streetMinLength);
        }
    }
}
