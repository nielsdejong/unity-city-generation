using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum WallObjects{
	DOOR, WINDOW
}

public class BuildingGenerator {

	//Objects needed to create the roadmesh
	static GameObject building;
	static GameObject windows;
	static GameObject buildings;

	static MeshFilter mf;
	static MeshRenderer mr;
	static Mesh mesh;

	static float minHeight; //lowest point on the terrain on which the building resides
	static float maxHeight; //highest point on the terrain on which the building resides

	//set up arrays needed for mesh generation
	//GENEREIC ARRAYS
	static List<Vector3> vertices; //vertices of the mesh
	static List<int> baseTri;	//triangle array for base

	//SKYSCRAPER SPECIFIC TRIANGLE ARRAYS
	static List<int> skyFloorTri; //triangle array for floors
	static int[] flatRoofTri;		//triangle array for roof

	//HOUSE SPECIFIC TRIANGLE ARRAYS
	static List<int> houseFrontTri; //triangle array for front of house
	static List<int> houseLeftTri; //triangle array for left of house
	static List<int> houseRightTri; //triangle array for right of house
	static List<int> houseBackTri; //triangle array for back of house
	static List<int> houseRoofTopTri; //triangle array for top of the roof of house
	static List<int> houseRoofSideTri; //triangle array for side of roof of house

	static List<Vector2> uv; //uv array

	static bool dataInitialized = false;
	static List<Material> pointyRoofMaterials;
	static List<Material> flatRoofMaterials;
	static List<Material> houseWallMaterials;
	static List<Material> skyscraperWallMaterials;
	static List<Material> baseMaterials;

	static List<GameObject> doorModels;
	static List<GameObject> windowModels;

	private static void initData(){
		//init
		pointyRoofMaterials = new List<Material> ();
		flatRoofMaterials = new List<Material> ();
		houseWallMaterials = new List<Material> ();
		skyscraperWallMaterials = new List<Material> ();
		baseMaterials = new List<Material> ();

		doorModels = new List<GameObject> ();
		windowModels = new List<GameObject> ();

		//for each of the textures, load them into script
		pointyRoofMaterials.Add(getMaterial("PointyRoofMaterials/roofMat1"));
		pointyRoofMaterials.Add(getMaterial("PointyRoofMaterials/roofMat2"));
		pointyRoofMaterials.Add(getMaterial("PointyRoofMaterials/roofMat3"));

		flatRoofMaterials.Add(getMaterial("FlatRoofMaterials/roofMat1"));
		flatRoofMaterials.Add(getMaterial("FlatRoofMaterials/roofMat2"));
		flatRoofMaterials.Add(getMaterial("FlatRoofMaterials/roofMat3"));

		houseWallMaterials.Add(getMaterial("HouseWallMaterials/wallMat1"));
		houseWallMaterials.Add(getMaterial("HouseWallMaterials/wallMat2"));
		houseWallMaterials.Add(getMaterial("HouseWallMaterials/wallMat3"));

		skyscraperWallMaterials.Add(getMaterial("SkyscraperWallMaterials/wallMat1"));
		skyscraperWallMaterials.Add(getMaterial("SkyscraperWallMaterials/wallMat2"));
		skyscraperWallMaterials.Add(getMaterial("SkyscraperWallMaterials/wallMat3"));

		baseMaterials.Add(getMaterial("BaseMaterials/baseMat1"));
		baseMaterials.Add(getMaterial("BaseMaterials/baseMat2"));
		baseMaterials.Add(getMaterial("BaseMaterials/baseMat3"));

		//https://archive3d.net/?a=download&id=f052734a
		doorModels.Add (getModel ("DoorModels/Door1"));
		//https://archive3d.net/?a=download&id=72010547
		doorModels.Add (getModel ("DoorModels/Door2"));
		//https://archive3d.net/?a=download&id=4c96e458
		doorModels.Add (getModel ("DoorModels/Door3"));

		/*
		//https://archive3d.net/?a=download&id=d09f6285
		windowModels.Add (getModel ("WindowModels/Window1"));
		//https://archive3d.net/?a=download&id=c1bfebc6
		windowModels.Add (getModel ("WindowModels/Window2"));
		//https://archive3d.net/?a=download&id=62722724
		windowModels.Add (getModel ("WindowModels/Window3"));*/


		windowModels.Add (getModel ("WindowModels/Window1Quad")); 
		windowModels.Add (getModel ("WindowModels/Window2Quad"));
		//windowModels.Add (getModel ("WindowModels/Window3"));

		dataInitialized = true;
	}

