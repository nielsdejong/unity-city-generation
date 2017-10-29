using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GlobalGoals {
    
    // Current Growth Rule
    private GrowthRule currentRule;

    // All growth rules used for the global goals.
    private GrowthRule newYorkRule;
    private GrowthRule parisRule;
    private GrowthRule basicRule;

    /// <summary>
    /// Used to distinguish between different types of branching.
    /// </summary>
    private enum BranchType { HIGHWAY_TO_HIGHWAY, STREET_TO_STREET, ANY_ROAD_TO_STREET, STREET_TO_HIGHWAY}

    /// <summary>
    /// Initializes the GlobalGoals component with all its growth rules.
    /// </summary>
    /// <param name="seed">Seed for the random number generator.</param>
    public GlobalGoals()
    {
        // we can't use random.range nor random.initstate here! just putting random numbers as seeds for now
        newYorkRule = new NewYorkRule(42); 
        parisRule = new ParisRule(52452);
        basicRule = new BasicRule(423);
    }

	//generate the first roads
	public List<Edge> generateFirstRoads (RoadMapGenerator.Approach approach){
        List<Edge> firstRoads;
        Vector2 peakVector, initialEdgeVector, oppositeEdgeVector;
        Node peakNode, initialEdgeNode, oppositeEdgeNode;
        Edge initialEdge, oppositeEdge;
		List<Vector2> densityPeaks = CityGenerator.densityPeaks;

        switch (approach)
        {
		case RoadMapGenerator.Approach.RANDOM:
			Node startNode = null;
			while (startNode == null) {
				startNode = new Node (Random.Range (10, CityGenerator.terrainSize - 10), Random.Range (10, CityGenerator.terrainSize - 10));
				if (PositionLegalizer.isNodeUnderWater (startNode))
					startNode = null;
			}

			firstRoads = new List<Edge> ();

			Vector2 startLocation = new Vector2 (startNode.x, startNode.y);
			initialEdgeVector = new Vector2 (CityGenerator.highwayMinLength, 0);
			initialEdgeVector = Quaternion.Euler (0, 0, Random.value * 360f) * initialEdgeVector;

                // Generate an opposite edge vector
			oppositeEdgeVector = Quaternion.Euler (0, 0, 180f) * initialEdgeVector;

                // Build initial edge
			initialEdgeVector += startLocation;
			initialEdgeNode = new Node (initialEdgeVector);
			initialEdge = new Edge (startNode, initialEdgeNode, RoadTypes.HIGHWAY);
			firstRoads.Add (initialEdge);

                // Build opposite edge
			oppositeEdgeVector += startLocation;
			oppositeEdgeNode = new Node (oppositeEdgeVector);
			oppositeEdge = new Edge (startNode, oppositeEdgeNode, RoadTypes.HIGHWAY);
			firstRoads.Add (oppositeEdge);

            return firstRoads;

            // MASSI APPROACH: a highway per peak
			case RoadMapGenerator.Approach.MASSI:
			
				if (CityGeneratorUI.DebugMode)
					Debug.Log ("Peaks: " + densityPeaks.Count);

				if (densityPeaks != null) {
					firstRoads = new List<Edge> ();

					foreach (Vector2 peakLocation in densityPeaks) {
						peakNode = new Node (peakLocation);

						initialEdgeVector = new Vector2 (CityGenerator.highwayMinLength, 0);
						initialEdgeVector = Quaternion.Euler (0, 0, Random.value * 360f) * initialEdgeVector;

						// Generate an opposite edge vector
						oppositeEdgeVector = Quaternion.Euler(0, 0, 180f) * initialEdgeVector;

						// Build initial edge
						initialEdgeVector += peakLocation;
						initialEdgeNode = new Node (initialEdgeVector);
						initialEdge = new Edge (peakNode, initialEdgeNode, RoadTypes.HIGHWAY);
						firstRoads.Add (initialEdge);

						// Build opposite edge
						oppositeEdgeVector += peakLocation;
						oppositeEdgeNode = new Node (oppositeEdgeVector);
						oppositeEdge = new Edge (peakNode, oppositeEdgeNode, RoadTypes.HIGHWAY);
						firstRoads.Add (oppositeEdge);
					}
					return firstRoads;
				} else {
					return null;
				}		

			// ROBIN APPROACH: a single highway on one of the peaks
			case RoadMapGenerator.Approach.ROBIN:				
				if (CityGeneratorUI.DebugMode)
					Debug.Log ("Peaks: " + densityPeaks.Count);

				if (densityPeaks != null) {
					firstRoads = new List<Edge> ();

					//take a random peak
					Vector2 randomPeak = densityPeaks [Random.Range (0, densityPeaks.Count)];
					peakNode = new Node (randomPeak);
					
				    initialEdgeVector = new Vector2 (CityGenerator.highwayMinLength, 0);
					initialEdgeVector = Quaternion.Euler (0, 0, Random.value * 360f) * initialEdgeVector;
					// Generate an opposite edge vector
					oppositeEdgeVector = -initialEdgeVector;

					// Build initial edge
					initialEdgeVector += randomPeak;
					initialEdgeNode = new Node (initialEdgeVector);
					initialEdge = new Edge (peakNode, initialEdgeNode, RoadTypes.HIGHWAY);
					firstRoads.Add (initialEdge);

					// Build opposite edge
					oppositeEdgeVector += randomPeak;
					oppositeEdgeNode = new Node (oppositeEdgeVector);
					oppositeEdge = new Edge (peakNode, oppositeEdgeNode, RoadTypes.HIGHWAY);	
					firstRoads.Add (oppositeEdge);

					Debug.Log ("initial edge: " + initialEdge + " oppositeEdge: " + oppositeEdge);

					return firstRoads;
				} else {
					return null;
				}
				

            // NEILS' APPROACH: only one highway starting from the first peak
            case RoadMapGenerator.Approach.NIELS:
                firstRoads = new List<Edge>();

                float xCoordinate = -1f;
                float yCoordinate = -1f;
                float maxPopulation = float.NegativeInfinity;
                for (int x = 0; x < CoordinateHelper.getTerrainSize().x; x++)
                {
                    for (int y = 0; y < CoordinateHelper.getTerrainSize().z; y++)
                    {
                        float population = CoordinateHelper.worldToPop(x, y);
                        if (population > maxPopulation)
                        {
                            maxPopulation = population;
                            xCoordinate = x;
                            yCoordinate = y;
                        }
                    }
                }

                peakVector = new Vector2(xCoordinate, yCoordinate);
                peakNode = new Node(peakVector);

                // Generate an initial edge vector with config length... 
                initialEdgeVector = new Vector2(CityGenerator.highwayMinLength, 0);

                // ... and rotate it randomly
                initialEdgeVector = Quaternion.Euler(0, 0, Random.value * 360f) * initialEdgeVector;

                // Generate an opposite edge vector
                oppositeEdgeVector = Quaternion.Euler(0, 0, 180f) * initialEdgeVector;

                // Build initial edge
                initialEdgeVector += peakVector;
                initialEdgeNode = new Node(initialEdgeVector);
                initialEdge = new Edge(peakNode, initialEdgeNode, RoadTypes.HIGHWAY);
                firstRoads.Add(initialEdge);

                // Build opposite edge
                oppositeEdgeVector += peakVector;
                oppositeEdgeNode = new Node(oppositeEdgeVector);
                oppositeEdge = new Edge(peakNode, oppositeEdgeNode, RoadTypes.HIGHWAY);
                firstRoads.Add(oppositeEdge);

                return firstRoads;

            default:
                return null;

        }
        
    }

    /// <summary>
    /// Generate new roads based on an existing one.
    /// </summary>
    /// <param name="oldRoad">Predecessor road</param>
    /// <returns>A list of new roads</returns>
    public List<Edge> generateNewRoads(Edge oldRoad)
    {
        List<Edge> newBranches = new List<Edge>();

        // Get the growth rule we are used based on the old edge's end point
        int growthRule = CoordinateHelper.worldToGrowth(oldRoad.n2.x, oldRoad.n2.y);
        switch (growthRule)
        {
            case GrowthRuleGenerator.red:
                currentRule = basicRule;
                break;
            case GrowthRuleGenerator.green:
                currentRule = newYorkRule;
                break;
            case GrowthRuleGenerator.blue:
                currentRule = parisRule;
                break;
            default:
                Debug.LogError("Invalid Growth Rule");
                break;
        }
        
        newBranches.AddRange(generateBranches((oldRoad.getRoadType() == RoadTypes.HIGHWAY)? BranchType.HIGHWAY_TO_HIGHWAY : BranchType.STREET_TO_STREET, oldRoad));
        newBranches.AddRange(generateBranches(BranchType.ANY_ROAD_TO_STREET, oldRoad));

        return newBranches;
    }

    /// <summary>
    /// Generates branches based on the current growth rule.
    /// </summary>
    /// <param name="branchType">Type of branching to be done.</param>
    /// <param name="startNode">Old node</param>
    /// <param name="oldDirection">Direction of old road fragment</param>
    /// <returns></returns>
    private List<Edge> generateBranches(BranchType branchType, Edge oldEdge)
    {
        List<Edge> branches = new List<Edge>();
        switch (branchType)
        {
            case BranchType.HIGHWAY_TO_HIGHWAY:
                currentRule.branchHighwayToHighway(ref branches, oldEdge);
                break;
            case BranchType.STREET_TO_STREET:
                currentRule.branchStreetToStreet(ref branches, oldEdge);
                break;
            case BranchType.ANY_ROAD_TO_STREET:
                currentRule.branchRoadToStreet(ref branches, oldEdge);
                break;
        }
        return branches;
    }
}
