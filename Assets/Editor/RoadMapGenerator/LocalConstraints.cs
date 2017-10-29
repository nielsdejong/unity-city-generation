using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LocalConstraints {
    IntersectionChecker intersectionChecker = new IntersectionChecker();

    public Edge validateRoad(Edge road){
		if(CityGeneratorUI.DebugMode)
			Debug.Log ("Localconstraints, checking: " + road);
		
        // Try to legalize the position
        Edge newRoad = PositionLegalizer.legalizeRoad(road);
        if (newRoad == null)
        {
			if(CityGeneratorUI.DebugMode)
				Debug.Log ("Position legalizer could not fix: " + road);
            return null;
        }
        else
        {
            // Fix intersections
			newRoad = intersectionChecker.fixRoad(newRoad);
            if(newRoad == null)
            {
				if(CityGeneratorUI.DebugMode)
					Debug.Log ("Intersection checker could not fix: " + road);
                return null;
            }          

			//Check if the road has a valid length
			if (!CoordinateHelper.validRoadLength(newRoad)) {
				if(CityGeneratorUI.DebugMode)
					Debug.Log ("Road was fixed but was not long enough: " + road);
				return null;
			} else {				
				return newRoad;
			}
        }
	}

    public List<Edge> validateRoads(List<Edge> roads)
    {
        for (int i=roads.Count-1; i >= 0; i--)
        {
            roads[i] = validateRoad(roads[i]);
            if (roads[i] == null) roads.RemoveAt(i);
        }
        return roads;
    }
}