	/// <summary>
	/// Gets the material at loc (should be in resources)
	/// </summary>
	/// <returns>The material.</returns>
	/// <param name="loc">Location.</param>
	private static  Material getMaterial(string loc){
		Material mat = (Material)Resources.Load (loc) as Material;
		//to be safe
		if (mat == null) {
			Debug.LogError ("Material not found at: " + loc);
		}
		return mat;
	}

	/// <summary>
	/// Gets the model at loc (should be in resources)
	/// </summary>
	/// <returns>The model.</returns>
	/// <param name="loc">Location.</param>
	private static  GameObject getModel(string loc){
		GameObject model = (GameObject)Resources.Load (loc) as GameObject;
		//to be safe
		if (model == null) {
			Debug.LogError ("Model not found at: " + loc);
		}
		return model;
	}

	/// <summary>
	/// Generates the building. Based on 2D coordinates which represent the building outline
	/// </summary>
	/// <param name="points">Points.</param>
	/// <param name="buildingHeight">Building height.</param>
	public static void generateSkyScraper(Vector2[] points, int nrOfFloors){

		if (!dataInitialized) {
			initData ();
		}

        if (buildings == null)
        {
            buildings = new GameObject();
            buildings.name = "Buildings";
        }
			
        if (points.Length < 3) {
			if (CityGeneratorUI.DebugMode) Debug.LogError ("Input points array too small");
			return;
		}

		foreach (Vector2 point in points) {
			if (!CoordinateHelper.validEndPoint (point)) {
				if (CityGeneratorUI.DebugMode)
					Debug.LogError ("SkyScraper cannot be placed");
				return;
			}
		}
		GameObject window = getRandomWindow ();

		//set up new lists/arrays
		vertices = new List<Vector3> ();
		baseTri = new List<int> ();
		skyFloorTri = new List<int> (); 
		uv = new List<Vector2> ();

		//set up game object that represents the mesh
		building = new GameObject ();
        building.transform.parent = buildings.transform;
		building.name = "SkyScraper";

		windows = new GameObject ();
		windows.transform.parent = building.transform;
		windows.name = "Windows";

		mf = building.AddComponent<MeshFilter> ();
		mr = building.AddComponent<MeshRenderer> ();

		Material[] mats = new Material[3];
		mats [0] = baseMaterials [Random.Range (0, baseMaterials.Count)];
		mats [1] = skyscraperWallMaterials [Random.Range (0, skyscraperWallMaterials.Count)];
		mats [2] = flatRoofMaterials [Random.Range (0, flatRoofMaterials.Count)];
		mr.materials = mats; 

		mesh = new Mesh ();
		mesh.name = "BuildingMesh";
		mesh.subMeshCount = 3; //base, floors, roof

		//find the lowest point
		minHeight = float.MaxValue;
		maxHeight = float.MinValue;
		computeMinMaxHeight (points);

		//now loop over the points and build walls
		for(int i = 0; i < points.Length; i++){
			buildBase (points [i], points [(i + 1) % points.Length], minHeight - CityGenerator.baseHeightMargin, maxHeight);
			//generate wall
			skyFloorTri.AddRange(buildWall (points [i], points [(i + 1) % points.Length], maxHeight, nrOfFloors, window));
		}

		float roofHeight = maxHeight + (nrOfFloors * CityGenerator.floorHeight);
		buildFlatRoof (points, roofHeight);

		//finalize the mesh by setting the values
		mesh.vertices = vertices.ToArray ();
	
		//base uses material 0 
		mesh.SetTriangles (baseTri.ToArray(), 0);
		//floors use material 1
		mesh.SetTriangles(skyFloorTri.ToArray(), 1);
		//roof uses material 2
		mesh.SetTriangles(flatRoofTri, 2);

		mesh.RecalculateNormals ();
		mesh.uv = uv.ToArray ();
		mf.mesh = mesh;

		//optimization.. turn off everything related to light
		/*MeshRenderer[] meshRenderers = windows.transform.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer mr in meshRenderers) {
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.receiveShadows = false;
			mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
			mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
		}*/

		MeshCollider mc = building.AddComponent<MeshCollider> ();
		mc.convex = true;
		building.layer = LayerMask.NameToLayer ("Building");

		/*
		//now for performance, merge meshes -> the windows with the skyscraper
		MeshFilter[] meshFilters = building.GetComponentsInChildren<MeshFilter>();
		CombineInstance[] combine = new CombineInstance[meshFilters.Length];

		for (int j = 0; j < meshFilters.Length; j++) {
			combine[j].mesh = meshFilters[j].sharedMesh;
			combine[j].transform = meshFilters[j].transform.localToWorldMatrix;
			meshFilters[j].gameObject.active = false;
		}
		mf.mesh = new Mesh();
		mf.mesh.CombineMeshes(combine);
		building.active = true;*/
	}

