using UnityEditor;
using UnityEngine;
using System.IO;

public class TextureCreatorWindow : EditorWindow {

	string filename = "myProceduralTexture";
	float perlinXScale;
	float perlinYScale;
	int perlinOctaves;
	float perlinPersistence;
	float perlinHeightScale;
	int perlinOffsetX;
	int perlinOffsetY;
	bool alphaToggle = false;
	bool seamlessToggle = false;
	bool mapToggle = false;
	Texture2D pTexture;


	[MenuItem("Window/TextureCreatorWindow")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(TextureCreatorWindow));
	}

	void OnEnable()
	{
		pTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false);
	}

	void OnGUI()
	{
		GUILayout.Label("Settings", EditorStyles.boldLabel);
		filename = EditorGUILayout.TextField("Texture Name", filename);

		int wSize = (int)(EditorGUIUtility.currentViewWidth - 100);

		perlinXScale = EditorGUILayout.Slider("X Scale", perlinXScale, 0, 0.1f);
		perlinYScale = EditorGUILayout.Slider("Y Scale", perlinYScale, 0, 0.1f);
		perlinOctaves = EditorGUILayout.IntSlider("Octaves", perlinOctaves, 1, 10);
		perlinPersistence = EditorGUILayout.Slider("Persistence", perlinPersistence, 1, 10);
		perlinHeightScale = EditorGUILayout.Slider("Height Scale", perlinHeightScale, 0, 1);
		perlinOffsetX = EditorGUILayout.IntSlider("Offset X", perlinOffsetX, 0, 10000);
		perlinOffsetY = EditorGUILayout.IntSlider("Offset Y", perlinOffsetY, 0, 10000);
		alphaToggle = EditorGUILayout.Toggle("Alpha?", alphaToggle);
		mapToggle = EditorGUILayout.Toggle("Map?", mapToggle);
		seamlessToggle = EditorGUILayout.Toggle("Seamless", seamlessToggle);

		// begin centered row
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Generate", GUILayout.Width(wSize)))
		{
			int width = 513;
			int height = 513;
			float pValue;
			Color pixelColor = Color.white;
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					pValue = Utils.fractalBrownianMotion(
						(x + perlinOffsetX) * perlinXScale,
						(y + perlinOffsetY) * perlinYScale,
						perlinOctaves,
						perlinPersistence,
						2 // todo: is this a good frequencyMultiplier?
					) * perlinHeightScale;

					float colValue = pValue;
					pixelColor = new Color(colValue, colValue, colValue, alphaToggle ? colValue : 1);
					pTexture.SetPixel(x, y, pixelColor);
				} 
			}
			pTexture.Apply(false, false);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		// end centered row

		// begin centered row
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(pTexture, GUILayout.Width(wSize), GUILayout.Height(wSize));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		// end centered row

		// begin centered row
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Save", GUILayout.Width(wSize)))
		{

		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		// end centered row
	}
}
