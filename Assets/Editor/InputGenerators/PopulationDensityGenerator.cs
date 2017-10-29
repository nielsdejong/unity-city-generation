using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PopulationDensityGenerator {

    private static PerlinGenerator pg;
    private static float[,] fPopMap = null;
    private static float max = float.MinValue;
    private static float min = float.MaxValue;
    private static float avg = 0;
    private static float[,] areas;

    public float[,] generate() {

        // IF population density map must be generated randomly then we do it
		if (CityGenerator.popMapInput == null) {
            if (CityGeneratorUI.DebugMode) Debug.Log("Generating Random Population Density Map...");
			pg = new PerlinGenerator(CityGenerator.popSeed, CityGenerator.popOctaves, CityGenerator.popPersistance, CityGenerator.popZoom, 0, 1);            
			fPopMap = pg.getValues(CityGenerator.mapSize, CityGenerator.mapSize);
            if (CityGeneratorUI.DebugMode) Debug.Log("Random Population Density Map generated!");
        }
        // ELSE population map should be passed as input by the user
        else {
            if (CityGeneratorUI.DebugMode) Debug.Log("Population Density Map generated using existing map");

			//make a new reader and use it to read the texture
			TextureReader g = new TextureReader ();
			fPopMap = g.readTexture (CityGenerator.popMapInput, CityGenerator.mapSize);                              
        }
        
        findPeaks();
		        
        return fPopMap;
    }

    private static void getMaxMinAvg()
    {
        
        for (int i = 0; i < fPopMap.GetLength(0); i++)
        {
            for (int j = 0; j < fPopMap.GetLength(1); j++)
            {
                if (fPopMap[i, j] > max) max = fPopMap[i, j];
                if (fPopMap[i, j] < min) min = fPopMap[i, j];
                avg = avg + fPopMap[i, j];
            }
        }
        avg = (float)avg / (float)(fPopMap.GetLength(0) * fPopMap.GetLength(1));
        if (CityGeneratorUI.DebugMode) Debug.Log("avg: " + avg);
    }

    /// <summary>
    /// Finds the population density peaks in the population density map.
    /// </summary>
    private static void findPeaks()
    {
        List<Vector2> densityPeaks = new List<Vector2>();
        getMaxMinAvg();

        int n_areas = (int)Mathf.Log(fPopMap.GetLength(0), 2);
        int area_size = (int)Mathf.Floor(fPopMap.GetLength(0) / n_areas);
        if (CityGeneratorUI.DebugMode) Debug.Log("Number of areas per side: " + n_areas + "; Single area size: " + area_size);

        areas = new float[n_areas, n_areas];

        // - divides the popMap into log(size)^2 areas
        // - assigns value 1 to the area if its avg density is above general avg; 0 otherwise
        string _x = "";
        for (int i = 0; i < n_areas; i++)
        {
            for (int j = 0; j < n_areas; j++)
            {
                int _x2 = area_size * (i + 1);
                int _y2 = area_size * (j + 1);
                if (i == n_areas - 1) _x2 = fPopMap.GetLength(0);
                if (j == n_areas - 1) _y2 = fPopMap.GetLength(1);

                float localAvg = findAvg(area_size * i, _x2, area_size * j, _y2);
                areas[i, j] = (localAvg > avg) ? 1 : 0;
                _x += areas[i, j] + " ";
            }
            _x += "\n";
        }
        if (CityGeneratorUI.DebugMode) Debug.Log(_x);

        // - iterates over the areas
        // - when an area with high density is found all the other areas around that one are checked recursively
        // - when no more adjacent areas with high density are found then the whole zone is considered to be a peak
        // - each peak is stored as an 2D array containing the range over the x and the y axis
        List<int[]> peaks = new List<int[]>();
        for (int i = 0; i < n_areas; i++)
        {
            for (int j = 0; j < n_areas; j++)
            {
                if (areas[i,j] == 1)
                {
                    areas[i, j] = -1;
                    
                    int[] ij = lookaround(i, j);
                    peaks.Add(ij);

                    i = Mathf.Max(i, ij[0]);
                    j = Mathf.Max(j, ij[2]);                    
                }
            }
        }
        if (CityGeneratorUI.DebugMode) Debug.Log("peaks: " + peaks.Count);

        // - iterates over the peaks
        // - finds the "central" coordinates of the peak and converts it to a world position
        foreach (int[] peak in peaks)
        {
            // find the central area of the peak
            int i = (int)(Mathf.Floor((float)(peak[0] + peak[1]) / 2f));
            int j = (int)(Mathf.Floor((float)(peak[2] + peak[3]) / 2f));
            if (CityGeneratorUI.DebugMode) Debug.Log("A peak is in area [" + i + ", " + j + "]");

            // find the central point of the central area
            int i1 = area_size * i;
            int i2 = (i++ == n_areas) ? fPopMap.GetLength(0) : area_size * i++;
            int j1 = area_size * j;
            int j2 = (j++ == n_areas) ? fPopMap.GetLength(1) : area_size * j++;

            int centerx = (int)Mathf.Floor((float)(i1 + i2) / 2f);
            int centerz = (int)Mathf.Floor((float)(j1 + j2) / 2f);

            Vector2 newPeak = new Vector2(centerx, centerz);
            densityPeaks.Add(newPeak);
            if (CityGeneratorUI.DebugMode) GameObject.Instantiate((GameObject)Resources.Load("Node") as GameObject, 
                new Vector3(newPeak.x, CoordinateHelper.worldToTerrainHeight(newPeak.x, newPeak.y), newPeak.y),
                Quaternion.identity);
        }

        CityGenerator.densityPeaks = densityPeaks;
    }

    /// <summary>
    /// Recursively searches for areas with high density population starting from the area specified in the input indices.
    /// It stops when an area surrounded by areas with low density or areas already discovered is found.
    /// Returns an array of length 4 containing the minimum and the maximum (of both) indices reached during the search.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    private static int[] lookaround(int i, int j)
    {
        int minI = i;
        int minJ = j;
        int maxI = i;
        int maxJ = j;

        //check left
        if (j - 1 >= 0 && areas[i, j - 1] == 1)
        {
            areas[i, j - 1] = -1;
            int[] _ij = lookaround(i, j - 1);
            minI = Mathf.Min(minI, _ij[0]);
            maxI = Mathf.Max(maxI, _ij[1]);
            minJ = Mathf.Min(j-1, _ij[2]);
            maxJ = Mathf.Max(maxJ, _ij[3]);
        }

        //check right
        if (j+1 < areas.GetLength(1) && areas[i, j+1] == 1)
        {
            areas[i, j+1] = -1;
            int[] _ij = lookaround(i, j + 1);
            minI = Mathf.Min(minI, _ij[0]);
            maxI = Mathf.Max(maxI, _ij[1]);
            minJ = Mathf.Min(minJ, _ij[2]);
            maxJ = Mathf.Max(j+1, _ij[3]);
        }

        //check bottom
        if (i + 1 < areas.GetLength(0) && areas[i+1, j] == 1)
        {
            areas[i + 1, j] = -1;
            int[] _ij = lookaround(i + 1, j);
            minI = Mathf.Min(minI, _ij[0]);
            maxI = Mathf.Max(i+1, _ij[1]);
            minJ = Mathf.Min(minJ, _ij[2]);
            maxJ = Mathf.Max(maxJ, _ij[3]);
        }

        //check bottom-right
        if (i + 1 < areas.GetLength(0) && j + 1 < areas.GetLength(1) && areas[i+1, j + 1] == 1)
        {
            areas[i+1, j + 1] = -1;
            int[] _ij = lookaround(i+1, j + 1);
            minI = Mathf.Min(minI, _ij[0]);
            maxI = Mathf.Max(i + 1, _ij[1]);
            minJ = Mathf.Min(minJ, _ij[2]);
            maxJ = Mathf.Max(j + 1, _ij[3]);
        }

        //check bottom-left
        if (j-1 >= 0 && i + 1 < areas.GetLength(1) && areas[i + 1, j - 1] == 1)
        {
            areas[i + 1, j - 1] = -1;
            int[] _ij = lookaround(i + 1, j - 1);
            minI = Mathf.Min(minI, _ij[0]);
            maxI = Mathf.Max(i + 1, _ij[1]);
            minJ = Mathf.Min(j - 1, _ij[2]);
            maxJ = Mathf.Max(maxJ, _ij[3]);
        }

        //check top
        if (i - 1 >= 0 && areas[i - 1, j] == 1)
        {
            areas[i - 1, j] = -1;
            int[] _ij = lookaround(i - 1, j);
            minI = Mathf.Min(i - 1, _ij[0]);
            maxI = Mathf.Max(maxI, _ij[1]);
            minJ = Mathf.Min(minJ, _ij[2]);
            maxJ = Mathf.Max(maxJ, _ij[3]);
        }

        //check top-right
        if (i -1 >= 0 && j+1 < areas.GetLength(1) && areas[i - 1, j+1] == 1)
        {
            areas[i - 1, j + 1] = -1;
            int[] _ij = lookaround(i - 1, j + 1);
            minI = Mathf.Min(i - 1, _ij[0]);
            maxI = Mathf.Max(maxI, _ij[1]);
            minJ = Mathf.Min(minJ, _ij[2]);
            maxJ = Mathf.Max(j + 1, _ij[3]);
        }

        //check top-left
        if (i - 1 >= 0 && j - 1 >= 0 && areas[i -1, j - 1] == 1)
        {
            areas[i - 1, j - 1] = -1;
            int[] _ij = lookaround(i - 1, j - 1);

            minI = Mathf.Min(i - 1, _ij[0]);
            maxI = Mathf.Max(maxI, _ij[1]);
            minJ = Mathf.Min(j - 1, _ij[2]);
            maxJ = Mathf.Max(maxJ, _ij[3]);
        }

        return new int[] { minI, maxI, minJ, maxJ };
    }

    /// <summary>
    /// Returns the average population density of the terrain delimited by the specified indices.
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="x2"></param>
    /// <param name="y1"></param>
    /// <param name="y2"></param>
    /// <returns></returns>
    private static float findAvg(int x1, int x2, int y1, int y2)
    {
        float _avg = 0;
        for (int i = x1; i < x2; i++)
        {
            for (int j = y1; j < y2; j++)
            {
                _avg += fPopMap[i, j];
            }
        }
        _avg = (float)_avg / (float)(Mathf.Abs(x1 - x2) * Mathf.Abs(y1 - y2));
        return _avg;
    }

}