    /// <summary>
    /// Generates the house.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="direction">Direction.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="nrOfFloors">Nr of floors.</param>
	public static void generateHouse(Vector2 position, Vector2 direction, float width, float depth, int nrOfFloors) {

		if (!dataInitialized) {
			initData ();
		}

		if (buildings == null)
		{
			buildings = new GameObject();
			buildings.name = "Buildings";
		}

		GameObject window = getRandomWindow ();

        //set up new lists/arrays
        vertices = new List<Vector3>();
        baseTri = new List<int>();
        houseFrontTri = new List<int>();
        houseBackTri = new List<int>();
        houseLeftTri = new List<int>();
        houseRightTri = new List<int>();
        houseRoofTopTri = new List<int>();
        houseRoofSideTri = new List<int>();       
        uv = new List<Vector2>();

		//set up game object that represents the mesh
		building = new GameObject ();
        building.transform.parent = buildings.transform;
		building.name = "House";

		windows = new GameObject ();
		windows.transform.parent = building.transform;
		windows.name = "Windows";

		mf = building.AddComponent<MeshFilter> ();
		mr = building.AddComponent<MeshRenderer> ();

		//get a random wall material
		Material wallMat = houseWallMaterials [Random.Range (0, houseWallMaterials.Count)];
		Material roofMat = pointyRoofMaterials[Random.Range(0, pointyRoofMaterials.Count)];

		//set up materials
		Material[] mats = new Material[7];
		mats[0] = baseMaterials[Random.Range(0, baseMaterials.Count)];	//the base
		mats[1] = wallMat; //the front of the house
		mats[2] = wallMat; //the left side of the house
		mats[3] = wallMat; //the right side of the house
		mats[4] = wallMat; //the back
		mats[5] = roofMat; //top of the roof of the house
		mats[6] = roofMat; //sides of the roof of the house
		mr.materials = mats; 

		mesh = new Mesh ();
		mesh.name = "House";
		mesh.subMeshCount = 7; //base, front, left, right, back, roof top, roof sides

		//compute the points outline of this house
		direction = direction.normalized;
		Vector2 perpenDirection = new Vector2 (-direction.y, direction.x);

		//get the points
		Vector2 frontLeft = position + (direction * (depth / 2)) + (perpenDirection * (width / 2));
		Vector2 frontRight = position + (direction * (depth / 2)) + (-perpenDirection * (width / 2));
		Vector2 backLeft = position - (direction * (depth / 2)) + (perpenDirection * (width / 2));
		Vector2 backRight = position - (direction * (depth / 2)) + (-perpenDirection * (width / 2));

		//turn into array, note the clockwise order!
		Vector2[] points = new Vector2[4]{ frontLeft, frontRight, backRight, backLeft };
		foreach (Vector2 point in points) {
			if (!CoordinateHelper.validEndPoint (point)) {
				if (CityGeneratorUI.DebugMode)
					Debug.LogError ("House cannot be placed");
				return;
			}
		}

		//find the lowest point
		minHeight = float.MaxValue;
		maxHeight = float.MinValue;
		computeMinMaxHeight (points);

		//now loop over the points and build walls  
		for(int i = 0; i < points.Length; i++){
			buildBase (points [i], points [(i + 1) % points.Length], minHeight - CityGenerator.baseHeightMargin, maxHeight);
		}

		//generate the triangle arrays for each wall
		houseFrontTri = buildWall (points [0], points [1], maxHeight, nrOfFloors,  window, true, false);
		houseLeftTri = buildWall (points [3], points [0], maxHeight, nrOfFloors, window);
		houseRightTri = buildWall (points [1], points [2], maxHeight, nrOfFloors, window);
		houseBackTri = buildWall (points [2], points [3], maxHeight, nrOfFloors, window);

		//build the roof
		float roofHeight = maxHeight + (nrOfFloors * CityGenerator.floorHeight);
		buildPointyRoof (points, position, direction, width, roofHeight);

		//finalize the mesh by setting the values
		mesh.vertices = vertices.ToArray ();

		//set submeshes
		//base uses material 0 
		mesh.SetTriangles (baseTri.ToArray(), 0);
		//house front use material 1
		mesh.SetTriangles(houseFrontTri.ToArray(), 1);
		//house left uses material 2
		mesh.SetTriangles(houseLeftTri.ToArray(), 2);
		//house right uses material 3
		mesh.SetTriangles(houseRightTri.ToArray(), 3);
		//house back uses material 4
		mesh.SetTriangles(houseBackTri.ToArray(), 4);
		//roof top uses material 5
		mesh.SetTriangles(houseRoofTopTri.ToArray(), 5);
		//roof side material 6
		mesh.SetTriangles(houseRoofSideTri.ToArray(), 6);

		mesh.RecalculateNormals ();
		mesh.uv = uv.ToArray ();
		mf.mesh = mesh;

		MeshCollider mc = building.AddComponent<MeshCollider> ();
		mc.convex = true;
		building.layer = LayerMask.NameToLayer ("Building");
	}

