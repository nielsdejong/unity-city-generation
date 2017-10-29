using UnityEditor;
using UnityEngine;
using UnityEditor.AnimatedValues;

// useful alias
using CG = CityGenerator;
using GL = UnityEngine.GUILayout;
using EG = UnityEditor.EditorGUI;
using EGL = UnityEditor.EditorGUILayout;

public class CityGeneratorUI : EditorWindow
{
	
	public static CityGeneratorUI window;
	private CityGenerator generator = new CityGenerator ();
	private bool oldShowPop = false;
	private bool oldShowGrowth = false;
	private Vector2 scrollLocation = new Vector2 (0, 0);
	private bool showTerrainUI = true;
	private bool showPopUI = false;
	private bool showGrowthUI = false;
	private bool showRoadMapAdvanced = false;
	private bool showRoadMapUI = false;
	private bool showBuildingUI = false;
	string terrainLabel = "1. Terrain Generation - NOT COMPLETED ✘";
	string populationLabel = "2. Population Map Generation - NOT COMPLETED ✘";
	string growthLabel = "3. Growth Map Generation - NOT COMPLETED ✘";
	string roadmapLabel = "4. Road Map Generation - NOT COMPLETED ✘";
	string buildingLabel = "5. Building Generation - NOT COMPLETED ✘";
	bool isTerrain = false;
	bool terrainGenerated = false;
	bool isPopMap = false;
	bool populationGenerated = false;
	bool isGrowthMap = false;
	bool growthMapGenerated = false;
	//bool isRoadMap = false;
	bool roadMapGenerated = false;
	bool roadMeshGenerated = false;

	public static bool DebugMode { get; private set; }


	// Fade booleans
	AnimBool rDebugToggle;
	AnimBool rWaterToggle;
	AnimBool rTerrainSeed, rPopSeed, rGrowthSeed;
	AnimBool rProcGen;
	AnimBool rCustomMap;
	AnimBool rPopToggle, rGrowthToggle;

	//used for road generation visualization
	private float m_PlaybackModifier;
	private float m_LastTime;

	[MenuItem ("Tools/CityGenerator")] //Add a menu item to the toolbar
	static void OpenWindow ()
	{
		window = (CityGeneratorUI)EditorWindow.GetWindow<CityGeneratorUI> (false, "City Generator"); //create a window
	}

	void OnEnable ()
	{
		rDebugToggle = new AnimBool (false);
		rDebugToggle.valueChanged.AddListener (() => {
			DebugMode = rDebugToggle.value;
		}
		);

		rWaterToggle = new AnimBool (false);
		rWaterToggle.valueChanged.AddListener (Repaint);

		rTerrainSeed = new AnimBool (false);
		rTerrainSeed.valueChanged.AddListener (Repaint);
		rPopSeed = new AnimBool (false);
		rPopSeed.valueChanged.AddListener (Repaint);
		rGrowthSeed = new AnimBool (false);
		rGrowthSeed.valueChanged.AddListener (Repaint);

		rProcGen = new AnimBool (false);
		rCustomMap = new AnimBool (false);
		rProcGen.speed = 1000f;
		rCustomMap.speed = 1000f;
		//used for road generation visualization
		EditorApplication.update -= OnEditorUpdate;
		EditorApplication.update += OnEditorUpdate;            
	}

	void OnDisable ()
	{
		EditorApplication.update -= OnEditorUpdate;
	}

