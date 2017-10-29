using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PositionLegalizer {
      
    private static Node start;
    private static Node end;
	private static Vector2 originalDirection;
	private static float originalLength;

    /// <summary>
    /// Returns a valid road with respect to environmental constraints. If it is not possible to adjust the road in input it simply returns a null object.
    /// </summary>
    /// <param name="road"></param>
    /// <returns></returns>
    public static Edge legalizeRoad(Edge road) {

        start = road.n1;        
        end = road.n2;

		//get original direction and length
		originalDirection = road.getDirection().normalized;
		originalLength = (road.n2.pos - road.n1.pos).magnitude;		        

        if (areNodesOK(start, end, road.getRoadType()))
        {
            return road;
        } else {			
            int attempt = 0;
            do {
                end = getNewEnd();
                attempt++;
            }
			while (!areNodesOK(start, end, road.getRoadType()) && attempt < CityGenerator.legalizationAttempts);

			if (attempt == CityGenerator.legalizationAttempts) 
			{ 
				if (CityGeneratorUI.DebugMode) Debug.Log("Road cannot be placed"); 
				return null; 
			}
            else { 
				return new Edge(start, end, road.getRoadType()); 
			}
        }     
    }

    /// <summary>
    /// Returns a boolean that indicates whether the input node is placed below a "Water" GameObject.
    /// </summary>
    /// <param name="n">Input node.</param>
    /// <returns></returns>
    public static bool isNodeUnderWater(Node n) {
        // if there is water we check
       if (CityGenerator.water != null) {
            float waterHeight = CityGenerator.water.transform.position.y;
            float nodeHeight = CoordinateHelper.worldToTerrainHeight(n);
            
            return nodeHeight < waterHeight;
        } else {
            // otherwise there is no water
            return false;
        }
    }

    /// <summary>
    /// Returns a boolean that tells if the two endpoints in input are valid endpoints for a road.
    /// </summary>
    /// <param name="n1"></param>
    /// <param name="n2"></param>
    /// <returns></returns>
    private static bool areNodesOK(Node n1, Node n2, RoadTypes roadType) {
		float distance = (n1.pos - n2.pos).magnitude; 
        float roadSlope = (float)Mathf.Abs(CoordinateHelper.worldToTerrainHeight(n2) - CoordinateHelper.worldToTerrainHeight(n1)) / (float)distance;
        float maxSlope = (roadType == RoadTypes.HIGHWAY) ? CityGenerator.highwayMaxSlope : CityGenerator.streetMaxSlope;
		return (roadSlope <= maxSlope) && !isNodeUnderWater(n2) && CoordinateHelper.validEndPoint(n2);
    }

    /// <summary>
    /// <para>Returns a road with random length and random angle.</para>
    /// <para> ! Length is at most a half of the original road. </para>
    /// <para> ! Angle is at most 30° (left-right) with respect to the original road direction. </para>
    /// </summary>
    /// <returns></returns>
    private static Node getNewEnd() {   
		//find the new direction and new endpoint
		Vector2 newDirection = Quaternion.AngleAxis (Random.Range(-CityGenerator.maxLigalizationAngle, CityGenerator.maxLigalizationAngle), Vector3.forward) * originalDirection;
		Vector2 newEnd = start.pos + (newDirection * (originalLength * Random.Range (0.6f, 1.2f)));

        end = new Node(newEnd.x, newEnd.y);
        return end;
    }
}
