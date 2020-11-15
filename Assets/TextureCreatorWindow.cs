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

	float brightness = 0.5f;
	float contrast = 0.5f;


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

		int viewWidth = (int)(EditorGUIUtility.currentViewWidth - 100);

		perlinXScale = EditorGUILayout.Slider("X Scale", perlinXScale, 0, 0.1f);
		perlinYScale = EditorGUILayout.Slider("Y Scale", perlinYScale, 0, 0.1f);
		perlinOctaves = EditorGUILayout.IntSlider("Octaves", perlinOctaves, 1, 10);
		perlinPersistence = EditorGUILayout.Slider("Persistence", perlinPersistence, 1, 10);
		perlinHeightScale = EditorGUILayout.Slider("Height Scale", perlinHeightScale, 0, 1);
		perlinOffsetX = EditorGUILayout.IntSlider("Offset X", perlinOffsetX, 0, 10000);
		perlinOffsetY = EditorGUILayout.IntSlider("Offset Y", perlinOffsetY, 0, 10000);
		brightness = EditorGUILayout.Slider("Brightness", brightness, 0, 2);
		contrast = EditorGUILayout.Slider("Contrast", contrast, 0, 2);
		alphaToggle = EditorGUILayout.Toggle("Alpha?", alphaToggle);
		mapToggle = EditorGUILayout.Toggle("Map?", mapToggle);
		seamlessToggle = EditorGUILayout.Toggle("Seamless", seamlessToggle);
	
		float minColor = 1;
		float maxColor = 0;

		// begin centered row
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Generate", GUILayout.Width(viewWidth)))
		{
			int width = 513;
			int height = 513;
			float pValue;
			Color pixelColor = Color.white;
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					if (seamlessToggle)
					{
						float u = (float)x / (float)width;
						float v = (float)y / (float)height;
						// these each correspond to one pixel in a 2x2 grid with the current pixel at the bottom left
						float noise00 = Utils.fractalBrownianMotion(
							(x + perlinOffsetX) * perlinXScale,
							(y + perlinOffsetY) * perlinYScale,
							perlinOctaves,
							perlinPersistence,
							2 // todo: is this a good frequencyMultiplier?
						) * perlinHeightScale;
						float noise01 = Utils.fractalBrownianMotion(
							(x + perlinOffsetX) * perlinXScale,
							(y + perlinOffsetY + height) * perlinYScale,
							perlinOctaves,
							perlinPersistence,
							2
						) * perlinHeightScale;
						float noise10 = Utils.fractalBrownianMotion(
							(x + perlinOffsetX + width) * perlinXScale,
							(y + perlinOffsetY) * perlinYScale,
							perlinOctaves,
							perlinPersistence,
							2
						) * perlinHeightScale;
						float noise11 = Utils.fractalBrownianMotion(
							(x + perlinOffsetX + width) * perlinXScale,
							(y + perlinOffsetY + height) * perlinYScale,
							perlinOctaves,
							perlinPersistence,
							2
						) * perlinHeightScale;

						// all the magic of the seamlessness!
						// these factors cancel out the current coords, e.g. 0 * 0 * noise0 = 0
						float noiseTotal =
							(u * v * noise00) +
							(u * (1 - v) * noise01) +
							((1 - u) * v * noise10) +
							((1 - u) * (1 - v) * noise11);

						// create a color pixel and then turn it into grayscale?
						float value = (int)(256 * noiseTotal) + 50;
						float r = Mathf.Clamp((int)noise00, 0, 255); // this is the current pixel
						float g = Mathf.Clamp(value, 0, 255); // this is the noise of all the surrounding pixels
						float b = Mathf.Clamp(value + 50, 0, 255); // likewise, + 50
						pValue = (r + g + b) / (3 * 255.0f);
					}
					else
					{
						pValue = Utils.fractalBrownianMotion(
							(x + perlinOffsetX) * perlinXScale,
							(y + perlinOffsetY) * perlinYScale,
							perlinOctaves,
							perlinPersistence,
							2 // todo: is this a good frequencyMultiplier?
						) * perlinHeightScale;
					}

					// common graphics approach to integrating contrast and brightness with existing value
					float colorValue = contrast * (pValue - 0.5f) + 0.5f * brightness;

					if (minColor > colorValue) minColor = colorValue;
					if (maxColor < colorValue) maxColor = colorValue;

					pixelColor = new Color(colorValue, colorValue, colorValue, alphaToggle ? colorValue : 1);
					pTexture.SetPixel(x, y, pixelColor);
				} 
			}

			if (mapToggle)
			{
				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						pixelColor = pTexture.GetPixel(x, y);
						float colorValue = pixelColor.r; // doesn't matter which one we use, any will do
						colorValue = Utils.Map(colorValue, minColor, maxColor, 0, 1);
						pixelColor.r = colorValue;
						pixelColor.g = colorValue;
						pixelColor.b = colorValue;
						pTexture.SetPixel(x, y, pixelColor);
					}
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
		GUILayout.Label(pTexture, GUILayout.Width(viewWidth), GUILayout.Height(viewWidth));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		// end centered row

		// begin centered row
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Save", GUILayout.Width(viewWidth)))
		{
			byte[] bytes = pTexture.EncodeToPNG();
			System.IO.Directory.CreateDirectory(Application.dataPath + "/SavedTextures"); // asset folder
			File.WriteAllBytes(Application.dataPath + "/SavedTextures/" + filename + ".png", bytes);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		// end centered row
	}
}