	void OnGUI ()
	{        
		//This is used for debugging, when the code is changed, simply refocussing the City Generator window will reload the code
		if (window == null) {
			OpenWindow ();
		}
		scrollLocation = EGL.BeginScrollView (scrollLocation);

		GL.BeginHorizontal ();
		GL.Label ("Procedural City Generator", EditorStyles.boldLabel);
		GL.Space (50);
		EGL.BeginHorizontal ("Box");
		rDebugToggle.target = EGL.ToggleLeft ("Debug Mode?", rDebugToggle.target);
		EGL.EndHorizontal ();
		GL.EndHorizontal ();

		GL.Box ("", new GUILayoutOption[] { GL.ExpandWidth (true), GL.Height (1) });
		showTerrainGUI ();

		GL.Box ("", new GUILayoutOption[] { GL.ExpandWidth (true), GL.Height (1) });
		showPopulationMapGUI ();

		GL.Box ("", new GUILayoutOption[] { GL.ExpandWidth (true), GL.Height (1) });
		showGrowthMapGUI ();

		GL.Box ("", new GUILayoutOption[] { GL.ExpandWidth (true), GL.Height (1) });
		showRoadMapGUI ();

		GL.Box ("", new GUILayoutOption[] { GL.ExpandWidth (true), GL.Height (1) });
		showBuildingGUI ();

		GL.Box ("", new GUILayoutOption[] { GL.ExpandWidth (true), GL.Height (1) });
		if (GL.Button ("Reset")) {
			oldShowPop = false;
			oldShowGrowth = false;
			showTerrainUI = true;
			showPopUI = false;
			showGrowthUI = false;
			showRoadMapAdvanced = false;
			showRoadMapUI = false;
			showBuildingUI = false;
			terrainGenerated = false;
			populationGenerated = false;
			growthMapGenerated = false;
			roadMapGenerated = false;
			roadMeshGenerated = false;

			terrainLabel = "1. Terrain Generation - NOT COMPLETED ✘";
			populationLabel = "2. Population Map Generation - NOT COMPLETED ✘";
			growthLabel = "3. Growth Map Generation - NOT COMPLETED ✘";
			roadmapLabel = "4. Road Map Generation - NOT COMPLETED ✘";
			buildingLabel = "5. Building Generation - NOT COMPLETED ✘";
		}

		// TESTING
		/*
		if (GL.Button ("Generate Houses")) {
			generator.testHouses ();
		}*/
		//END TESTING

		EGL.EndScrollView ();
	}

	void showTerrainGUI ()
	{
		// General Terrain Settings
		showTerrainUI = EGL.Foldout (showTerrainUI, terrainLabel, true);

		if (showTerrainUI) { 

			GL.BeginHorizontal ();
			GL.BeginVertical ();
			GL.Label (new GUIContent ("Terrain Size", "The width in units of the generated Terrain Object."));
			GL.Label (new GUIContent ("Terrain Height Range", "The min and max height in units of the generated Terrain Object."));
			GL.Label ("Water?");
			GL.EndVertical ();
         
			GL.BeginVertical ();
			CG.terrainSize = EGL.IntSlider (CG.terrainSize, 512, 2048);
			GL.BeginHorizontal ();
			GL.TextField (CG.minHeight.ToString ("F1"));
			EGL.MinMaxSlider (ref CG.minHeight, ref CG.maxHeight, CG.terrainSize * -CG.highwayMaxSlope, CG.terrainSize * CG.highwayMaxSlope);
			GL.TextField (CG.maxHeight.ToString ("F1"));
			GL.EndHorizontal ();
			EG.BeginDisabledGroup (CG.minHeight > 0);
			rWaterToggle.target = EGL.Toggle (rWaterToggle.target);
			EG.EndDisabledGroup ();
			GL.EndVertical ();
			GL.EndHorizontal ();

			GL.BeginVertical ();
			GL.Label ("Height Map Generation", EditorStyles.centeredGreyMiniLabel);
			GL.BeginHorizontal ("box");
			GL.BeginVertical ();
			GL.Label ("Octaves");
			GL.Label ("Persistance");
			GL.Label ("Zoom");
			GL.Label ("Seed");          
			GL.EndVertical ();

			GL.BeginVertical ();
			CG.terrainOctaves = EGL.IntSlider (CG.terrainOctaves, 1, 6);
			CG.terrainPersistance = EGL.Slider (CG.terrainPersistance, 0, 0.7f);
			CG.terrainZoom = EGL.Slider (CG.terrainZoom, 0.01f, 0.04f);
			GL.BeginHorizontal ();
			EG.BeginDisabledGroup (rTerrainSeed.target == false);
			CG.terrainSeed = EGL.IntSlider (CG.terrainSeed, 0, int.MaxValue);
			EG.EndDisabledGroup ();
			rTerrainSeed.target = EGL.Toggle (rTerrainSeed.target);
			GL.EndHorizontal ();
			GL.Space (20);
			GL.BeginHorizontal ();
			GL.Label ("Or import your custom height map: ");
			CG.terrainMap = (Texture2D)EGL.ObjectField (CG.terrainMap, typeof(Texture2D), false);
			GL.EndHorizontal ();
			GL.EndVertical ();


			GL.EndHorizontal ();
			GL.EndVertical ();

			GL.BeginHorizontal ();
			if (GL.Button ("Generate New Terrain")) {
				if (rTerrainSeed.target == false && CG.terrainMap == null) {
					CG.terrainSeed = Random.Range (0, int.MaxValue);
				}
				CG.rWater = rWaterToggle.target;
				generator.generateTerrain ();
				isTerrain = true;
			}
			EG.BeginDisabledGroup (!isTerrain);
			if (GL.Button ("Save and Proceed")) {
				terrainGenerated = true;
				terrainLabel = "1. Terrain Generation - COMPLETED ✔";
				if (CityGeneratorUI.DebugMode)
					Debug.Log ("Terrain Generated");
				showTerrainUI = false;
				showPopUI = true;
			}
			EG.EndDisabledGroup ();

			GL.EndHorizontal ();
		}
	}