	/// <summary>
	/// Builds the base.
	/// </summary>
	/// <param name="from">From.</param>
	/// <param name="to">To.</param>
	/// <param name="minHeight">Minimum height.</param>
	/// <param name="maxHeight">Max height.</param>
	private static void buildBase(Vector2 from, Vector2 to, float minHeight, float maxHeight){
		//set up index for vertices of quad
		int botLeftIndex = vertices.Count;
		int botRightIndex = vertices.Count + 1;
		int topLeftIndex = vertices.Count + 2;
		int topRightIndex = vertices.Count + 3;
	
		//botLeft
		vertices.Add(new Vector3(to.x, minHeight, to.y));
		//botRight
		vertices.Add(new Vector3(from.x, minHeight, from.y));
		//topLeft
		vertices.Add(new Vector3 (to.x, maxHeight, to.y));
		//topRight
		vertices.Add(new Vector3(from.x, maxHeight, from.y));

		//  Lower left triangle.
		baseTri.Add (botLeftIndex);
		baseTri.Add (topLeftIndex);
		baseTri.Add (botRightIndex);

		//  Upper right triangle.   
		baseTri.Add (topLeftIndex);
		baseTri.Add (topRightIndex);
		baseTri.Add (botRightIndex);

		uv.Add (new Vector2 (0, 0));
		uv.Add (new Vector2 (1, 0));
		uv.Add (new Vector2 (0, 1));
		uv.Add (new Vector2 (1, 1));
	}

