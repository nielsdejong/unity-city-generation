using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TerrainGenerator {

	public void generate(){
        // Initialize Height Map
		float[,] heightMap = new float[CityGenerator.mapSize, CityGenerator.mapSize];

        //set the size
        CityGenerator.terrain.terrainData.size = new Vector3(CityGenerator.terrainSize, (CityGenerator.maxHeight - CityGenerator.minHeight), CityGenerator.terrainSize);

        //based on the size we also set the alphamap resolution
		CityGenerator.terrain.terrainData.alphamapResolution = CityGenerator.mapSize;
		if (CityGenerator.terrainMap == null) {
			//generate random terrain
			PerlinGenerator perlin = new PerlinGenerator(CityGenerator.terrainSeed, CityGenerator.terrainOctaves, CityGenerator.terrainPersistance, CityGenerator.terrainZoom, 0, 1);
			heightMap = perlin.getValues(CityGenerator.mapSize, CityGenerator.mapSize);
            if (CityGeneratorUI.DebugMode) Debug.Log("Random Terrain Generated");
		} else {
            //use the given terrain Map
			TextureReader g = new TextureReader();
			heightMap = g.readTexture(CityGenerator.terrainMap, CityGenerator.mapSize);
		}

        int w = heightMap.GetLength(0);
        int h = heightMap.GetLength(1);

        float[,] result = new float[h, w];

        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                result[j, i] = heightMap[i, j];
            }
        }

        heightMap = result;

        CityGenerator.terrain.terrainData.SetHeights(0, 0, heightMap);
        CityGenerator.terrain.gameObject.transform.position = new Vector3(0, CityGenerator.minHeight, 0);

        if (CityGenerator.rWater)
        {
            // we must assign a new water to the terrain, so that the coordinates are updated
            GameObject.DestroyImmediate(GameObject.Find("Water(Clone)"));
            CityGenerator.water = GameObject.Instantiate((GameObject)Resources.Load("Water"));

            // water is at height 0
            CityGenerator.water.transform.position = new Vector3(CityGenerator.terrainSize/2f, 0, CityGenerator.terrainSize/2f);
            CityGenerator.water.transform.localScale = new Vector3(Mathf.Sqrt(2)*CityGenerator.terrainSize / 2f, 1, Mathf.Sqrt(2) * CityGenerator.terrainSize / 2f);
        } else
        {
            GameObject.DestroyImmediate(GameObject.Find("Water(Clone)"));
        }
    }
}