	void showPopulationMapGUI ()
	{
		EG.BeginDisabledGroup (!(terrainGenerated));
		showPopUI = EGL.Foldout (showPopUI, populationLabel, true);
		if (showPopUI) {
			GL.BeginVertical ();
			GL.BeginHorizontal ("box");
			GL.BeginVertical ();
			GL.Label ("Octaves");
			GL.Label ("Persistance");
			GL.Label ("Zoom");
			GL.Label ("Seed");
			GL.EndVertical ();

			GL.BeginVertical ();
			CG.popOctaves = EGL.IntSlider (CG.popOctaves, 1, 6);
			CG.popPersistance = EGL.Slider (CG.popPersistance, 0, 0.5f);
			CG.popZoom = EGL.Slider (CG.popZoom, 0, 0.05f);
			GL.BeginHorizontal ();
			EG.BeginDisabledGroup (rPopSeed.target == false);
			CG.popSeed = EGL.IntSlider (CG.popSeed, 0, int.MaxValue);
			EG.EndDisabledGroup ();
			rPopSeed.target = EGL.Toggle (rPopSeed.target);
			GL.EndHorizontal ();

			GL.Space (20);

			GL.BeginHorizontal ();
			GL.Label ("Or import your custom pop map: ");
			CG.popMapInput = (Texture2D)EGL.ObjectField (CG.popMapInput, typeof(Texture2D), false);
			GL.EndHorizontal ();
			GL.EndVertical ();

			GL.EndHorizontal ();
			GL.EndVertical ();

			GL.BeginHorizontal ();
			if (GL.Button ("Generate Population Map")) {
				if (rPopSeed.target == false && CG.popMapInput == null) {
					CG.popSeed = Random.Range (0, int.MaxValue);
				}
				generator.generatePopulationMap ();
				isPopMap = true;
				CG.showPop = true;
			}

			EG.BeginDisabledGroup (!isPopMap);
			GL.BeginHorizontal ();
			GL.FlexibleSpace ();
			CG.showPop = EGL.ToggleLeft ("Preview Pop Map", CG.showPop);
			GL.EndHorizontal ();
            
			GL.EndHorizontal ();
			if (GL.Button ("Save and Proceed")) {
				populationGenerated = true;
				populationLabel = "2. Population Map Generation - COMPLETED ✔";
				showPopUI = false;
				CG.showPop = false;
				showGrowthUI = true;
			}
			EG.EndDisabledGroup ();
		}
		EG.EndDisabledGroup ();
        
	}

