using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class IntersectionChecker {

	Edge fixedRoad;
	Vector3 posN1;
	Vector3 posN2;

	public Edge fixRoad(Edge road){
		fixedRoad = road;

        //set positions of endpoints
        posN1 = CoordinateHelper.nodeToTerrain(road.n1);
		posN2 = CoordinateHelper.nodeToTerrain(road.n2);

        //start of by checking if the road intersects with another road
        if (checkIntersection (road, 0.0f)) {
			//at this point fixedRoad can be the new road or null
			//if its null, the intersection was there but it was invalid
			if (CityGeneratorUI.DebugMode)
				Debug.Log ("Intersection checker case 1 applies: road intersected");
			return fixedRoad;
		} 

		if(checkCloseNode(road)){
			//at this point fixedRoad can be the new road or null
			//if its null, the intersection was there but it was invalid
			if (CityGeneratorUI.DebugMode)
				Debug.Log ("Intersection checker case 2 applies: close node");
			return fixedRoad;
		}

		//and finally check if there is a road that this road can be connected to within a specified distance
		if (checkIntersection (road, CityGenerator.roadConnectDistance)) {
			//at this point fixedRoad can be the new road or null
			//if its null, the intersection was there but it was invalid
			if (CityGeneratorUI.DebugMode)
				Debug.Log ("Intersection checker case 3 applies: road endpoint close to other endpoint");
			return fixedRoad;
		}
		return fixedRoad;
	}

	/// <summary>
	/// Checks if the road intersects with other roads within a certain distance from the road (in the direction the road is facing)
	/// </summary>
	/// <returns><c>true</c>, if intersection was found and also changes fixedRoad, <c>false</c> otherwise.</returns>
	/// <param name="road">Road.</param>
	/// <param name="dist">Distance over which the intersection should be checked</param>
	private bool checkIntersection(Edge road, float dist){
		
		//set up variables needed for boxcastall
		int layerMask = 1 << LayerMask.NameToLayer ("Edge");
		Vector3 halfExtends = new Vector3 (0.1f, 100, ((posN1 - posN2).magnitude)/2); 
		Vector3 roadCenter = Vector3.Lerp (posN1, posN2, 0.5f);
		Vector3 direction = posN2 - posN1;
		Vector2 roadOrientation = new Vector2 (direction.x, direction.z);

		//get the intersections with roads
		RaycastHit[] intersections = Physics.BoxCastAll (roadCenter, halfExtends, direction, Quaternion.LookRotation (direction), dist, layerMask);

		//no intersections, good!
		if (intersections.Length == 0) {
			//Debug.Log ("Road did not intersect");
			return false;
		} else {
			float closestRoadDistance = float.MaxValue;

			//just set it to the first value
			GameObject closestRoad = null;
			Vector2 closestIntersection = Vector2.zero;

			//first we check which road is closest
			foreach(RaycastHit hit in intersections){
				//Debug.Log (road.getRoadType().ToString() + " intersects with: " + hit.collider.gameObject.name + " which has N1 at: " + hit.transform.GetChild (0).localPosition + " and posN1 is: " + posN1);
				//Debug.Log (road.getRoadType().ToString() + " intersects with: " + hit.collider.gameObject.name + " which has N2 at: " + hit.transform.GetChild (1).localPosition + " and posN1 is: " + posN1);
				//Debug.Log (hit.collider.gameObject.name + " shares N1: " + CoordinateHelper.areEqual (hit.transform.GetChild (0).localPosition, posN1));
				//Debug.Log (hit.collider.gameObject.name + " shares N2: " + CoordinateHelper.areEqual (hit.transform.GetChild (1).localPosition, posN1));
                
				//consider the case where roads have the same endpoints, i.e., they overlap exactly
				if ((CoordinateHelper.areEqual (hit.transform.GetChild (1).localPosition, posN1) && CoordinateHelper.areEqual (hit.transform.GetChild (0).localPosition, posN2)) ||
					(CoordinateHelper.areEqual (hit.transform.GetChild (1).localPosition, posN2) && CoordinateHelper.areEqual (hit.transform.GetChild (0).localPosition, posN1))) {
					//in this case the road cannot be placed
					if (CityGeneratorUI.DebugMode)
						Debug.Log ("Intersection checker: roads overlapped exactly");
					fixedRoad = null;
					return true;
				} else {

					//only consider road segments that are not a predecessor
					if (!CoordinateHelper.areEqual (hit.transform.GetChild (1).localPosition, posN1)) {

						//also dont consider roads that branch out from the same point
						if (!CoordinateHelper.areEqual (hit.transform.GetChild (0).localPosition, posN1)) {
							//these are the roads that have no endpoints in common with the current road.

							//check if the angle is valid
							Vector2 otherRoadOrientation = new Vector2 (hit.transform.forward.x, hit.transform.forward.z);
							//check if the two roads have enough "space" between them.

							if (isValidAngle (roadOrientation.normalized, otherRoadOrientation.normalized)) {

								//find the road that is intersected with first
								Vector2 intersectionPoint = lineIntersectionPoint (
									                            road.n1.pos, 
									                            road.n2.pos, 
									                            CoordinateHelper.threeDtoTwoD (hit.transform.GetChild (0).localPosition), 
									                            CoordinateHelper.threeDtoTwoD (hit.transform.GetChild (1).localPosition));
								
								float distance = (road.n1.pos - intersectionPoint).magnitude;
								if (distance < closestRoadDistance) {
									//Debug.Log ("Road: " + hit.collider.gameObject.name + " has distance: " + (posN1 - hit.collider.transform.position).magnitude + " to n1");
									closestRoad = hit.collider.gameObject;
									closestIntersection = intersectionPoint;
									closestRoadDistance = distance;
								}
							} else {							
								//return that the road is invalid
								fixedRoad = null;
								return true;
							}
						} else {
		                            		if (CityGeneratorUI.DebugMode) Debug.Log ("Roads have same start point");
							//there is another road branching out from the same point.. check their angle						
							Vector2 sameBranchRoadOrientation = new Vector2 (hit.transform.forward.x, hit.transform.forward.z);

							if (Vector2.Angle(roadOrientation.normalized, sameBranchRoadOrientation.normalized) > CityGenerator.minRoadAngle) {
								//this is fine
							} else {
								//when this happens we cannot place this road
								fixedRoad = null;
								return true;
							}
						} 
					} else {
						//just to be sure, we dont want to make sharp turns
						if (CityGeneratorUI.DebugMode) Debug.Log ("Roads have same start point");
						//there is another road branching out from the same point.. check their angle						
						Vector2 sameBranchRoadOrientation = new Vector2 (hit.transform.forward.x, hit.transform.forward.z);

						if (Vector2.Angle(roadOrientation.normalized, sameBranchRoadOrientation.normalized) < 180 - CityGenerator.minRoadAngle) {
							//this is fine
						} else {
							//when this happens we cannot place this road
							fixedRoad = null;
							return true;
						}
					}
				}
			}

			//no legitimate intersection was found
			if (closestRoad == null) {
				//return that no intersection was found
				return false;
			} else {
				//Now we want to see if the angle between the roads is large enough.
				Vector2 closestRoadOrientation = new Vector2 (closestRoad.transform.forward.x, closestRoad.transform.forward.z);

				//we want to make sure the angle is large enough
				if (isValidAngle(roadOrientation.normalized, closestRoadOrientation.normalized)) {

					//now we also need to update the roads list in the roadmapgenerator
					//set up all involved nodes
					Node roadToReplaceN1 = new Node(closestRoad.transform.GetChild(0).localPosition.x, closestRoad.transform.GetChild(0).localPosition.z);
					Node intersectionNode = new Node (closestIntersection);
					Node roadToReplaceN2 = new Node(closestRoad.transform.GetChild(1).localPosition.x, closestRoad.transform.GetChild(1).localPosition.z);

					//check if after splitting the new road segments are long enough
					if ((intersectionNode.pos - roadToReplaceN1.pos).magnitude > CityGenerator.minRoadLengthAfterSplit) {
						if ((intersectionNode.pos - roadToReplaceN2.pos).magnitude > CityGenerator.minRoadLengthAfterSplit) {

							fixedRoad.n2 = intersectionNode;

							//also check if the fixed road is long enough, do this before generating intersections!
							if (CoordinateHelper.validRoadLength (fixedRoad)) {

								//if the width of the closest road equals the highwaywidth, we create new highways
								RoadTypes roadType = (closestRoad.transform.localScale.x == CityGenerator.highwayWidth) ? RoadTypes.HIGHWAY : RoadTypes.STREET;

								//create new road segments for the road that is going to be split
								Edge splitRoad1 = new Edge (roadToReplaceN1, intersectionNode, roadType);
								Edge splitRoad2 = new Edge (intersectionNode, roadToReplaceN2, roadType);

								//get a reference to the road that is going to be split
								Edge roadToReplace = RoadMapGenerator.getRoad (roadToReplaceN1, roadToReplaceN2);

								//the road segments which will replace closestRoad
								List<Edge> replacementRoads = new List<Edge> ();
								replacementRoads.Add (splitRoad1);
								replacementRoads.Add (splitRoad2);

								if (CityGeneratorUI.DebugMode) Debug.Log (closestRoad.name + " was split by road starting at" + road.n1 + " and ending at " + road.n2);

								//physically change the road
								RoadVisualizer.replaceRoad (closestRoad, replacementRoads);

								//now replace the roads in the road list (logically)
								RoadMapGenerator.replaceRoad (roadToReplace, replacementRoads);
							}

						} else {
							//we cannot fix this intersection
							fixedRoad = null;
							return true;
						}
					} else {
						//we cannot fix this intersection
						fixedRoad = null;
						return true;
					}

					return true;
				} else {
					//the road intersects with a road while the angle between them is too small
					//so we cannot place this road
					fixedRoad = null;
					return true;
				}
			}
		}		
	}

	/// <summary>
	/// Checks if there is a nearby node that this road can connect to
	/// </summary>
	/// <returns><c>true</c>, if close node was found, <c>false</c> otherwise.</returns>
	/// <param name="road">Road.</param>
	private bool checkCloseNode(Edge road){
		float roadLength = (posN1 - posN2).magnitude;
		int layerMask = 1 << LayerMask.NameToLayer ("Node");

		//we use the road length to make sure we dont find the node at PosN1
		Collider[] hits = Physics.OverlapSphere (posN2, Mathf.Min (roadLength / 2, CityGenerator.nodeCheckRadius), layerMask);

		if (hits.Length == 0) {
			//Debug.Log ("Node close nodes found");
			return false;
		} else {
			//we hit a node

			Collider closestNode = null;
			float closestNodeDistance = float.MaxValue;

			//find the closest hit
			foreach(Collider hit in hits){
				//ignore the node at posN2
				if (!CoordinateHelper.areEqual (hit.gameObject.transform.position, posN2)) {
					//check if the current hit is closer
					if ((hit.gameObject.transform.position - posN2).magnitude < closestNodeDistance) {
						closestNode = hit;
						closestNodeDistance = (hit.gameObject.transform.position - posN2).magnitude;
					}
				}
			}

			//no closest node found
			if (closestNode == null) {
				return false;
			} else {				
				//we found the closest node
				//Debug.Log ("The closest node is: " + closestNode.gameObject.name);
				fixedRoad.n2.pos = CoordinateHelper.threeDtoTwoD (closestNode.transform.position);
				return true;
			}
		}
	}

	/// <summary>
	/// Finds the intersection point between two lines
	/// </summary>
	/// <returns>The intersection point.</returns>
	/// <param name="ps1">Pstart1.</param>
	/// <param name="pe1">Pend1.</param>
	/// <param name="ps2">Pstart2.</param>
	/// <param name="pe2">Pend2.</param>
	/// Obtained from: http://www.wyrmtale.com/blog/2013/115/2d-line-intersection-in-c
	private Vector2 lineIntersectionPoint(Vector2 ps1, Vector2 pe1, Vector2 ps2, Vector2 pe2)
	{
		// Get A,B,C of first line - points : ps1 to pe1
		float A1 = pe1.y-ps1.y;
		float B1 = ps1.x-pe1.x;
		float C1 = A1*ps1.x+B1*ps1.y;

		// Get A,B,C of second line - points : ps2 to pe2
		float A2 = pe2.y-ps2.y;
		float B2 = ps2.x-pe2.x;
		float C2 = A2*ps2.x+B2*ps2.y;

		// Get delta and check if the lines are parallel
		float delta = A1*B2 - A2*B1;
		if (delta == 0) {			
			Debug.LogError ("Angle between:" + (pe1 - ps1).normalized + " and " + (pe2 - ps2).normalized + "  " + Vector2.Angle ((pe1 - ps1).normalized, (pe2 - ps2).normalized) + " ps2: " + ps2 + " pe2 " + pe2 + " closestRoad: ");
			//throw new System.Exception ("Lines are parallel");
		}

		// now return the Vector2 intersection point
		return new Vector2(
			(B2*C1 - B1*C2)/delta,
			(A1*C2 - A2*C1)/delta
		);
	}

	private bool isValidAngle(Vector2 v1, Vector2 v2){		
		//the vectors face in "opposite" direction so flip one of them
		if (Vector2.Angle (v1, v2) > 90) {			
			v1 = -v1;
		}

		//just to be sure
		if (v1 == Vector2.zero || v2 == Vector2.zero) {
			return false;
		}

		if (Vector2.Angle (v1, v2) > CityGenerator.minRoadAngle && Vector2.Angle(v1, v2) != 0) {
			//Debug.Log ("Valid Angle between: " + v1 + " and " + v2 + " is: " + Vector2.Angle (v1, v2));
			return true;
		}
		return false;
	}
}
