using UnityEngine;
using UnityEditor;
using EditorGUITable;

[CustomEditor(typeof(CustomTerrain))]
[CanEditMultipleObjects]
public class CustomTerrainEditor : Editor {

	// properties ---------------------
	SerializedProperty randomHeightRange;
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


	// fold outs ----------------------
	bool showRandom = false;
	bool showLoadHeights = false;
	bool showPerlin = false;

	void OnEnable()
	{
		randomHeightRange = serializedObject.FindProperty("randomHeightRange");
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
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		CustomTerrain terrain = (CustomTerrain)target;

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

		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		if (GUILayout.Button("Reset Terrain"))
		{
			terrain.ResetTerrain();
		}

		serializedObject.ApplyModifiedProperties();
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
