using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Assets.Editor.LotsGenerator;

public class CityGenerator
{

    //Instantiate all the generators
    private TerrainGenerator terrGen = new TerrainGenerator();
    private GrowthRuleGenerator growthGen = new GrowthRuleGenerator();
    private PopulationDensityGenerator popGen = new PopulationDensityGenerator();
    private RoadMapGenerator roadMapGen = new RoadMapGenerator();
    //private DistrictGenerator distrGen = new DistrictGenerator();
    private LotsGenerator lotsGen = new LotsGenerator();
    private HousePlacer housePlacer = new HousePlacer();
    //Instantiate the map visualizer
    private MapVisualizer mapVisualizer = new MapVisualizer();

    public static bool terrainGenerated = false; //true when the terrain has been generated
    public static bool popGenerated = false; //true when the population map has been generated
    public static bool growthGenerated = false; //true when the growth map has been generated

    public static Texture2D terrainMap;
    public static Texture2D popMapInput;
    public static Texture2D growthMapInput;

    private static Terrain _terrain = null;
    public static Terrain terrain
    {
        get
        {
            if (_terrain == null)
            {
                _terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
                if (_terrain == null)
                {
                    Debug.LogError("Terrain not found");
                }
            }
            return _terrain;
        }
        set
        {
            _terrain = value;
        }
    }

    //bools specifying if randomization should be used
    public static bool rWater;
    private static GameObject _water = null;
    public static GameObject water
    {
        get
        {
            if (_water == null)
            {
                _water = GameObject.Find("Water(Clone)");
            }
            return _water;
        }
        set
        {
            _water = value;
        }
    }
    public static float[,] popMap;
    public static float[,] growthMap;
    public static List<Vector2> densityPeaks;

    public static int nrOfRoads;
    //CONSTANTS
    public static int mapSize = 512;  //the size of the population/growth map/heightmap/alphamap

    //TERRAIN GENERATION PARAMETERS
    public static int terrainSize = 1024;
    public static int terrainSeed = 0;
    public static float terrainZoom = 0.05f;
    public static float terrainPersistance = 0.45f;
    public static float minHeight = -20f;
    public static float maxHeight = 50f;
    public static int terrainOctaves = 5;
    public static bool showPop = false; //when true, the popmap is visualized on the terrain
    public static bool showGrowth = false; //when true, the growthmap is visualized on the terrain

    //POPULATION MAP GENERATOR PARAMETERS
    public static int popSeed = 0;
    public static int popOctaves = 5;
    public static float popPersistance = 0.0f;
    public static float popZoom = 0.05f;

    //GROWTH MAP GENERATOR PARAMETERS
    public static int growthSeed = 0;
    public static int growthOctaves = 5;
    public static float growthPersistance = 0.45f;
    public static float growthZoom = 0.05f;
    public static float growthBasic = 1f / 3f;
    public static float growthNewYork = 1f / 3f;
    public static float growthParis = 1f / 3f;

    //ROAD MAP GENERATOR PARAMETERS ------------------------------------

    //STREETS
    public static int streetWidth = 5;					            //width of streets.              
	public static int streetPriority = 3;                           //priority of a street being added to the road network.
	public static float streetMinLength = 30f;                      //default length of a street segment.
    public static int streetLookAhead = 2;                       //how many streets ahead should the raycaster look at.
    public static float streetPopThreshold = 0.5f;                     //min population value required to branch off a street.
	public static float streetBranchProb = 1f / 2f;                    //probablity of branching off sideways for a street.
	public static float streetBranchAngle = 90f;                       //default angle at which street side-branching happens.
	public static float streetStraightAngle = 15f;                     //maximum angle by which straight streets can vary.
    public static float streetMaxSlope = 30f / 100f;                      //maximum slope a road

    //HIGHWAYS
    public static int highwayWidth = streetWidth * 2;			    	//width of highways.
	public static int highwayPriority = 1;                          //priority of a highway being added to the road network.
	public static float highwayMinLength = 100f;                    //default length of a highway segment.
    public static int highwayLookAhead = 
        (int)(terrainSize / highwayMinLength);                      //how many highways ahead should the raycaster look at.    
    public static float highwayPopThreshold = 0.4f;                      //min population value required to go straight on a highway.
	public static float highwayBranchProb = 1f / 3f;                     //probablity of branching off sideways for a highway.
	public static float highwayBranchAngle = 30f;                        //default angle at which highway side-branching happens
	public static float highwayStraightAngle = 15f;                      //maximum angle by which the default branch angle for highways can vary.
    public static float highwayMaxSlope = 50f / 100f;                      //maximum slope a road

    //OTHER
    public static int rayCount = 10;                                //number of rays used by growth rule branching
	public static float maxMeshGenerationAngle = 60f;			    //maximum angle allowed between consecutive edges when generating the roadmesh

	//LOCAL CONSTRAINTS PARAMETERS
	public static float maxLigalizationAngle = 20f;			        //max angle by which a road can be changed when trying to legalize it
	public static float minRoadLengthAfterSplit = 15f;		        //minimum road length required after splitting a road
	public static int legalizationAttempts = 15; 	    	        //number of attempts we do at legalizing a road
	public static int minRoadAngle = 30;			    	        //the minimum angle that we want between roads
	public static float nodeCheckRadius = 40f;		    	        //the radius used when finding nearby nodes (road endpoints)
	public static float roadConnectDistance = 25f;                  //the distance used when finding nearby roads 