	void showGrowthMapGUI ()
	{
		EG.BeginDisabledGroup (!(terrainGenerated));
		showGrowthUI = EGL.Foldout (showGrowthUI, growthLabel, true);
		if (showGrowthUI) {
			GL.BeginVertical ();

			GL.BeginHorizontal ();
			GL.BeginVertical ();
			if (GL.Button ("Basic Rule")) {
				CG.growthBasic = 1;
				CG.growthNewYork = 0;
				CG.growthParis = 0;
			}
			if (GL.Button ("New York Rule")) { 
				CG.growthNewYork = 1;
				CG.growthBasic = 0;
				CG.growthParis = 0;
			}
			if (GL.Button ("Paris Rule")) {
				CG.growthParis = 1;
				CG.growthBasic = 0;
				CG.growthNewYork = 0;
			}
			GL.EndVertical ();
			GL.BeginVertical ();
			GL.Space (1);
			CG.growthBasic = EGL.Slider (1 - CG.growthNewYork - CG.growthParis, 0, 1);
			GL.Space (1);
			CG.growthNewYork = EGL.Slider (1 - CG.growthBasic - CG.growthParis, 0, 1);
			GL.Space (1);
			CG.growthParis = EGL.Slider (1 - CG.growthBasic - CG.growthNewYork, 0, 1);
			GL.EndVertical ();
			GL.EndHorizontal ();
			GL.Space (1);

			if (GL.Button ("Default")) {
				CG.growthParis = 1f / 3f;
				CG.growthBasic = 1f / 3f;
				CG.growthNewYork = 1f / 3f;
			}

			GL.BeginHorizontal ("box");
			GL.BeginVertical ();
			GL.Label ("Octaves");
			GL.Label ("Persistance");
			GL.Label ("Zoom");
			GL.Label ("Seed");
			GL.EndVertical ();

			GL.BeginVertical ();
			CG.growthOctaves = EGL.IntSlider (CG.growthOctaves, 1, 6);
			CG.growthPersistance = EGL.Slider (CG.growthPersistance, 0, 0.7f);
			CG.growthZoom = EGL.Slider (CG.growthZoom, 0, 0.05f);
			GL.BeginHorizontal ();
			EG.BeginDisabledGroup (rGrowthSeed.target == false);
			CG.growthSeed = EGL.IntSlider (CG.growthSeed, 0, int.MaxValue);
			EG.EndDisabledGroup ();
			rGrowthSeed.target = EGL.Toggle (rGrowthSeed.target);
			GL.EndHorizontal ();

			GL.Space (20);

			GL.BeginHorizontal ();
			GL.Label ("Or import your growth-rule map: ");
			CG.growthMapInput = (Texture2D)EGL.ObjectField (CG.growthMapInput, typeof(Texture2D), false);
			GL.EndHorizontal ();
			GL.EndVertical ();

			GL.EndHorizontal ();
			GL.EndVertical ();

			GL.BeginHorizontal ();
			if (GL.Button ("Generate Growth Map")) {
				if (rGrowthSeed.target == false && CG.growthMapInput == null) {
					CG.growthSeed = Random.Range (0, int.MaxValue);
				}
				generator.generateGrowthRule ();
				isGrowthMap = true;
				CG.showGrowth = true;
			}

			EG.BeginDisabledGroup (!isGrowthMap);
			GL.BeginHorizontal ();
			GL.FlexibleSpace ();
			CG.showGrowth = EGL.ToggleLeft ("Preview Growth Map", CG.showGrowth);
			GL.FlexibleSpace ();
			GL.EndHorizontal ();
			GL.EndHorizontal ();

			if (GL.Button ("Save and Proceed")) {
				growthMapGenerated = true;
				growthLabel = "3. Growth Map Generation - COMPLETED ✔";
				showGrowthUI = false;
				CG.showGrowth = false;
				showRoadMapUI = true;
			}
			EG.EndDisabledGroup ();
		}
		EG.EndDisabledGroup ();
	}

