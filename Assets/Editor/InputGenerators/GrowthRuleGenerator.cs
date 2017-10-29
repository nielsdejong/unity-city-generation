using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GrowthRuleGenerator {

    public const int red = 0;
    public const int green = 1;
    public const int blue = 2;

    private static PerlinGenerator pg;
    private static float[,] fGrowthMap = null;
    //private static int[] colors = { red, green, blue };

	public float[,] generate()
    {
        //set up array
        fGrowthMap = new float[CityGenerator.mapSize, CityGenerator.mapSize];

        // IF growth rule must be generated randomly then we do it
        if (CityGenerator.growthMapInput == null)
        {
            if (CityGeneratorUI.DebugMode) Debug.Log("Generating Random Growth Rule Map...");
			pg = new PerlinGenerator(CityGenerator.growthSeed, CityGenerator.growthOctaves, CityGenerator.growthPersistance, CityGenerator.growthZoom, 0, 1);

			float[,] perlinNoise = pg.getValues(CityGenerator.mapSize, CityGenerator.mapSize);

            float redBound = CityGenerator.growthBasic;
            float greenBound = redBound + CityGenerator.growthNewYork;

            for (int i = 0; i < CityGenerator.mapSize; i++) {
				for (int j = 0; j < CityGenerator.mapSize; j++) {
                    if (perlinNoise[i, j] <= redBound) {         // we choose red
                        fGrowthMap[i, j] = red;
                    } else if (perlinNoise[i, j] <= greenBound) {  // we choose green
                        fGrowthMap[i, j] = green;
                    } else {                                    // we choose blue
                        fGrowthMap[i, j] = blue;                
                    }
                }
            }
            if (CityGeneratorUI.DebugMode) Debug.Log("Random Growth Rule Map generated!");
        }
        // ELSE growth rule should be passed as input by the user
        else
        {     
            if (CityGeneratorUI.DebugMode) Debug.Log("GrowthRule Map generated using existing map");

            // IF the map given in input is of the right size, then we are happy
			if (CityGenerator.growthMapInput.height == CityGenerator.mapSize && CityGenerator.growthMapInput.width == CityGenerator.mapSize) {
				//the array that will contain all the pixels of the texture
				Color[] pixelArray = new Color[CityGenerator.mapSize * CityGenerator.mapSize];
				pixelArray = CityGenerator.growthMapInput.GetPixels(0, 0, CityGenerator.mapSize, CityGenerator.mapSize);

				for (int i = 0; i < pixelArray.Length; i += CityGenerator.mapSize) {
					for (int j = i; j < i + CityGenerator.mapSize; j++) {
                        if (pixelArray[j].r == 1 && pixelArray[j].g == 0 && pixelArray[j].b == 0) {
							fGrowthMap[(j % CityGenerator.mapSize), (i / CityGenerator.mapSize)] = red;
                        }
                        if (pixelArray[j].r == 0 && pixelArray[j].g == 1 && pixelArray[j].b == 0) {
							fGrowthMap[(j % CityGenerator.mapSize), (i / CityGenerator.mapSize)] = green;
                        }
                        if (pixelArray[j].r == 0 && pixelArray[j].g == 0 && pixelArray[j].b == 1) {
							fGrowthMap[(j % CityGenerator.mapSize), (i / CityGenerator.mapSize)] = blue;
                        }
                    }
                }        
            }
            // ELSE we show an error
            else { Debug.LogError("Map must have the same size of the terrain!"); }
        }
        return fGrowthMap;
    }    
}
