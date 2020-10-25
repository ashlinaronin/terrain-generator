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



	// fold outs ----------------------
	bool showRandom = false;
	bool showLoadHeights = false;
	bool showPerlin = false;
	bool showMultiplePerlin = false;
	bool showVoronoi = false;
	bool showMidPointDisplacement = false;

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
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		CustomTerrain terrain = (CustomTerrain)target;

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
			if (GUILayout.Button("MPD"))
			{
				terrain.MidPointDisplacement();
			}
		}

		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		if (GUILayout.Button("Reset Terrain"))
		{
			terrain.ResetTerrain();
		}

		serializedObject.ApplyModifiedProperties();
	}
}