	void showRoadMapGUI ()
	{
		EG.BeginDisabledGroup (!(terrainGenerated && growthMapGenerated && populationGenerated));
		showRoadMapUI = EGL.Foldout (showRoadMapUI, roadmapLabel, true);
		if (showRoadMapUI) {

			GL.Label ("Streets", EditorStyles.centeredGreyMiniLabel);
			GL.BeginHorizontal ();
			GL.BeginVertical ();
			GL.Label ("Width");
			GL.Label ("Min Length");
			GL.Label ("LookAhead");
			GL.Label ("Pop Threshold");
			GL.Label ("Max Slope");
			GL.EndVertical ();
			GL.BeginVertical ();
			CG.streetWidth = EGL.IntSlider (CG.streetWidth, 5, 20);
			CG.streetMinLength = EGL.Slider (CG.streetMinLength, 5f, 100f);
			CG.streetLookAhead = EGL.IntSlider (CG.streetLookAhead, 1, (int)(CG.terrainSize / CG.streetMinLength));
			CG.streetPopThreshold = EGL.Slider (CG.streetPopThreshold, 0f, 1f);
			CG.streetMaxSlope = EGL.Slider (CG.streetMaxSlope, 0f, 1f);
			GL.EndVertical ();
			GL.EndHorizontal ();

			GL.Space (5);

			GL.Label ("Highways", EditorStyles.centeredGreyMiniLabel);
			GL.BeginHorizontal ();
			GL.BeginVertical ();
			GL.Label ("Width");
			GL.Label ("Min Length");
			GL.Label ("LookAhead");
			GL.Label ("Pop Threshold");
			GL.Label ("Max Slope");
			GL.EndVertical ();
			GL.BeginVertical ();
			CG.highwayWidth = EGL.IntSlider (CG.highwayWidth, 10, 25);
			CG.highwayMinLength = EGL.Slider (CG.highwayMinLength, 5f, 200f);
			CG.highwayLookAhead = EGL.IntSlider (CG.highwayLookAhead, 1, (int)(CG.terrainSize / CG.highwayMinLength));
			CG.highwayPopThreshold = EGL.Slider (CG.highwayPopThreshold, 0f, 1f);
			CG.highwayMaxSlope = EGL.Slider (CG.highwayMaxSlope, 0f, 1f);
			GL.EndVertical ();
			GL.EndHorizontal ();

			GL.Space (10);

			showRoadMapAdvanced = EGL.Foldout (showRoadMapAdvanced, "Advanced Settings", true);
			if (showRoadMapAdvanced) {
				EGL.HelpBox ("Adjusting these settings might break the Editor or severely influence performance.", MessageType.Warning);
				GL.Label ("General Advanced Settings", EditorStyles.centeredGreyMiniLabel);
				GL.BeginHorizontal ();
				GL.BeginVertical ();
				GL.Label ("Legalization Attempts");
				GL.Label ("Min Road Correction Angle");
				GL.Label ("Node Check Radius");
				GL.Label ("Road Connect Max Distance");
				GL.Label ("Ray Count");
				GL.EndVertical ();
				GL.BeginVertical ();
				CG.legalizationAttempts = EGL.IntSlider (CG.legalizationAttempts, 1, 100);
				CG.minRoadAngle = EGL.IntSlider (CG.minRoadAngle, 0, 90);
				CG.nodeCheckRadius = EGL.Slider (CG.nodeCheckRadius, 0f, 100f);
				CG.roadConnectDistance = EGL.Slider (CG.roadConnectDistance, 0f, 100f);
				CG.rayCount = EGL.IntSlider (CG.rayCount, 1, 32);
				GL.EndVertical ();
				GL.EndHorizontal ();

				GL.Label ("Advanced Settings for L-system Component", EditorStyles.centeredGreyMiniLabel);
				EGL.HelpBox ("Low values correspond to higher priority.", MessageType.Info);
				GL.BeginHorizontal ();
				GL.BeginVertical ();
				GL.Label ("Street - Priority");
				GL.Label ("Highway - Priority");
				GL.EndVertical ();
				GL.BeginVertical ();
				CG.streetPriority = EGL.IntSlider (CG.streetPriority, 1, 5);
				CG.highwayPriority = EGL.IntSlider (CG.highwayPriority, 1, 5);
				GL.EndVertical ();
				GL.EndHorizontal ();

				GL.Label ("Advanced Settings for Growth Rules", EditorStyles.centeredGreyMiniLabel);
				GL.BeginHorizontal ();
				GL.BeginVertical ();
				GL.Label ("Street - Straight Angle");
				GL.Label ("Street - Branch  Angle");
				GL.Space (10);
				GL.Label ("Highway - Branch Prob");
				GL.Label ("Highway - Straight Angle");
				GL.Label ("Highway - Branch Angle");
				GL.EndVertical ();
				GL.BeginVertical ();
				CG.streetStraightAngle = EGL.Slider (CG.streetStraightAngle, 0f, 90f);
				CG.streetBranchAngle = EGL.Slider (CG.streetBranchAngle, 0f, 90f);
				GL.Space (10);
				CG.highwayBranchProb = EGL.Slider (CG.highwayBranchProb, 0f, 1f);
				CG.highwayStraightAngle = EGL.Slider (CG.highwayStraightAngle, 0f, 90f);
				CG.highwayBranchAngle = EGL.Slider (CG.highwayBranchAngle, 0f, 90f);
				GL.EndVertical ();
				GL.EndHorizontal ();
			}
			if (roadMapGenerated) {
				GL.BeginHorizontal ("Box");
				GL.FlexibleSpace ();
				CG.showPop = EGL.ToggleLeft ("Preview Pop Map", CG.showPop);
				GL.FlexibleSpace ();
				CG.showGrowth = EGL.ToggleLeft ("Preview Growth Map", CG.showGrowth);
				GL.FlexibleSpace ();
				GL.EndHorizontal ();

				if (DebugMode) {
					showPreviewGUI ();
				}
			}
			EGL.HelpBox ("The 'Generate Road Map' button may take several tries to generate road map due to the random nature of the algorithm.", MessageType.Info);
			GL.BeginHorizontal ();
			if (GL.Button ("Generate Road Map")) {
				GameObject roadMap = GameObject.Find ("RoadMap");
				GameObject nodes = GameObject.Find ("Nodes");


				if (roadMap != null) {
					roadMap.SetActive (true);
				}
				if (nodes != null) {
					nodes.SetActive (true);
				}
				generator.generateRoadMap ();
				roadMapGenerated = true;
				roadMeshGenerated = false;
			}

			EG.BeginDisabledGroup (!roadMapGenerated);
			if (GL.Button ("Generate Road Meshes & Blocks")) {
				generator.generateRoadMeshes ();
				generator.generateBlocks ();
				roadMeshGenerated = true;
			}
			EG.EndDisabledGroup ();
			GL.EndHorizontal ();

			EG.BeginDisabledGroup (!roadMeshGenerated);
			if (GL.Button ("Save and Proceed")) {
				showRoadMapUI = false;
				showRoadMapAdvanced = false;
				showBuildingUI = true;
				GameObject roadMap = GameObject.Find ("RoadMap");
				if (roadMap != null) {
					roadMap.SetActive (false);
				}
				GameObject nodes = GameObject.Find ("Nodes");
				if (nodes != null) {
					nodes.SetActive (false);
				}
				roadmapLabel = "4. Road Map Generation - COMPLETED ✔";
			}
			EG.EndDisabledGroup ();
                
		} 
		EG.EndDisabledGroup ();
	}

