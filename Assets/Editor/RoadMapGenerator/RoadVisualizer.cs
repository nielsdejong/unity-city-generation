using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RoadVisualizer {
	//prefab of a node
	private static GameObject _nodeObj;
	private static GameObject nodeObj { 
		get {  
			if (_nodeObj == null) {
				_nodeObj = (GameObject)Resources.Load ("Node") as GameObject;
				if (_nodeObj == null) {
					Debug.LogError ("Node object not found");
				}
			}
			return _nodeObj;
		}
	}

	//prefab of a street
	private static GameObject _streetObj;
	private static GameObject streetObj { 
		get {  
			if (_streetObj == null) {
				_streetObj = (GameObject)Resources.Load ("Street") as GameObject;
				if (_streetObj == null) {
					Debug.LogError ("Street object not found");
				}
			}
			return _streetObj;
		}
	}

	//prefab of a highway
	private static GameObject _highwayObj;
	private static GameObject highwayObj { 
		get {  
			if (_highwayObj == null) {
				_highwayObj = (GameObject)Resources.Load ("Highway") as GameObject;
				if (_highwayObj == null) {
					Debug.LogError ("Highway object not found");
				}
			}
			return _highwayObj;
		}
	}

	public static GameObject roadMap;
	//gameobject in scene that holds all initialized game objects (roads)
	private static GameObject nodes;

	static Vector3 posN1;
	//position of node n1 (of the road to visualize)
	static Vector3 posN2;
	//position of node n2 (of the road to visualize)

	private static void initVars ()
	{

		roadMap = GameObject.Find ("RoadMap");
		if (roadMap == null) {
			//add it to the scene
			roadMap = new GameObject ();
			roadMap.name = "RoadMap";
		}

		nodes = GameObject.Find ("Nodes");
		if (nodes == null) {
			//add it to the scene
			nodes = new GameObject ();
			nodes.name = "Nodes";
		}
	
	}

	//visualizes a road on the terrain (no textures yet)
	public static void placeRoad (Edge road, string index)
	{
        if (roadMap == null || nodes == null)
        {
            initVars();
        }

        //set positions of endpoints
        posN1 = CoordinateHelper.nodeToTerrain(road.n1);
        posN2 = CoordinateHelper.nodeToTerrain(road.n2);

        //show the nodes
        showNode (posN1);
		showNode (posN2);

		float roadWidth = (road.getRoadType () == RoadTypes.HIGHWAY) ? CityGenerator.highwayWidth : CityGenerator.streetWidth;

		//show the road itself
		showEdge (road, index, roadWidth);
	}

	//shows the node and also returns its height
	private static void showNode (Vector3 pos)
	{		
		//check if a node already exists at this position	
		int layerMask = 1 << LayerMask.NameToLayer ("Node");
		float radius = (nodeObj.transform.localScale.x / 2) + 1;

		Collider[] collisions = Physics.OverlapSphere (pos, radius, layerMask);

		if (collisions.Length == 0) {
			GameObject.Instantiate (nodeObj, pos, Quaternion.identity, nodes.transform);
		}
	}

	private static void showEdge (Edge e, string i, float roadWidth)
	{		
		GameObject edge;
		if (e.getRoadType () == RoadTypes.STREET) {
			//instantiate the object
			edge = GameObject.Instantiate (streetObj, Vector3.Lerp (posN1, posN2, 0.5f), Quaternion.LookRotation (posN2 - posN1), roadMap.transform);
			edge.name = i + "_Street";
		} else {
			//instantiate the object
			edge = GameObject.Instantiate (highwayObj, Vector3.Lerp (posN1, posN2, 0.5f), Quaternion.LookRotation (posN2 - posN1), roadMap.transform);
			edge.name = i + "_Highway";
		}

		//add child gameobjects s.t. we remember the endpoints of the edge :)
		GameObject n1 = new GameObject ();
		n1.name = "N1_" + edge.name;
		n1.transform.SetParent(edge.transform);
		n1.transform.localPosition = posN1;

		GameObject n2 = new GameObject ();
		n2.name = "N2_"+ edge.name;
		n2.transform.SetParent (edge.transform);
		n2.transform.localPosition = posN2;

		//set the street length
		Vector3 scale = edge.transform.localScale;
		scale.z = (posN1 - posN2).magnitude;
		scale.x = roadWidth;
		edge.transform.localScale = scale;
	}		

	public static void replaceRoad(GameObject roadToReplace, List<Edge> replacementRoads){

		foreach (Edge e in replacementRoads) {
			placeRoad (e, roadToReplace.name);
		}

		GameObject.DestroyImmediate (roadToReplace);
	}


    /* OLD CODE USED TO PAINT ROADS ON TERRAIN
	 * 
	 * 
	//paints the roads on the terrain
	public void paintRoads ()
	{
		if (roadMap == null || nodes == null)
        {
            initVars();
        }
		initTextures ();	//calling this will automatically make everything grass
		Debug.Log ("Painting roads.. textures initialized");

		//first paint all the streets
		foreach (GameObject street in streets){
			paintRoad (street, "s");
		}

		//and then paint all the highways
		foreach (GameObject highway in highways) {
			paintRoad (highway, "h");
		}
	}

	private void paintRoad(GameObject road, string type){
		//paint the road yay!
		Debug.Log("Painting: " + road.name);
		Debug.Log ("Bounds: " + road.GetComponent<Renderer> ().bounds.size);
		Renderer roadRenderer = road.GetComponent<Renderer> ();

		//we define a the square in which this road gameObject lies
		int minX = Mathf.FloorToInt(road.transform.position.x - (roadRenderer.bounds.size.x / 2));
		int maxX = Mathf.CeilToInt(road.transform.position.x + (roadRenderer.bounds.size.x / 2));
		int minZ = Mathf.FloorToInt(road.transform.position.z - (roadRenderer.bounds.size.z / 2));
		int maxZ = Mathf.CeilToInt(road.transform.position.z + (roadRenderer.bounds.size.z / 2));
		Debug.Log ("minX: " + minX + " maxX: " + maxX + " minZ: " + minZ + " maxZ: " + maxZ);

		//this array covers the square, we will use it to set new texture values
		float[,,] newTextureData = terrain.terrainData.GetAlphamaps(minX, minZ, (maxX - minX) + 1, (maxZ - minZ) + 1);
		Debug.Log ("ARRAY has size x: " + newTextureData.GetLength(0) + " z: " + newTextureData.GetLength(1));

		//we set the bounds between which we shoot the rays
		float minHeight = terrain.transform.position.y;
		float maxHeight = terrain.terrainData.size.y;
		int layerMask = 1 << LayerMask.NameToLayer ("Edge");	//we only want to collide with edges


		for (int x = minX; x <= maxX; x++) {
			for (int z = minZ; z <= maxZ; z++) {				
				//not at each of these coordinates we shoot a ray up from the y-coordinate of the terrain. If a ray hits the road object, 
				//then we know we should paint a texture at that point
				if(Physics.Raycast(new Vector3(x, minHeight, z), Vector3.up, maxHeight, layerMask)){
					// Dont know why but for some reason we have to swap z and x.. but it works so.. yay!
					newTextureData [z - minZ, x - minX, 0] = 0;	//grass
					newTextureData [z - minZ, x - minX, 1] = (type == "h") ? 1 : 0;	//highway
					newTextureData [z - minZ, x - minX, 2] = (type == "h") ? 0 : 1;	//street
				} else {
					//do nothing (we wish to keep old values)
				}
			} 
		}

		//apply the change
		terrain.terrainData.SetAlphamaps (minX, minZ, newTextureData);
		road.SetActive (false); //disable the road s.t. we dont collide with it anymore
	}

	private void initTextures(){
		//remove the current textures
		terrain.terrainData.splatPrototypes = null;

		//this will contain the eventual textures
		SplatPrototype[] textureArray = new SplatPrototype[3];
		string[] textureNames = new string[] {
			"grassTexture",
			"highwayTexture",
			"streetTexture"
		};

		//Add a new SplatPrototype (= texture for terrain) for each texture
		for (int i = 0; i < 3; i++) {
			textureArray[i] = new SplatPrototype(); 
			textureArray[i].texture = (Texture2D)Resources.Load(textureNames[i],typeof(Texture2D));

			if (textureArray [i].texture == null) {
				Debug.LogError ("Texture not found");
			}

			textureArray[i].tileOffset = new Vector2(0, 0); 
			textureArray[i].tileSize = new Vector2(15, 15);
			textureArray[i].texture.Apply(true);
		}

		//set the new texture array
		terrain.terrainData.splatPrototypes = textureArray;
	}*/

}
