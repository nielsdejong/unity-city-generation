using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Script responsible for visualizing one of the maps on the terrain
public class MapVisualizer {
	private float[,,] textureData;		//used to store the texturing result

	private bool oldShowPop = false;	//value of the last pop visualization
	private bool oldShowGrowth = false;	//value of the last growth visualization
	private float[,,] oldAlphaMap;		//the old texturing of the terrain
	private SplatPrototype[] oldTerrainTextures;	//the old textures that were used for the old texturing

	//Given the parameters, visualizes the map on the terrain
	public void visualizeMap(){
		
		//in this case we wish to remember the old texturing
		if(oldShowPop == false && oldShowGrowth == false){
			oldAlphaMap = CityGenerator.terrain.terrainData.GetAlphamaps 
                (0, 0, CityGenerator.terrain.terrainData.alphamapWidth, CityGenerator.terrain.terrainData.alphamapHeight);
			oldTerrainTextures = CityGenerator.terrain.terrainData.splatPrototypes;
		}

		float popFrac;		//the fraction in how much the pop map adds to the final texture
		float growthFrac;   //the fraction in how much the growth map adds to the final texture

		//we wish to show the old alpha maps again
		if (CityGenerator.showPop == false && CityGenerator.showGrowth == false) {

            //set up old textures
            CityGenerator.terrain.terrainData.splatPrototypes = oldTerrainTextures;

            //set up old texturing
            CityGenerator.terrain.terrainData.SetAlphamaps (0, 0, oldAlphaMap);

			//store and restore the old values
			oldShowPop = CityGenerator.showPop;
			oldShowGrowth = CityGenerator.showGrowth;

			return;

		} else if (CityGenerator.showPop == true && CityGenerator.showGrowth == false) {
			//we are painting only the pop map
			popFrac = 1f;
			growthFrac = 0f;
		} else if (CityGenerator.showPop == false && CityGenerator.showGrowth == true) {
			//we are painting only the growth map
			popFrac = 0f;
			growthFrac = 1f;
		} else {
			//we are painting both
			popFrac = 0.5f;
			growthFrac = 0.5f;
		}

        if (CityGeneratorUI.DebugMode)
        {
            Debug.Log("popFrac:" + popFrac);
            Debug.Log("growthFrac:" + growthFrac);
        }

		// this makes sure that we can use the textures we need
		// to paint the pop/growth maps
		setUpTextures ();

		//float[y, x, nr of textures]
		textureData = new float[CityGenerator.terrain.terrainData.alphamapWidth, 
            CityGenerator.terrain.terrainData.alphamapHeight,
            CityGenerator.terrain.terrainData.alphamapLayers];

		//now check if the map size equals the terrain size.. this should be the case
		//if(terrainData.alphamapWidth != map.GetLength(0) || terrainData.alphamapHeight != map.GetLength(1)){
		//	Debug.LogError ("Map size does not match terrain size");
		//}

		for (int y = 0; y < CityGenerator.terrain.terrainData.alphamapHeight; y++) {
			for (int x = 0; x < CityGenerator.terrain.terrainData.alphamapWidth; x++) {
                
                float popMapValue = (CityGenerator.popMap == null) ? 0 : CityGenerator.popMap[x, y];
                float growthMapValue = (CityGenerator.growthMap == null) ? -1 : CityGenerator.growthMap[x, y];

                textureData[y, x, 0] = popMapValue * popFrac;   //white
                textureData[y, x, 1] = (1 - popMapValue) * popFrac; //black

                //if the current coordinate is red
                if (growthMapValue == 0)
                {
                    textureData[y, x, 2] = 1 * growthFrac;  //red
                }
                else
                {
                    textureData[y, x, 2] = 0;   //red
                }

                //if the current coordinate is green
                if (growthMapValue == 1)
                {
                    textureData[y, x, 3] = 1 * growthFrac;  //green
                }
                else
                {
                    textureData[y, x, 3] = 0;   //green
                }

                //if the current coordiante is blue
                if (growthMapValue == 2)
                {
                    textureData[y, x, 4] = 1 * growthFrac;  //blue
                }
                else
                {
                    textureData[y, x, 4] = 0;   //blue
                }
            }
        }

        //apply the new textures
        CityGenerator.terrain.terrainData.SetAlphamaps (0, 0, textureData);

		//store and restore the old values
		oldShowPop = CityGenerator.showPop;
		oldShowGrowth = CityGenerator.showGrowth;
	}

	//in order to texture the terrain, we need to set the textures up in the terrain paint thingy
	private void setUpTextures(){
        //remove the current textures
        CityGenerator.terrain.terrainData.splatPrototypes = null;

		//this will contain the eventual textures
		SplatPrototype[] textureArray = new SplatPrototype[5];
		string[] textureNames = new string[] {
			"whiteTexture",
			"blackTexture",
			"redTexture",
			"greenTexture",
			"blueTexture"
		};

		//Add a new SplatPrototype (= texture for terrain) for each texture
		for (int i = 0; i < 5; i++) {
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
        CityGenerator.terrain.terrainData.splatPrototypes = textureArray;
	}
}
