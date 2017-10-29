using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class RoadMapGenerator {

    public enum Approach { NIELS, MASSI, ROBIN, RANDOM };

    private PriorityQueue<Edge> q; //contains edges still to be added
	public static List<Edge> roads {get; private set;}  //contains edges that are added
	public static List<Node> nodes {get; private set;} // contains nodes in the road map

    private GlobalGoals globalGoals;
    private LocalConstraints localConstraints;
	private RoadGenerator roadGenerator;
    private static bool roadBuilt = false;

    public RoadMapGenerator()
    {
        globalGoals = new GlobalGoals();
        localConstraints = new LocalConstraints();
		roadGenerator = new RoadGenerator ();
    }

    public void generateRoadMap() {
        resetRoads();

        // Generate the first roads 
		foreach (Edge road in globalGoals.generateFirstRoads(Approach.RANDOM))
        {
            q.push(road, road.getTime());
        }
        
        while (q.size > 0) {
            Edge roadToBuild = q.pop();		

            //check with localconstraints
            roadToBuild = localConstraints.validateRoad(roadToBuild);
			if (roadToBuild != null) {
				if(CityGeneratorUI.DebugMode)
					Debug.Log ("roadToBuild was valid");
				//add it to the roads array
				roads.Add (roadToBuild);
				//visualize it and give it the correct index
				RoadVisualizer.placeRoad (roadToBuild, (roads.Count - 1).ToString ());

				foreach (Edge road in globalGoals.generateNewRoads(roadToBuild)) {
					q.push (road, road.getTime ());
				}
			} else {
				if(CityGeneratorUI.DebugMode)
					Debug.Log ("roadToBuild was not valid");
			}
        }
   
        //construct the list of nodes with all their references
        constructNodeGraph();

		CityGenerator.nrOfRoads = roads.Count;

        PreviewRoads.NrRoads = roads.Count; //reset the nr of roads visualized
        roadBuilt = true;

    }

    //reset the roads
    private void resetRoads() {
        GameObject.DestroyImmediate(GameObject.Find("RoadMap"));
        GameObject.DestroyImmediate(GameObject.Find("Nodes"));
        GameObject.DestroyImmediate(GameObject.Find("Buildings"));
        GameObject.DestroyImmediate(GameObject.Find("RoadMeshes"));
        roads = new List<Edge>();
        q = new PriorityQueue<Edge>();
        roadBuilt = false;
    }

    /// <summary>
    /// Replaces the given road in the roads list with the given roads
    /// </summary>
    /// <param name="e">E.</param>
    /// <param name="roadsToAdd">Roads to add.</param>
    public static void replaceRoad(Edge e, List<Edge> roadsToAdd) {
        int roadIndex = 0;
        //remove this road by iterating backwards
        for (int i = roads.Count - 1; i >= 0; i--)
        {
            if (e.Equals(roads[i])) {
                roads.RemoveAt(i);
                roadIndex = i;
                break;
            }
        }

        //add the new roads
        roads.InsertRange(roadIndex, roadsToAdd);
    }

    /// <summary>
    /// Converts List of edges to connected Node graph.
    /// </summary>
    /// <param name="roads"></param>
    /// <returns></returns>
    private void constructNodeGraph()
    {
        // Create a node list where all nodes have references to the edges they are part of.
        nodes = new List<Node>();
        foreach (Edge e in roads)
        {
			//for N1
            Node n1 = e.n1;
            if (!nodes.Contains(n1))
            {
                nodes.Add(n1);
            }
            else
            {
                Node n_temp = nodes.Find(n => n.Equals(n1));
                n1 = n_temp;
            }
            if (!n1.edges.Contains(e))
            {
                n1.edges.Add(e);
            }

			//for N2
			Node n2 = e.n2;
			if (!nodes.Contains(n2))
			{
				nodes.Add(n2);
			}
			else
			{
				Node n_temp = nodes.Find(n => n.Equals(n2));
				n2 = n_temp;
			}
			if (!n2.edges.Contains(e))
			{
				n2.edges.Add(e);
			}
        }

		foreach (Node n in nodes) {
			//check the number of connected edges to determine the node type
			if (n.edges.Count == 1) {
				n.nodeType = NodeTypes.ROADEND;
			} else if (n.edges.Count == 2) {
				n.nodeType = NodeTypes.STRAIGHT;
			} else if (n.edges.Count > 2) {
				n.nodeType = NodeTypes.INTERSECTION;
			} else {
				Debug.LogError ("Node not connected to any edge");
			}
            // Check if the node is on a highway
            n.onHighway = false;
            foreach(Edge e in n.edges)
            {
                if (e.getRoadType() == RoadTypes.HIGHWAY)
                {
                    n.onHighway = true;
                    break;
                }
            }
		}
    }

	/// <summary>
	/// Given two endpoints, returns the road in the roads list that has this endpoint
	/// </summary>
	/// <returns>The road.</returns>
	/// <param name="n1">N1.</param>
	/// <param name="n2">N2.</param>
	public static Edge getRoad(Node n1, Node n2){
		foreach (Edge road in roads) {
			if (road.n1.Equals(n1) && road.n2.Equals(n2)) {
				return road;	
			}
		}

		Debug.LogError ("Road not found in roads list");
		return null;
	}

	//this makes sure the gizmo lines are drawn
	[DrawGizmo(GizmoType.NotInSelectionHierarchy)]
	static void drawRoadGizmo(GameObject terr, GizmoType gizmoType) {
		if (terr.name ==  "Terrain" && roadBuilt && CityGeneratorUI.DebugMode) {						
			// Draw gizmos...
			for (int i = 0; i < (int)PreviewRoads.NrRoads; i++) {
				if (roads [i].getRoadType () == RoadTypes.HIGHWAY) {
					Gizmos.color = Color.blue;
				} else {
					Gizmos.color = Color.yellow;
				}
				Vector3 n1 = new Vector3 (roads [i].n1.x, CoordinateHelper.worldToTerrainHeight(roads [i].n1) + 3, roads [i].n1.y);
				Vector3 n2 = new Vector3 (roads [i].n2.x, CoordinateHelper.worldToTerrainHeight(roads [i].n2) + 3, roads [i].n2.y);
				Gizmos.DrawLine (n1, n2);

				Gizmos.color = Color.green;
				Gizmos.DrawSphere (n1, 1);
				Gizmos.DrawSphere (n2, 1);
			}
		}

	}

    public List<Edge> getRoads()
    {
        return roads;
    }


	/// <summary>
	/// Gets the nodes of the road graph
	/// </summary>
	public List<Node> getNodes()
	{
		return nodes;
	}

	//TESTING
	public void test() {
		RoadVisualizer.placeRoad(new Edge(new Node(100, 100), new Node(125, 125), RoadTypes.HIGHWAY), "0");
		RoadVisualizer.placeRoad(new Edge(new Node(125, 125), new Node(180, 150), RoadTypes.HIGHWAY), "1");
		RoadVisualizer.placeRoad(PositionLegalizer.legalizeRoad(new Edge(new Node(180, 150), new Node(200, 250), RoadTypes.HIGHWAY)), "2");
		RoadVisualizer.placeRoad(new Edge(new Node(200, 250), new Node(200, 550), RoadTypes.HIGHWAY), "3");
		RoadVisualizer.placeRoad(new Edge(new Node(200, 550), new Node(200, 750), RoadTypes.HIGHWAY), "4");

		RoadVisualizer.placeRoad(new Edge(new Node(125, 125), new Node(125, 175), RoadTypes.STREET), "5");
		RoadVisualizer.placeRoad(new Edge(new Node(125, 175), new Node(80, 225), RoadTypes.STREET), "6");
		RoadVisualizer.placeRoad(new Edge(new Node(125, 175), new Node(180, 150), RoadTypes.STREET), "7");
	}		

	public void test3() {
		resetRoads();
	}

	public void test4() {
		IntersectionChecker i = new IntersectionChecker ();
		Edge road = new Edge (new Node (20, 155), new Node (75, 200), RoadTypes.STREET);
		i.fixRoad (road);
		RoadVisualizer.placeRoad (road, "0");

	}

	public void generateRoadMesh() {
        GameObject.Find("RoadMap").SetActive(false);
        GameObject.Find("Nodes").SetActive(false);
        PreviewRoads.NrRoads = 0;

        GameObject.DestroyImmediate(GameObject.Find("RoadMeshes"));

        //convert to mesh
        roadGenerator.generateRoadMeshNetwork (roads, nodes);

	}
	//TESTING
}
