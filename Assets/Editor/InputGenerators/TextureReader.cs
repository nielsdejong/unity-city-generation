using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TextureReader {

	//reads the given texture and returns a 2D array of values between 0 and 1 representing the grayscale values
	public float[,] readTexture(Texture2D map, int textureSize){
		//the map that will be returned
		float[,] fMap = null; 

		// IF the map given in input has the right size, then we are even happier
		if (map.height == textureSize && map.width == textureSize) {
			//read the pixels from the texture
			Color[] pixelArray = new Color[textureSize * textureSize];
			pixelArray = map.GetPixels (0, 0, textureSize, textureSize);

			//set up array
			fMap = new float[textureSize, textureSize];
	
			for (int i = 0; i < pixelArray.Length; i += textureSize) {
				for (int j = i; j < i + textureSize; j++) {
					fMap [(j % textureSize), (i / textureSize)] = pixelArray [j].grayscale;
				}
			}

		} else {
			Debug.LogError ("Texture size does not match terrain size!");
		}

		return fMap;
	}
}
