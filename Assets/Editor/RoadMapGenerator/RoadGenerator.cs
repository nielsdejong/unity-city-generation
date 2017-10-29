using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class RoadGenerator
{
	//Objects needed to create the roadmesh
	GameObject roadMeshes;
	GameObject road;
	MeshFilter mf;
	MeshRenderer mr;
	Mesh mesh;

	//used for mesh generation
	List<Vector3> vertices;
	List<int> tri;
	List<Vector2> uv;

	public void generateRoadMeshNetwork(List<Edge> r, List<Node> n){
		//make sure we dont change the original road and nodes lists
		List<Edge> roads = new List<Edge>(r);
		List<Node> nodes = new List<Node>(n);

		List<List<Node>> highwayRoadPoints = new List<List<Node>> ();
		List<List<Node>> streetRoadPoints = new List<List<Node>> ();

		while (roads.Count != 0) {
			//get the first road from the list and remove it from the list
			Edge roadStart = roads [0];
			RoadTypes roadType = roadStart.getRoadType ();
			roads.RemoveAt (0);

			//set up roadPoints list
			List<Node> roadPoints = new List<Node> ();
			roadPoints.Add (roadStart.n1);
			roadPoints.Add (roadStart.n2);

			//now traverse the graph in both directions to find the successors/predecessors
			Edge nextSucc = findNext(roadStart, roadStart.n2, roads);

			while (nextSucc != null) {
				//find the node of nextSucc which is not in roadPoints yet
				Node nodeToInsert = findCorrectNode (roadPoints, nextSucc, nodes);

				/*if(nodeToInsert == null){
					Debug.Log ("nodeToInsert was null for " + nextSucc.ToString());
					Debug.Log ("roadPoints contains: [");
					foreach (Vector2 point in roadPoints) {
						Debug.Log (point+", ");
					}
					Debug.Log ("]");
				}*/

				//add the point to the end of roadPoints
				roadPoints.Add (nodeToInsert);

				//delete the edge from the roads list
				roads.Remove (nextSucc);

				nextSucc = findNext(nextSucc, nodeToInsert, roads);
			}

			//find the next predecessor by going back from N1
			Edge nextPred = findNext (roadStart, roadStart.n1, roads);

			//as long as we find new predecessors
			while (nextPred != null) {
				//find the node of nextPred which is not in roadPoints yet
				Node nodeToInsert = findCorrectNode (roadPoints, nextPred, nodes);

				//add the point to the beginning roadPoints
				roadPoints.Insert(0, nodeToInsert);

				//delete the edge from the roads list
				roads.Remove (nextPred);

				nextPred = findNext(nextPred, nodeToInsert, roads);
			}

			if (roadType == RoadTypes.HIGHWAY) {
				highwayRoadPoints.Add (roadPoints);
			} else {
				streetRoadPoints.Add (roadPoints);
			}
		}

		//now generate the highway meshes
		foreach(List<Node> points in highwayRoadPoints){
			generateRoadMesh (points.ToArray (), RoadTypes.HIGHWAY);
		}

		//fix the heightmap based on the highways
		fixHeightMap ();

		//now generate the street meshes
		foreach(List<Node> points in streetRoadPoints){
			generateRoadMesh (points.ToArray (), RoadTypes.STREET);
		}

		//fix the heightmap based on the streets
		fixHeightMap ();
	}


	/// <summary>
	/// Given the roadPoints, this method returns the node of the given edge which is not in roadPoints yet
	/// Note that we return the reference from the nodes array such that the node contains all required information
	/// </summary>
	/// <returns>The correct node.</returns>
	/// <param name="roadPoints">Road points.</param>
	/// <param name="e">E.</param>
	/// <param name="nodes">Nodes.</param>
	private Node findCorrectNode(List<Node> roadPoints, Edge e, List<Node> nodes){
		foreach (Node point in roadPoints) {
			//N1 is already in roadPoints, return N2
			if (Mathf.Approximately(point.x, e.n1.x) && Mathf.Approximately(point.y, e.n1.y)) {
				//Debug.Log ("N1 was found in nodes array for: " + e.ToString ());
				return nodes.Find (n => n.Equals (e.n2));
			}
			//N2 is already in roadPoints, return N1
			if (Mathf.Approximately(point.x, e.n2.x) && Mathf.Approximately(point.y, e.n2.y)) {
				//Debug.Log ("N2 was found in nodes array for: " + e.ToString ());
				return nodes.Find (n => n.Equals (e.n1));
			} 
		}

		Debug.LogError ("Both points of given edge were not in roadPoints yet!");
		return null;
	}

	/// <summary>
	/// Given a road and one of the endpoints of this road, find the next suitable road
	/// </summary>
	/// <returns>The next.</returns>
	/// <param name="fromRoad">From road.</param>
	/// <param name="fromNode">From node.</param>
	private Edge findNext(Edge fromRoad, Node fromNode, List<Edge> roads){
		//get the direction of this road, direction is always towards "fromNode"
		Vector2 roadDirection = (fromNode.pos - fromRoad.getOpposite (fromNode).pos);

		float smallestAngle = float.MaxValue;
		Edge nextRoad = null;

		//loop over the candidates
		foreach (Edge candidate in fromNode.edges) {
			//get the direction this road points in, this is always starting at "fromNode"
			Vector2 candidateDirection = (candidate.getOpposite (fromNode).pos - fromNode.pos);

			//make sure the road was not handled already
			//this also makes sure we do not find fromRoad itself
			if (roads.Contains (candidate)) {
				//make sure types are equal
				if (candidate.getRoadType () == fromRoad.getRoadType ()) {
					//we do not care about angles, just take this candidate
					if (fromNode.nodeType == NodeTypes.STRAIGHT) {
						nextRoad = candidate;
					} else {
						//check angles
						if (Vector2.Angle (roadDirection, candidateDirection) < CityGenerator.maxMeshGenerationAngle &&
						   Vector2.Angle (roadDirection, candidateDirection) < smallestAngle) {
							smallestAngle = Vector2.Angle (roadDirection, candidateDirection);
							nextRoad = candidate;
						}
					}
				}
			}
		}


		return nextRoad;
	}  

	/// <summary>
	/// Generates the road mesh. Takes an array of points (road nodes) and makes a smooth road based on these points
	/// </summary>
	/// <param name="roadPoints">Road points.</param>
	/// <param name="width">Width of the road</param>
	private void generateRoadMesh (Node[] roadPoints, RoadTypes type)
	{
		if (roadMeshes == null) {
			roadMeshes = new GameObject ();
			roadMeshes.name = "RoadMeshes";
		}

		road = new GameObject ();
		road.name = "Road";
		road.transform.parent = roadMeshes.transform;

		mf = road.AddComponent<MeshFilter> ();
		mr = road.AddComponent<MeshRenderer> ();

		mr.material = (Material)Resources.Load ("roadMaterial") as Material;

		mesh = new Mesh ();
		mesh.name = "Road";

		//generate the road using the smoothpoints
		generateRoad (smoothPoints (roadPoints), type, roadPoints[0], roadPoints[roadPoints.Length - 1]);
	}

	/// <summary>
	/// Generates the road mesh given an array of nodes along which the road should traverse
	/// </summary>
	/// <param name="nodes">Nodes along which the road traverses</param>
	/// <param name="width">Width of the road</param>
	/// <param name="startNode">Reference to the start node of this road</param> 
	/// <param name="endNode">Reference to the end node of this road</param>
	private void generateRoad (Node[] nodes, RoadTypes type, Node startNode, Node endNode)
	{				
		//set up arrays needed for mesh generation
		vertices = new List<Vector3> ();
		tri = new List<int> ();
		uv = new List<Vector2> ();

		//used to determine the correct UV height
		float uvHeight = 0.0f;
		bool up = true;

		//the point from which we "move" in the roads direction
		Vector2 origin = nodes[0].pos;

		//used to keep track of index of previous vertices in index array.
		int prevLeft = -1;
		int prevRight = -1;

		//highways are a bit higher s.t. they are always "above"
		float extraHeight = (type == RoadTypes.HIGHWAY) ? 0.02f : 0.01f;
		float width = (type == RoadTypes.HIGHWAY) ? (float)CityGenerator.highwayWidth : (float)CityGenerator.streetWidth;

		//we loop over the points (except the last one)
		for (int i = 0; i < nodes.Length - 1; i++) {			
			
			//now for each point we determin how many road "segments" we need to make
			Vector2 localDirection = (nodes[i+1].pos - origin);

			//we make roadsegments equal to the amount of units we have
			for (int j = 0; j < Mathf.RoundToInt (localDirection.magnitude); j++) {
				Vector2 leftPoint2D = origin + (new Vector2 (-localDirection.y, localDirection.x).normalized * (width / 2));
				Vector2 rightPoint2D = origin + (new Vector2 (localDirection.y, -localDirection.x).normalized * (width / 2));

				//find right and left point with respect to origin and update minHeight
				Vector3 leftPoint = new Vector3 (leftPoint2D.x, CoordinateHelper.getAccurateTerrainHeight (leftPoint2D.x, leftPoint2D.y) + extraHeight, leftPoint2D.y);
				Vector3 rightPoint = new Vector3 (rightPoint2D.x, CoordinateHelper.getAccurateTerrainHeight (rightPoint2D.x, rightPoint2D.y) + extraHeight, rightPoint2D.y);

                // Fixes for water
                if (CityGenerator.rWater)
                {
                    leftPoint.y = Mathf.Max(leftPoint.y, 1f);
                    rightPoint.y = Mathf.Max(rightPoint.y, 1f);
                }

                //if we have previous coordinates
                if (prevLeft >= 0 && prevRight >= 0) {
					int triSize = tri.Count;
					int vertSize = vertices.Count;
					//lower left triangle
					tri.Add (prevLeft);	//the previous left point
					tri.Add (vertSize); //the current left point
					tri.Add (prevRight); //the current right point

					//upper right triangle
					tri.Add (vertSize); //the current left point
					tri.Add (vertSize + 1); //the current right point
					tri.Add (prevRight); //the previous right point

					//we also make a box collider for this "segment"
					GameObject coll = new GameObject();
					coll.transform.parent = road.transform;
					coll.transform.localPosition = Vector3.Lerp (leftPoint, rightPoint, 0.5f);
					coll.layer = LayerMask.NameToLayer ("Road");

					BoxCollider bc = coll.AddComponent<BoxCollider> ();

					bc.size = new Vector3 (2.0f, 0.01f, width);

					//allign it with the road
					coll.transform.LookAt (rightPoint);
				} else {
					//prevLeft and prevRight are not set so do nothing here
				}

				if (up) {
					uvHeight += 0.1f;
					if (Mathf.Approximately (1.0f, uvHeight) || (uvHeight > 1)) {
						uvHeight = 1.0f;
						up = false;
					}
				} else {
					uvHeight -= 0.1f;
					if (Mathf.Approximately (0.0f, uvHeight) || (uvHeight < 0)) {
						uvHeight = 0.0f;
						up = true;
					}
				}

				//add the points to the vertex list
				vertices.Add (leftPoint);
				//and set the uv coordinates for leftPoint
				uv.Add (new Vector2 (0, uvHeight));

				//do the same for rightPoint
				vertices.Add (rightPoint);
				uv.Add (new Vector2 (1, uvHeight));

				//set the previous indices
				prevLeft = vertices.Count - 2;
				prevRight = vertices.Count - 1;

				//move 1 unit towards our "local goal"
				origin += localDirection.normalized;
			}				
		}

		//store these values
		int startLeftIndex = 0;
		int startRightIndex = 1;
		int endLeftIndex = vertices.Count - 2;
		int endRightIndex = vertices.Count - 1;

		//and these
		Vector3 startLeftVertex = vertices [startLeftIndex];
		Vector3 startRightVertex = vertices [startRightIndex];
		Vector3 endLeftVertex = vertices [endLeftIndex];
		Vector3 endRightVertex = vertices [endRightIndex];

		//now check the ends of the road mesh
		//the start of this road is an roadEnd
		if(startNode.nodeType == NodeTypes.ROADEND){
			//make a road end
			makeRoadEnd (startRightVertex, startLeftVertex, startRightIndex, startLeftIndex, type); 
		}

		if (endNode.nodeType == NodeTypes.ROADEND) {
			//make a road end		
			makeRoadEnd (endLeftVertex, endRightVertex, endLeftIndex, endRightIndex, type);
		}

		//finalize the mesh by setting the values
		mesh.vertices = vertices.ToArray ();
		mesh.triangles = tri.ToArray ();
		mesh.RecalculateNormals ();
		mesh.uv = uv.ToArray ();
		mf.mesh = mesh;
	}

	private void makeRoadEnd(Vector3 leftPoint, Vector3 rightPoint, int leftPointIndex, int rightPointIndex, RoadTypes roadType){
		//find point between leftPoint and rightPoint and add it to the mesh
		Vector3 middle = Vector3.Lerp (leftPoint, rightPoint, 0.5f);
		vertices.Add (middle);
		uv.Add (new Vector2 (0.5f, 0));
		//store the index
		int middleIndex = vertices.Count - 1;

		//direction of the road end
		Vector2 endDirection = new Vector2(rightPoint.x, rightPoint.z) - new Vector2(leftPoint.x, leftPoint.z); 
		//angle between roadend direction and xAxis
		float angleXAxis = Vector2.Angle (Vector2.right, endDirection);

		//if the vector points down we need to subtract the angle
		if (endDirection.y < 0 ) {
			angleXAxis = -angleXAxis;
		}

		int numPoints = 9; //number of points on circle
		List<Vector3> circlePoints = new List<Vector3>();
		Vector3 previousPoint = Vector3.zero;
		Vector3 newPoint = Vector3.zero;

		float radius = (roadType == RoadTypes.HIGHWAY) ? ((float)CityGenerator.highwayWidth / 2f) : ((float)CityGenerator.streetWidth / 2f);

		//generate points on a (half) circle
		for (int i = 0; i <= numPoints; i++) {
			//store previous point
			previousPoint = newPoint;

			//set up angle and find coordinates on circle
			float angle = ((float)(numPoints - i) * (180f / (float)numPoints)) + angleXAxis;
			float x = (float)(radius * Mathf.Cos(angle * Mathf.PI / 180F)) + middle.x;
			float z = (float)(radius * Mathf.Sin(angle * Mathf.PI / 180F)) + middle.z;
			float y = CoordinateHelper.getAccurateTerrainHeight (x, z);
            if (CityGenerator.rWater)
            {
                y = Mathf.Max(1f, y);
            }

			//define new point
			newPoint = new Vector3 (x, y, z);

			if (previousPoint != Vector3.zero) {
				circlePoints.Add (previousPoint);
				vertices.Add (previousPoint);
				uv.Add (new Vector2 (0, 0));
			}

			//add the point to the mesh
			circlePoints.Add(newPoint);
			vertices.Add (newPoint);
			uv.Add (new Vector2 (0, 0.1f));

			//now make triangles, we need at least two points
			if(i > 0){
				tri.Add (vertices.Count - 2);
				tri.Add (vertices.Count - 1);
				tri.Add (middleIndex);
			}

			//make colliders such that the terrain can be fixed
			GameObject collider = new GameObject ();
			collider.transform.parent = road.transform;
			collider.transform.position = middle;
			collider.layer = LayerMask.NameToLayer ("Road");
			collider.transform.LookAt (newPoint);

			//set up collider and its size
			BoxCollider bc = collider.AddComponent<BoxCollider> ();
			bc.size = new Vector3 (4f, 0.1f, radius * 2);
		}


	}

	[DrawGizmo(GizmoType.NotInSelectionHierarchy)]
	static void Test (GameObject go, GizmoType gizmoType){
		if (go.name == "CircleNode") {
			Gizmos.DrawSphere (go.transform.position, 1);
		}
			
	}

	/// <summary>
	/// Given an array of input points, this method returns a new array of points that represent a curve along the input points.
	/// </summary>
	/// <returns>A new array of vector 2 points (including the input points)</returns>
	/// <param name="arrayToCurve">Array to curve. Vector 2 points</param>
	/// Used: http://www.habrador.com/tutorials/interpolation/1-catmull-rom-splines/
	private Node[] smoothPoints (Node[] arrayToCurve)
	{

		if (CityGeneratorUI.DebugMode) {
			foreach (Node node in arrayToCurve) {
				GameObject n = GameObject.Instantiate ((GameObject)Resources.Load ("Node") as GameObject, new Vector3 (node.x, CoordinateHelper.getAccurateTerrainHeight (node.x, node.y), node.y), Quaternion.identity);
				n.name = "nonCurvedNode";
			}
		}	

		List<Node> smoothPoints = new List<Node> ();

		//iterate over input points except the last one (there is no segment leaving from the last point)
		for(int s = 0; s < arrayToCurve.Length - 1; s++) {			
			Node p0 = arrayToCurve[Mathf.Max(0, s - 1)];
			Node p1 = arrayToCurve [s];
			Node p2 = arrayToCurve [s + 1];
			Node p3 = arrayToCurve [Mathf.Min(arrayToCurve.Length - 1, s + 2)];

			//now we go from 0 to 1 (point p1 to point p2), the amount with wich we increase t deteremines the smoothness of the curve
			for (float t = 0f; t <= 1f; t += 0.1f) {
				//The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
				Vector2 a = 2f * p1.pos;
				Vector2 b = p2.pos - p0.pos;
				Vector2 c = 2f * p0.pos - 5f * p1.pos + 4f * p2.pos - p3.pos;
				Vector2 d = -p0.pos + 3f * p1.pos - 3f * p2.pos + p3.pos;

				//The cubic polynomial: a + b * t + c * t^2 + d * t^3
				Vector2 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));

				//TODO: FIX, the last smoothPoint stops too soon, because of this the roadEnds are not included
				//we wish to keep the old nodes such that we still have their edge list and nodeType
				Node newNode = arrayToCurve.ToList ().Find (n => (Mathf.Approximately(n.x, pos.x) && Mathf.Approximately(n.y, pos.y)));
				if (newNode == null) {
					//Debug.Log ("node did not exist yet, new pos is: " + pos);
					smoothPoints.Add (new Node (pos));
				} else {
					smoothPoints.Add (newNode);
				}

				if (CityGeneratorUI.DebugMode) {
					GameObject n = GameObject.Instantiate ((GameObject)Resources.Load ("NodeCurved") as GameObject, new Vector3 (pos.x, CoordinateHelper.getAccurateTerrainHeight (pos.x, pos.y), pos.y), Quaternion.identity);
					n.name = "curvedNode";	
				}
			}
		}

		return smoothPoints.ToArray();
	}

	/// <summary>
	/// Fixes the height map. By shooting rays up, checking collisions with roads. When a road is hit, the terrain heigth is changed
	/// </summary>
	private void fixHeightMap(){						
		int maxXmap = CityGenerator.terrain.terrainData.heightmapResolution;
		int maxYmap = CityGenerator.terrain.terrainData.heightmapResolution;

		//get the current heightmap values in the square we just defined
		float[,] heightMap = CityGenerator.terrain.terrainData.GetHeights (0, 0, maxXmap, maxYmap );

		//get terrain dimensions
		float minTerrainHeight = CityGenerator.terrain.transform.position.y;
		float maxTerrainHeight = CityGenerator.terrain.terrainData.size.y;

		//loop over these values 
		for(int x = 0; x < maxXmap; x++){
			for (int y = 0; y < maxYmap; y++) {
				//shoot a ray up to see how high the road lies
				Vector3 start = new Vector3(CoordinateHelper.heightmapXtoTerrainX (x), minTerrainHeight,CoordinateHelper.heightmapYtoTerrainZ (y));
				int layerMask = 1 << LayerMask.NameToLayer ("Road");	//we only want to collide with roads

				RaycastHit[] hits = Physics.RaycastAll (start, Vector3.up, maxTerrainHeight, layerMask);

				//if we hit a road segments, update the height
				if (hits.Length > 0) {
					//find the lowest hit
					float lowestHit = float.MaxValue;
					foreach(RaycastHit hit in hits){
						if (hit.point.y < lowestHit) {
							lowestHit = hit.point.y;
						}
					}

					//set the height
					float newHeight = ((lowestHit - CityGenerator.terrain.transform.position.y) 
                        / CityGenerator.terrain.terrainData.size.y ) - (0.25f / CityGenerator.terrain.terrainData.size.y);
					//the array is indexed as [y, x]
					heightMap [y, x] = Mathf.Clamp(newHeight, 0f, 1f);				
				}						
			}
		}

        //update the heightmap
        CityGenerator.terrain.terrainData.SetHeights(0, 0, heightMap);
	}
}