	/// <summary>
	/// Builds a flat polygon based on the terrain between to points and builds a number of floors
	/// </summary>
	/// <param name="from">From.</param>
	/// <param name="to">To.</param>
	/// <param name="height">Height. of the wall in world coordinates</param>
	/// Returns a list containing the triangulation
	private static List<int> buildWall(Vector2 from, Vector2 to, float startHeight, int nrOfFloors, GameObject window, bool buildDoor = false, bool buildWindows = true){
		if (CityGeneratorUI.DebugMode) Debug.Log ("Building wall");

		List<int> triangles = new List<int> ();

		//iterate over number of floors
		for (int i = 0; i < nrOfFloors; i++) {
			//set up the height where this floor starts
			float floorBaseHeight = startHeight + (i * CityGenerator.floorHeight);

			//set up index for vertices of quad
			int botLeftIndex = vertices.Count;
			int botRightIndex = vertices.Count + 1;
			int topLeftIndex = vertices.Count + 2;
			int topRightIndex = vertices.Count + 3;

			//botLeft
			vertices.Add(new Vector3(to.x, floorBaseHeight, to.y));
			//botRight
			vertices.Add(new Vector3(from.x, floorBaseHeight, from.y));
			//topLeft
			vertices.Add(new Vector3 (to.x, floorBaseHeight + CityGenerator.floorHeight, to.y));
			//topRight
			vertices.Add(new Vector3(from.x, floorBaseHeight + CityGenerator.floorHeight, from.y));

			//  Lower left triangle.
			triangles.Add(botLeftIndex);
			triangles.Add (topLeftIndex);
			triangles.Add (botRightIndex);

			//  Upper right triangle.   
			triangles.Add(topLeftIndex);
			triangles.Add (topRightIndex);
			triangles.Add (botRightIndex);

			uv.Add (new Vector2 (0, 0));
			uv.Add (new Vector2 (1, 0));
			uv.Add (new Vector2 (0, 1));
			uv.Add (new Vector2 (1, 1));

			if (buildDoor && i == 0) {
				//build a door and a window next to it (houses)
				//generate windows and possibly doors
				Vector3 startPoint = new Vector3(from.x, maxHeight, from.y);
				Vector3 endPoint = new Vector3(to.x, maxHeight, to.y);
				placeObjOnWall (startPoint, endPoint, 0.15f, 0.5f, 0f, WallObjects.DOOR, true);
				placeObjOnWall (startPoint, endPoint, 0.6f, 0.85f, CityGenerator.floorHeight / 2, WallObjects.WINDOW, false, window);
			} 

		}

		if(buildWindows && CityGenerator.generateWindows){
			//place windows	
			Vector3 to3D = new Vector3 (to.x, maxHeight, to.y);
			Vector3 from3D = new Vector3 (from.x, maxHeight, from.y);
			Vector3 wallDirection = (to3D - from3D);
			float wallLength = (to3D - from3D).magnitude;

			int nrWindows = (int)wallLength / 5;

			Vector3 windowRotation = to3D - from3D;
			windowRotation = Quaternion.AngleAxis (-90, Vector3.up) * windowRotation;

			float stepSize = (wallLength / (float)nrWindows);
			for(float j = Mathf.Max(0.1f, stepSize); j < (0.95f * wallLength); j += stepSize){
				Vector3 startPoint = from3D + (wallDirection.normalized * j);
				placeWindowsOnWall (startPoint, CityGenerator.floorHeight, nrOfFloors, windowRotation, window);
			}				
		}
			
		return triangles;
	}

	private static void buildFlatRoof(Vector2[] roofPoints, float roofHeight){
		//used for the triangulation of the roof
		Triangulator triangulator = new Triangulator(roofPoints);

		flatRoofTri = triangulator.Triangulate ();

		if (CityGeneratorUI.DebugMode) Debug.Log ("Printing triangles");
		//increase the index of every triangle index to make sure we link to the correct vertices
		for(int i = 0; i < flatRoofTri.Length; i++) {
			flatRoofTri[i] += vertices.Count;
			if (CityGeneratorUI.DebugMode) Debug.Log (flatRoofTri[i]);
		}

		// Create the Vector3 vertices
		for (int j = 0; j < roofPoints.Length; j++) {
			vertices.Add (new Vector3(roofPoints[j].x, roofHeight,roofPoints[j].y));
			uv.Add (new Vector2 (0, 1));
		}
	}