	void showPreviewGUI ()
	{

		GL.BeginVertical ("Box");
		GL.Label ("Showing first: " + (int)PreviewRoads.NrRoads + " roads");

		GL.BeginHorizontal ();
		{
			if (GL.Button ("|<", GL.Height (30))) {
				m_PlaybackModifier = 0f;
				PreviewRoads.NrRoads = 0;
				SceneView.RepaintAll ();
			}

			if (GL.Button ("<", GL.Height (30))) {
				m_PlaybackModifier = 0f;
				PreviewRoads.NrRoads -= 1;
			}

			if (m_PlaybackModifier == 0) {
				if (GL.Button ("< Play", GL.Height (30))) {
					m_PlaybackModifier = -2f;
				}
				if (GL.Button ("Play >", GL.Height (30))) {
					m_PlaybackModifier = 2f;
				}
			} else {
				if (GL.Button ("||", GL.Height (30))) {
					m_PlaybackModifier = 0f;
				}
			}

			if (GL.Button (">", GL.Height (30))) {
				m_PlaybackModifier = 0f;
				PreviewRoads.NrRoads += 1;
			}

			if (GL.Button (">|", GL.Height (30))) {
				m_PlaybackModifier = 0f;
				PreviewRoads.NrRoads = CG.nrOfRoads;
				SceneView.RepaintAll ();
			}
		}
		GL.EndHorizontal ();
		if (m_PlaybackModifier == 0) {
			PreviewRoads.NrRoads = EGL.IntSlider ((int)PreviewRoads.NrRoads, 0, CG.nrOfRoads);
		} else {
			EGL.IntSlider ((int)PreviewRoads.NrRoads, 0, CG.nrOfRoads);
		}
		GL.EndHorizontal ();
	}

