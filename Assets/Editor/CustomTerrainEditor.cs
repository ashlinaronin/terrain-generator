using UnityEngine;
using UnityEditor;
using EditorGUITable;

[CustomEditor(typeof(CustomTerrain))]
[CanEditMultipleObjects]
public class CustomTerrainEditor : Editor {

	// properties ---------------------
	SerializedProperty randomHeightRange;
	SerializedProperty voronoiHeightRange;
	SerializedProperty heightMapScale;
	SerializedProperty heightMapImage;
	SerializedProperty perlinXScale;
	SerializedProperty perlinYScale;
	SerializedProperty perlinOffsetX;
	SerializedProperty perlinOffsetY;
	SerializedProperty perlinOctaves;
	SerializedProperty perlinPersistence;
	SerializedProperty perlinFrequencyMultiplier;
	SerializedProperty perlinHeightScale;
	SerializedProperty resetTerrain;
	GUITableState perlinParameterTable;
	SerializedProperty perlinParameters;
	SerializedProperty voronoiPeakCount;
	SerializedProperty voronoiFalloff;
	SerializedProperty voronoiDropoff;
	SerializedProperty voronoiMinHeight;
	SerializedProperty voronoiMaxHeight;
	SerializedProperty voronoiType;
	SerializedProperty mpdMinHeight;
	SerializedProperty mpdMaxHeight;
	SerializedProperty mpdRoughness;
	SerializedProperty mpdHeightDampenerPower;
	SerializedProperty smoothAmount;
	GUITableState splatMapTable;
	SerializedProperty splatHeights;

	GUITableState vegetationTable;
	SerializedProperty vegetation;
	SerializedProperty maxTrees;
	SerializedProperty treeSpacing;

	GUITableState detailsTable;
	SerializedProperty details;
	SerializedProperty maxDetails;
	SerializedProperty detailSpacing;

	SerializedProperty waterHeight;
	SerializedProperty waterGameObject;
	SerializedProperty shorelineMaterial;


	Texture2D heightMapTexture;


	// fold outs ----------------------
	bool showRandom = false;
	bool showLoadHeights = false;
	bool showPerlin = false;
	bool showMultiplePerlin = false;
	bool showVoronoi = false;
	bool showMidPointDisplacement = false;
	bool showSmooth = false;
	bool showSplatMaps = false;
	bool showVegetation = false;
	bool showDetails = false;
	bool showWater = false;
	bool showHeightMap = false;

	void OnEnable()
	{
		// todo: can we make this easier?
		randomHeightRange = serializedObject.FindProperty("randomHeightRange");
		voronoiHeightRange = serializedObject.FindProperty("voronoiHeightRange");
		heightMapScale = serializedObject.FindProperty("heightMapScale");
		heightMapImage = serializedObject.FindProperty("heightMapImage");
		perlinXScale = serializedObject.FindProperty("perlinXScale");
		perlinYScale = serializedObject.FindProperty("perlinYScale");
		perlinOffsetX = serializedObject.FindProperty("perlinOffsetX");
		perlinOffsetY = serializedObject.FindProperty("perlinOffsetY");
		perlinOctaves = serializedObject.FindProperty("perlinOctaves");
		perlinPersistence = serializedObject.FindProperty("perlinPersistence");
		perlinFrequencyMultiplier = serializedObject.FindProperty("perlinFrequencyMultiplier");
		perlinHeightScale = serializedObject.FindProperty("perlinHeightScale");
		resetTerrain = serializedObject.FindProperty("resetTerrain");
		perlinParameterTable = new GUITableState("perlinParametersTable");
		perlinParameters = serializedObject.FindProperty("perlinParameters");
		voronoiPeakCount = serializedObject.FindProperty("voronoiPeakCount");
		voronoiFalloff = serializedObject.FindProperty("voronoiFalloff");
		voronoiDropoff = serializedObject.FindProperty("voronoiDropoff");
		voronoiMinHeight = serializedObject.FindProperty("voronoiMinHeight");
		voronoiMaxHeight = serializedObject.FindProperty("voronoiMaxHeight");
		voronoiType = serializedObject.FindProperty("voronoiType");
		mpdMinHeight = serializedObject.FindProperty("mpdMinHeight");
		mpdMaxHeight = serializedObject.FindProperty("mpdMaxHeight");
		mpdRoughness = serializedObject.FindProperty("mpdRoughness");
		mpdHeightDampenerPower = serializedObject.FindProperty("mpdHeightDampenerPower");
		smoothAmount = serializedObject.FindProperty("smoothAmount");
		splatMapTable = new GUITableState("splatMapTable");
		splatHeights = serializedObject.FindProperty("splatHeights");
		vegetationTable = new GUITableState("vegetationTable");
		vegetation = serializedObject.FindProperty("vegetation");
		maxTrees = serializedObject.FindProperty("maxTrees");
		treeSpacing = serializedObject.FindProperty("treeSpacing");
		detailsTable = new GUITableState("detailsTable");
		details = serializedObject.FindProperty("details");
		maxDetails = serializedObject.FindProperty("maxDetails");
		detailSpacing = serializedObject.FindProperty("detailSpacing");
		waterHeight = serializedObject.FindProperty("waterHeight");
		waterGameObject = serializedObject.FindProperty("waterGameObject");
		shorelineMaterial = serializedObject.FindProperty("shorelineMaterial");

		CustomTerrain terrain = (CustomTerrain)target;

		// TODO: what happens when height map width or height change?
		heightMapTexture = new Texture2D(terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight, TextureFormat.ARGB32, false);
	}