	private static void buildPointyRoof(Vector2[] roofPoints, Vector2 position, Vector2 direction, float width, float roofHeight){
		//define height of the pointy roof, randomly.
		float height = Random.Range (2.0f, 5.0f);

		direction = direction.normalized;
		Vector2 perpenDirection = new Vector2 (-direction.y, direction.x);

		//set up vertices for roof
		Vector2 roofTopLeftCoordinate = position + (perpenDirection * (width / 2));
		Vector2 roofTopRightCoordinate = position - (perpenDirection * (width / 2));
		Vector3 roofTopLeftVertex = new Vector3(roofTopLeftCoordinate.x, roofHeight + height, roofTopLeftCoordinate.y);
		Vector3 roofTopRightVertex = new Vector3(roofTopRightCoordinate.x, roofHeight + height, roofTopRightCoordinate.y);
		Vector3 roofMiddleLeftVertex = new Vector3(roofTopLeftCoordinate.x, roofHeight, roofTopLeftCoordinate.y);
		Vector3 roofMiddleRightVertex = new Vector3(roofTopRightCoordinate.x, roofHeight, roofTopRightCoordinate.y);

		//set up index of vertices in vertices array
		int roofFrontLeft = vertices.Count;
		int roofFrontRight = vertices.Count + 1;
		int roofBackRight = vertices.Count + 2;
		int roofBackLeft = vertices.Count + 3;
		int roofTopLeft = vertices.Count + 4;
		int roofTopRight = vertices.Count + 5;
		int roofMiddleLeft = vertices.Count + 6;
		int roofMiddleRight = vertices.Count + 7;

		//add the vertices to the vertices array and set up UVs
		//frontLeft
		vertices.Add (new Vector3(roofPoints[0].x, roofHeight, roofPoints[0].y));
		uv.Add (new Vector2 (0, 0));
		//frontRight
		vertices.Add (new Vector3(roofPoints[1].x, roofHeight, roofPoints[1].y));
		uv.Add (new Vector2 (1, 0));
		//backRight
		vertices.Add (new Vector3(roofPoints[2].x, roofHeight, roofPoints[2].y));
		uv.Add (new Vector2 (1, 0));
		//backLeft
		vertices.Add (new Vector3(roofPoints[3].x, roofHeight, roofPoints[3].y));
		uv.Add (new Vector2 (0, 0));
	
		//roof top vertices
		vertices.Add (roofTopLeftVertex);
		uv.Add (new Vector2 (0, 1));
		vertices.Add (roofTopRightVertex);
		uv.Add (new Vector2 (1, 1));

		//roof middle vertices
		vertices.Add(roofMiddleLeftVertex);
		uv.Add (new Vector2 (1, 0));
		vertices.Add (roofMiddleRightVertex);
		uv.Add (new Vector2 (1, 0));

		//create the top of the roof
		//front slope, bottom right triangle
		houseRoofTopTri.Add(roofFrontLeft);
		houseRoofTopTri.Add(roofFrontRight);
		houseRoofTopTri.Add(roofTopLeft);

		//front slope, top left triangle
		houseRoofTopTri.Add(roofFrontRight);
		houseRoofTopTri.Add(roofTopRight);
		houseRoofTopTri.Add(roofTopLeft);

		//back slope, bottom right triangle
		houseRoofTopTri.Add(roofBackRight);
		houseRoofTopTri.Add(roofBackLeft);
		houseRoofTopTri.Add(roofTopRight);

		//back slope, top left triangle
		houseRoofTopTri.Add(roofBackLeft);
		houseRoofTopTri.Add(roofTopLeft);
		houseRoofTopTri.Add(roofTopRight);

		//create the sides of the roof -----------------------

		//we need to add some vertices again to make sure UV mapping is correct

		int roofSideRightFront = vertices.Count;
		int roofSideRightBack = vertices.Count + 1;
		int roofSideRightTop = vertices.Count + 2;

		//frontRight
		vertices.Add (new Vector3(roofPoints[1].x, roofHeight, roofPoints[1].y));
		uv.Add (new Vector2 (0, 0));
		//backRight
		vertices.Add (new Vector3(roofPoints[2].x, roofHeight, roofPoints[2].y));
		uv.Add (new Vector2 (0, 0));
		//topRight
		vertices.Add (roofTopRightVertex);
		uv.Add (new Vector2 (1, 1));

		//leftFrontTriangle
		houseRoofSideTri.Add(roofFrontLeft);
		houseRoofSideTri.Add(roofTopLeft);
		houseRoofSideTri.Add(roofMiddleLeft);

		//leftBackTriangle
		houseRoofSideTri.Add(roofBackLeft);
		houseRoofSideTri.Add(roofMiddleLeft);
		houseRoofSideTri.Add(roofTopLeft);

		//rightFrontTriangle
		houseRoofSideTri.Add(roofSideRightFront);
		houseRoofSideTri.Add(roofMiddleRight);
		houseRoofSideTri.Add(roofSideRightTop);

		//rightBackTriangle
		houseRoofSideTri.Add(roofSideRightBack);
		houseRoofSideTri.Add(roofSideRightTop);
		houseRoofSideTri.Add(roofMiddleRight);

	}