	void showBuildingGUI ()
	{
		EG.BeginDisabledGroup (!roadMeshGenerated);
		// General Terrain Settings
		showBuildingUI = EGL.Foldout (showBuildingUI, buildingLabel, true);
		if (showBuildingUI) {
            

			/*if (GL.Button("Generate blocks"))
            {
                generator.generateBlocks();
            }*/

			if (GL.Button ("Generate Buildings")) {				
				generator.generateBuildings ();
				buildingLabel = "5. Building Generation - COMPLETED ✔";
			}
			CG.generateWindows = EGL.ToggleLeft ("Generate Windows", CG.generateWindows);
		}
		EG.EndDisabledGroup ();

	}

	void Update ()
	{		
		if (oldShowPop != CG.showPop) {
			if (CityGeneratorUI.DebugMode)
				Debug.Log ("Show Population value changed");
			oldShowPop = CG.showPop;
			generator.repaintTerrain ();
		}

		if (oldShowGrowth != CG.showGrowth) {
			if (CityGeneratorUI.DebugMode)
				Debug.Log ("Show Growth value changed");
			oldShowGrowth = CG.showGrowth;
			generator.repaintTerrain ();
		}

		Repaint ();
	}

	void OnEditorUpdate ()
	{
		if (m_PlaybackModifier != 0f) {
			PreviewRoads.NrRoads += ((Time.realtimeSinceStartup - m_LastTime) * m_PlaybackModifier);

			//If the preview time changes, make sure you Repaint this window so you can see it immediately. Otherwise Unity
			//will only call Repaint if it determines the window needs to be redrawn. For example if you move it
			Repaint ();

			//Since we are previewing data in the SceneView we also want to make sure it is updated each time the preview has changed
			SceneView.RepaintAll ();
		}

		m_LastTime = Time.realtimeSinceStartup;
	}

}
