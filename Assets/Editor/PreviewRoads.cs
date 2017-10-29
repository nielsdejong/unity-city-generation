using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Obtained from unity tutorial on editor scripting: https://www.youtube.com/watch?v=9bHzTDIJX_Q&t=2773s
/// </summary>
public class PreviewRoads 
{
	public static float NrRoads
	{
		get
		{
			if( Application.isPlaying == true )
			{
				return UnityEngine.Time.timeSinceLevelLoad;
			}

			//EditorPrefs is the same as PlayerPrefs but it only works in the editor
			//This way you can store variables persistantly even if you close the editor
			return EditorPrefs.GetFloat( "NrRoads", 0 );
		}
		set
		{
			if (value <= CityGenerator.nrOfRoads && value >= 0) {
				EditorPrefs.SetFloat ("NrRoads", value);
			}
		}
	}
}