	private static GameObject getRandomDoor(){
		return doorModels [Random.Range (0, doorModels.Count)];
	}

	private static GameObject getRandomWindow(){
		return windowModels [Random.Range (0, windowModels.Count)];
	}

	private static void placeObjOnWall(Vector3 startPoint, Vector3 endPoint, float frac1, float frac2, float extraHeight, WallObjects wallObject, bool random, GameObject ob = null){
		GameObject obj;
		if (random) {
			obj = (wallObject == WallObjects.DOOR) ? getRandomDoor () : getRandomWindow ();
		} else {
			obj = ob;
		}

		//take a position between 0.2 and 0.8 from the edges of the house
		Vector3 objPosition = Vector3.Lerp(startPoint, endPoint, Random.Range(frac1, frac2));

		//find the rotation 
		Vector3 objRotation = endPoint - startPoint;
		objRotation = Quaternion.AngleAxis (-90, Vector3.up) * objRotation;

		GameObject doorObject = GameObject.Instantiate (obj, objPosition, Quaternion.LookRotation(objRotation), building.transform);
		//move the door up a bit such that it is placed correctly
		doorObject.transform.Translate (new Vector3 (0, extraHeight, 0));
	}

	private static void placeWindowsOnWall(Vector3 startPoint, float heightStepSize, int nrOfSteps, Vector3 windowRotation, GameObject window){
		for (int i = 0; i < nrOfSteps; i++) {
			//take a position between 0.2 and 0.8 from the edges of the house
			Vector3 windowPosition = startPoint + (Vector3.up * i * heightStepSize);

			GameObject windowObject = GameObject.Instantiate (window, windowPosition, Quaternion.LookRotation(windowRotation), windows.transform);
			//move the door up a bit such that it is placed correctly
			windowObject.transform.Translate (new Vector3 (0, CityGenerator.floorHeight / 2, 0));
		}
	}

	/// <summary>
	/// Given a building outline, computes the lowest and highest point on the terrain on which the building resides
	/// </summary>
	/// <param name="points">Points.</param>
	private static void computeMinMaxHeight(Vector2[] points){
		foreach (Vector2 p in points) {
			float height = CoordinateHelper.worldToTerrainHeight (p);
			if (height < minHeight) {
				minHeight = height;
			}
			if (height > maxHeight) {
				maxHeight = height;
			}
		}
	}
}