	Vector2 scrollPos;
	public override void OnInspectorGUI()
	{
		int viewWidth = (int)(EditorGUIUtility.currentViewWidth - 100);

		serializedObject.Update();

		CustomTerrain terrain = (CustomTerrain)target;

		// scrollbar starting code
		Rect r = EditorGUILayout.BeginVertical();
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(r.width), GUILayout.Height(r.height));
		EditorGUI.indentLevel++;

		EditorGUILayout.PropertyField(resetTerrain);

		showRandom = EditorGUILayout.Foldout(showRandom, "Random");
		if (showRandom)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Set Heights Between Random Values", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(randomHeightRange);
			if (GUILayout.Button("Random Heights"))
			{
				terrain.RandomTerrain();
			}
		}
		
		showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");
		if (showLoadHeights)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(heightMapImage);
			EditorGUILayout.PropertyField(heightMapScale);
			if (GUILayout.Button("Load Texture"))
			{
				terrain.LoadTexture();
			}
		}

		showPerlin = EditorGUILayout.Foldout(showPerlin, "Single Perlin Noise");
		if (showPerlin)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Set Heights with Perlin Noise", EditorStyles.boldLabel);
			EditorGUILayout.Slider(perlinXScale, 0, 1, new GUIContent("X Scale"));
			EditorGUILayout.Slider(perlinYScale, 0, 1, new GUIContent("Y Scale"));
			EditorGUILayout.IntSlider(perlinOffsetX, 0, 10000, new GUIContent("X Offset"));
			EditorGUILayout.IntSlider(perlinOffsetY, 0, 10000, new GUIContent("Y Offset"));
			EditorGUILayout.IntSlider(perlinOctaves, 1, 10, new GUIContent("Octaves"));
			EditorGUILayout.Slider(perlinPersistence, 0.1f, 10, new GUIContent("Persistence"));
			EditorGUILayout.Slider(perlinFrequencyMultiplier, 2, 10, new GUIContent("Frequency Multiplier"));
			EditorGUILayout.Slider(perlinHeightScale, 0, 1, new GUIContent("Height Scale"));
			if (GUILayout.Button("Perlin Noise"))
			{
				terrain.Perlin();
			}
		}

		showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multiple Perlin Noise");
		if (showMultiplePerlin)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Multiple Perlin Noise", EditorStyles.boldLabel);
			perlinParameterTable = GUITableLayout.DrawTable(perlinParameterTable, perlinParameters);
			
			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("+"))
			{
				terrain.AddNewPerlin();
			}
			if (GUILayout.Button("-"))
			{
				terrain.RemovePerlin();
			}
			EditorGUILayout.EndHorizontal();
			if (GUILayout.Button("Apply Multiple Perlin"))
			{
				terrain.MultiplePerlin();
			}
		}

		showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");
		if (showVoronoi)
		{
			EditorGUILayout.IntSlider(voronoiPeakCount, 1, 10, new GUIContent("Peak Count"));
			EditorGUILayout.Slider(voronoiFalloff, 0, 10, new GUIContent("Falloff"));
			EditorGUILayout.Slider(voronoiDropoff, 0, 10, new GUIContent("Dropoff"));
			EditorGUILayout.Slider(voronoiMinHeight, 0, 1, new GUIContent("Min Height"));
			EditorGUILayout.Slider(voronoiMaxHeight, 0, 1, new GUIContent("Max Height"));
			EditorGUILayout.PropertyField(voronoiType);
			if (GUILayout.Button("Voronoi"))
			{
				terrain.Voronoi();
			}
		}

		showMidPointDisplacement = EditorGUILayout.Foldout(showMidPointDisplacement, "Mid Point Displacement");
		if (showMidPointDisplacement)
		{
			EditorGUILayout.Slider(mpdMinHeight, -10, 0, new GUIContent("Min Height"));
			EditorGUILayout.Slider(mpdMaxHeight, 0, 10, new GUIContent("Max Height"));
			EditorGUILayout.Slider(mpdRoughness, 0, 5, new GUIContent("Roughness"));
			EditorGUILayout.Slider(mpdHeightDampenerPower, 0, 1, new GUIContent("Height Dampener Power"));
			if (GUILayout.Button("MPD"))
			{
				terrain.MidPointDisplacement();
			}
		}

		showSplatMaps = EditorGUILayout.Foldout(showSplatMaps, "Splat Maps");
		if (showSplatMaps)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Splat Maps", EditorStyles.boldLabel);
			splatMapTable = GUITableLayout.DrawTable(splatMapTable, splatHeights);

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("+"))
			{
				terrain.AddNewSplatHeight();
			}
			if (GUILayout.Button("-"))
			{
				terrain.RemoveSplatHeight();
			}
			EditorGUILayout.EndHorizontal();
			if (GUILayout.Button("Apply SplatMaps"))
			{
				terrain.ApplySplatMaps();
			}
		}

		showVegetation = EditorGUILayout.Foldout(showVegetation, "Vegetation");
		if (showVegetation)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Vegetation", EditorStyles.boldLabel);
			EditorGUILayout.IntSlider(maxTrees, 0, 10000, new GUIContent("Maximum Trees"));
			EditorGUILayout.IntSlider(treeSpacing, 2, 20, new GUIContent("Tree Spacing"));
			vegetationTable = GUITableLayout.DrawTable(vegetationTable, vegetation);

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("+"))
			{
				terrain.AddNewVegetation();
			}
			if (GUILayout.Button("-"))
			{
				terrain.RemoveVegetation();
			}
			EditorGUILayout.EndHorizontal();
			if (GUILayout.Button("Apply Vegetation"))
			{
				terrain.ApplyVegetation();
			}
		}
		
		showDetails = EditorGUILayout.Foldout(showDetails, "Details");
		if (showDetails)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Details", EditorStyles.boldLabel);
			EditorGUILayout.IntSlider(maxDetails, 0, 10000, new GUIContent("Maximum Details"));
			EditorGUILayout.IntSlider(detailSpacing, 1, 20, new GUIContent("Detail Spacing"));
			detailsTable = GUITableLayout.DrawTable(detailsTable, details);

			// sync unity's detail distance with max details value from slider
			// so we can see all the details while developing. may want to dial back if framerate
			// is affected
			terrain.GetComponent<Terrain>().detailObjectDistance = maxDetails.intValue;

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("+"))
			{
				terrain.AddNewDetails();
			}
			if (GUILayout.Button("-"))
			{
				terrain.RemoveDetails();
			}
			EditorGUILayout.EndHorizontal();
			if (GUILayout.Button("Apply Details"))
			{
				terrain.ApplyDetails();
			}
		}

		showWater = EditorGUILayout.Foldout(showWater, "Water");
		if (showWater)
		{
			EditorGUILayout.Slider(waterHeight, 0, 1, new GUIContent("Water Height"));
			EditorGUILayout.PropertyField(waterGameObject);
			if (GUILayout.Button("Apply Water"))
			{
				terrain.ApplyWater();
			}

			EditorGUILayout.PropertyField(shorelineMaterial);
			if (GUILayout.Button("Draw Shoreline"))
			{
				terrain.DrawShoreline();
			}
		}

		showSmooth = EditorGUILayout.Foldout(showSmooth, "Smooth Terrain");
		if (showSmooth)
		{
			EditorGUILayout.IntSlider(smoothAmount, 1, 10, new GUIContent("Smooth Amount"));
			if (GUILayout.Button("Smooth"))
			{
				terrain.Smooth();
			}
		}



		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		if (GUILayout.Button("Reset Terrain"))
		{
			terrain.ResetTerrain();
		}

		showHeightMap = EditorGUILayout.Foldout(showHeightMap, "Height Map");
		if (showHeightMap)
		{
			// begin centered row
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(heightMapTexture, GUILayout.Width(viewWidth), GUILayout.Height(viewWidth));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			// end centered row

			// begin centered row
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Refresh", GUILayout.Width(viewWidth)))
			{
				float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);
				LoadArrayToTexture2D(heightMapTexture, heights);
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			// end centered row
		}

		// scrollbar ending code
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();

		serializedObject.ApplyModifiedProperties();
	}

	void LoadArrayToTexture2D(Texture2D texture, float[,] heightMapArray)
	{
		int width = heightMapArray.GetLength(0);
		int height = heightMapArray.GetLength(1);

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				float grayscaleValue = heightMapArray[x, y];
				Color newColor = new Color(grayscaleValue, grayscaleValue, grayscaleValue, 1);
				heightMapTexture.SetPixel(x, y, newColor);
			}
		}
		
		texture.Apply(false, false);
	}

}

