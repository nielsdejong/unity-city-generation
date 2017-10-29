using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HousePlacer{

	public void placeHouses(){
		foreach (Edge e in RoadMapGenerator.roads) {
			if (e.getRoadType() != RoadTypes.HIGHWAY) {
				attemptPlaceHouses (e);
			}
		}
	}

	private void attemptPlaceHouses(Edge e){
		//we try to place a house on both sides of the edge
		Vector2 edgeMiddle = Vector2.Lerp(e.n1.pos, e.n2.pos, 0.5f);
		float edgePop = CoordinateHelper.worldToPop (edgeMiddle.x, edgeMiddle.y);
		float edgeLength = (e.n2.pos - e.n1.pos).magnitude;
		Vector2 edgeDirection = e.n2.pos - e.n1.pos;
		Vector2 edgePerpen = new Vector2 (edgeDirection.y, -edgeDirection.x);

		//make sure we place houses only in low pop areas
		if (edgePop < CityGenerator.skyscraperPopThreshold) {
			//two iterations (both sides of road), we have -1 and 1 for i
			for (int i = -1; i <= 1; i += 2) {
				float width = Mathf.Min(Random.Range (0.65f * edgeLength, 0.87f * edgeLength), CityGenerator.maxHouseWidth);
				float depth = Random.Range (width / 2.5f, width);
				int nrOfFloors = (edgePop < (CityGenerator.skyscraperPopThreshold / 2)) ? 1 : 2;
				Vector2 position = edgeMiddle + (i * edgePerpen.normalized * ((depth / 2) + (CityGenerator.streetWidth / 2) + 4));
				Vector2 direction =  -i * edgePerpen.normalized;

				int layerMask1 = 1 << LayerMask.NameToLayer ("Edge");
				int layerMask2 = 1 << LayerMask.NameToLayer ("Building");
				int layerMask = layerMask1 | layerMask2;	//or operator

				Vector3 halfExtends = new Vector3 ((depth / 2) - 1, (nrOfFloors * CityGenerator.floorHeight)/2, (width / 2) - 1);
				Vector3 position3D = new Vector3 (position.x, CoordinateHelper.worldToTerrainHeight (position), position.y);

                // not underwater
                if(position3D.y < 0)
                {
                    continue;
                }
				Vector3 edgeMiddle3D = position3D - new Vector3 (edgeMiddle.x, position3D.y, edgeMiddle.y);
				
				if(!Physics.CheckBox(position3D, halfExtends, Quaternion.LookRotation(edgeMiddle3D, Vector3.up), layerMask)){					
					//place the house
					BuildingGenerator.generateHouse (position, direction, width, depth, nrOfFloors);
				}
			}

		}			
	}
}