    //BUILDING GENERATOR PARAMETERS
    public static float floorHeight = 3f;                           //height of a single floor of a building.
	public static float baseHeightMargin = 3f;                      // --- missing information ---
	public static bool generateWindows = false;                     //true if windows have to be generated
	public static float skyscraperPopThreshold = 0.65f;             //population density threshold over which skyscrapers can be placed
    public static float maxHouseWidth = 30f;                        //maximum width of a house

    // BLOCK GENERATOR PARAMETERS:
    public static float maxBlockArea = 5000f;
    public static float minBlockArea = 100f;
    public static float blockShrinkPercentage = 0.3f;

    /// <summary>
    /// Generates the terrain and visualize it.
    /// </summary>
	public void generateTerrain(){
		terrGen.generate();
		terrainGenerated = true;

        // if popmap was already generated we regenerate it in case the terrain size changed
        if (popGenerated)
            popMap = popGen.generate();

        // if growthmap was already generated we regenerate it in case the terrain size changed
        if (growthGenerated)
            growthMap = growthGen.generate();

        if (terrain != null && popMap != null && growthMap != null)
		    repaintTerrain ();
	}

    /// <summary>
    /// Generates the population density map and visualize it.
    /// </summary>
	public void generatePopulationMap(){
		popMap = popGen.generate();
        if (popMap != null)
        {
            popGenerated = true;
            repaintTerrain();
        }
	}

    /// <summary>
    /// Generates the growth rule map and visualize it.
    /// </summary>
    public void generateGrowthRule()
    {
        growthMap = growthGen.generate();
        if (growthMap != null)
        {
            growthGenerated = true;
            repaintTerrain();
        }
    }

    /// <summary>
    /// Generates the road map and visualize it.
    /// </summary>
    public void generateRoadMap()
    {
        roadMapGen.generateRoadMap();
    }

	//retextures the terrain based on the currently selected method
	public void repaintTerrain(){
		if (CityGeneratorUI.DebugMode) Debug.Log ("RepaintTerrain called");
        mapVisualizer.visualizeMap();
		if (CityGeneratorUI.DebugMode) Debug.Log ("RepaintTerrain finished");
	}

    public void generateBlocks()
    {
        lotsGen.generate();
    }

	//TESTING
	public void test(){
		roadMapGen.test ();
	}
	public void test4(){
		roadMapGen.test4 ();
	}

	public void generateRoadMeshes(){
		roadMapGen.generateRoadMesh ();
	}
    public void generateBuildings()
    {
        GameObject.DestroyImmediate(GameObject.Find("Buildings"));

        foreach (Lot lot in lotsGen.lots)
        {
            float pop = lot.getPopulationValue();
            BuildingGenerator.generateSkyScraper(lot.corners.ToArray(),
                Random.Range((int)(pop * 10f), (int)(pop * 10f + 7)));
        }

        GameObject roadMap = RoadVisualizer.roadMap;
        if (roadMap == null)
        {
            Debug.LogError("Roadmap not found, cannot generate houses");
        }
        else
        {
            roadMap.SetActive(true);
            housePlacer.placeHouses();
            roadMap.SetActive(false);
        }

    }

    public void testHouses()
    {
		GameObject roadMap = RoadVisualizer.roadMap;
		if (roadMap == null)
		{
			Debug.LogError("Roadmap not found, cannot generate houses");
		}
		else
		{
			roadMap.SetActive(true);
			housePlacer.placeHouses();
			roadMap.SetActive(false);
		}

        /*Vector2[] buildingPoints = new Vector2[4] { new Vector2(100, 100), new Vector2(100, 120), new Vector2(120, 120), new Vector2(120, 100) };
       // BuildingGenerator.generateSkyScraper(buildingPoints, 15);

        buildingPoints = new Vector2[4] { new Vector2(150, 100), new Vector2(140, 110), new Vector2(160, 135), new Vector2(180, 100) };
       // BuildingGenerator.generateSkyScraper(buildingPoints, 25);

        buildingPoints = new Vector2[6] { new Vector2(200, 80), new Vector2(190, 90), new Vector2(210, 115), new Vector2(240, 130), new Vector2(220, 95), new Vector2(230, 80) };
        //BuildingGenerator.generateSkyScraper(buildingPoints, 20);

        BuildingGenerator.generateHouse(new Vector2(100, 150), new Vector2(1, 0), 15, 10, 1);
        BuildingGenerator.generateHouse(new Vector2(85, 150), new Vector2(0.5f, 0.5f), 10, 8, 1);
        BuildingGenerator.generateHouse(new Vector2(100, 170), new Vector2(0, 1), 15, 12, 1);

        //failsafe test
        BuildingGenerator.generateHouse(new Vector2(-100, 170), new Vector2(0, 1), 15, 12, 1);*/
    }

    public static void restoreDefaults()
    {
        terrainGenerated = false; 
        popGenerated = false; 
        growthGenerated = false;
        showPop = false;
        showGrowth = false;
        terrainMap = null;
        popMapInput = null;
        growthMapInput = null;
    }

}
