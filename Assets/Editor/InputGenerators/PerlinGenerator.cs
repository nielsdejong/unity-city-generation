using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinGenerator
{
    //private int seed;
    private int octaves;
    private float persistance;
    private float zoom;
    private float maxHeight;
    private float minHeight;
    private float[] xOffsets;
    private float[] yOffsets;


    public PerlinGenerator(int seed, int octaves, float persistance, float zoom, float minHeight, float maxHeight)
    {
        /* Set variables for the generator script */
        //this.seed = seed;
        this.octaves = octaves;
        this.persistance = persistance;
        this.zoom = zoom;
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;

        /* Build offset array for all octaves of the Perlin Noise functions */
        xOffsets = new float[octaves];
        yOffsets = new float[octaves];

        Random.InitState(seed);
        for(int a = 0; a < octaves; a++)
        {
            xOffsets[a] = Random.value * 10000f;
            yOffsets[a] = Random.value * 10000f;
        }
    }
    private float getValue(float X, float Y)
    {
        float value = 0f;
        /* Combine Octaves of noise with the zoom function */
        for(int a = 0; a < octaves; a++)
        {
            value = (value * persistance) + Mathf.PerlinNoise(xOffsets[a] + X * zoom * Mathf.Pow(2, -a),
                                       yOffsets[a] + Y * zoom * Mathf.Pow(2, -a));
        }
        return value;
    }


    public float[,] getValues(int width, int height)
    {
        float[,] values = new float[width, height];

        /* Loop through map and fill with generated values. Also, find the lowest and highest value in the map */
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                values[x, y] = getValue(x, y);
                min = Mathf.Min(values[x, y], min);
                max = Mathf.Max(values[x, y], max);
            }
        }
      
        /* Normalize the height values based on the detected minimum and maximum values */
        float generatedRange = max - min;
        float newRange = maxHeight - minHeight;
        if (CityGeneratorUI.DebugMode)
        {
            Debug.Log(min);
            Debug.Log(max);
            Debug.Log(generatedRange);
            Debug.Log(newRange);
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float percentage = ((values[x, y]-min) / generatedRange);
                values[x, y] = minHeight + (percentage * newRange);
            }
        }
        return values;
    }
}
