using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour {

	public Vector2 randomHeightRange = new Vector2(0, 0.1f);
	public Texture2D heightMapImage;
	public Vector3 heightMapScale = new Vector3(1, 1, 1);

	public bool resetTerrain = true;

	// PERLIN NOISE --------------
	// TODO: why are these separate floats when the other scale and range variables use Vector2/3?
	public float perlinXScale = 0.01f;
	public float perlinYScale = 0.01f;
	public int perlinOffsetX = 0;
	public int perlinOffsetY = 0;

	public int perlinOctaves = 3;
	public float perlinPersistence = 8;
	public float perlinHeightScale = 0.09f;
	public float perlinFrequencyMultiplier = 2;

	// MULTIPLE PERLIN NOISE ----------------
	[System.Serializable]
	public class PerlinParameters
	{
		public float mPerlinXScale = 0.01f;
		public float mPerlinYScale = 0.01f;
		public int mPerlinOctaves = 3;
		public float mPerlinPersistence = 8;
		public float mPerlinFrequencyMultiplier = 2;
		public float mPerlinHeightScale = 0.09f;
		public int mPerlinOffsetX = 0;
		public int mPerlinOffsetY = 0;
		public bool remove = false;
	}

	public List<PerlinParameters> perlinParameters = new List<PerlinParameters>() { new PerlinParameters() };


	// VORONOI ------------------------
	public int voronoiPeakCount = 3;
	public float voronoiFalloff = 0.2f;
	public float voronoiDropoff = 0.6f;
	public float voronoiMinHeight = 0.25f;
	public float voronoiMaxHeight = 0.5f;

	public Terrain terrain;
	public TerrainData terrainData;

	float[,] GetHeightMap()
	{
		if (resetTerrain) {
			return new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
		} else {
			return terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
		}
	}

	public void Voronoi()
	{
		float[,] heightMap = GetHeightMap();
		Vector3[] peaks = new Vector3[voronoiPeakCount];
		
		for (int i = 0; i < peaks.Count(); i++)
		{
			float x = UnityEngine.Random.Range(0, terrainData.heightmapWidth);
			float y = UnityEngine.Random.Range(0, terrainData.heightmapHeight);
			float height = UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight);

			peaks[i] = new Vector3(x, y, height);
			heightMap[(int)x, (int)y] = Mathf.Max(height, heightMap[(int)x, (int)y]);
		}

		float maxDistance = Vector2.Distance(new Vector2Int(0, 0), new Vector2Int(terrainData.heightmapWidth, terrainData.heightmapHeight));

		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				Vector2Int currentLocation = new Vector2Int(x, y);
				foreach (Vector3 peak in peaks)
				{
					// don't process if we're at the peak location
					Vector2Int peakLocation = new Vector2Int((int)peak.x, (int)peak.y);
					if (peakLocation.Equals(currentLocation)) break;

					float distanceToPeak = Vector2.Distance(peak, currentLocation) / maxDistance;
					float height = peak.z - (distanceToPeak * voronoiFalloff) - Mathf.Pow(distanceToPeak, voronoiDropoff);

					// set a height to the greater of: 1. the previous height, and 2. what we just calculated
					heightMap[x, y] = Mathf.Max(height, heightMap[x,y]);
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	public void Perlin()
	{
		float[,] heightMap = GetHeightMap();
		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				heightMap[x, y] += Utils.fractalBrownianMotion(
					(x + perlinOffsetX) * perlinXScale,
					(y + perlinOffsetY) * perlinYScale,
					perlinOctaves,
					perlinPersistence,
					perlinFrequencyMultiplier
				);
			}
			terrainData.SetHeights(0, 0, heightMap);
		}
	}

	public void MultiplePerlin()
	{
		float[,] heightMap = GetHeightMap();
		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				foreach (PerlinParameters p in perlinParameters)
				{
					heightMap[x, y] += Utils.fractalBrownianMotion(
						(x + p.mPerlinOffsetX) * p.mPerlinXScale,
						(y + p.mPerlinOffsetY) * p.mPerlinYScale,
						p.mPerlinOctaves,
						p.mPerlinPersistence,
						p.mPerlinFrequencyMultiplier
					)  * p.mPerlinHeightScale;
				}
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	public void AddNewPerlin() {
		perlinParameters.Add(new PerlinParameters());
	}

	public void RemovePerlin()
	{
		List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();
		for (int i = 0; i < perlinParameters.Count; i++)
		{
			if (!perlinParameters[i].remove)
			{
				keptPerlinParameters.Add(perlinParameters[i]);
			}
		}
		if (keptPerlinParameters.Count == 0) // don't want to keep any
		{
			keptPerlinParameters.Add(perlinParameters[0]); // add at least 1
		}
		perlinParameters = keptPerlinParameters;
	}

	public void RandomTerrain()
	{
		float[,] heightMap = GetHeightMap();
		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				heightMap[x, y] += UnityEngine.Random.Range(randomHeightRange[0], randomHeightRange[1]);
			}
			terrainData.SetHeights(0, 0, heightMap);
		}
	}

	public void LoadTexture()
	{
		float[,] heightMap = GetHeightMap();

		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				heightMap[x, y] += heightMapImage.GetPixel(
					(int)(x * heightMapScale.x),
					(int)(y * heightMapScale.z)
				).grayscale * heightMapScale.y;
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	public void ResetTerrain()
	{
		float[,] heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				heightMap[x, y] = 0;
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	void OnEnable()
	{
		Debug.Log("Initializing Terrain Data");
		terrain = this.GetComponent<Terrain>();
		terrainData = terrain.terrainData;
	}

	void Awake()
	{
		SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		SerializedProperty tagsProp = tagManager.FindProperty("tags");

		AddTag(tagsProp, "Terrain");
		AddTag(tagsProp, "Cloud");
		AddTag(tagsProp, "Shore");

		// apply tag changes to tag database
		tagManager.ApplyModifiedProperties();

		// tag this object
		this.gameObject.tag = "Terrain";
	}

	void AddTag(SerializedProperty tagsProp, string newTag)
	{
		bool found = false;

		// ensure the tag doesn't already exist
		for (int i = 0; i < tagsProp.arraySize; i++)
		{
			SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
			if (t.stringValue.Equals(newTag)) { found = true; break;}
		}

		// add your new tag
		if (!found)
		{
			tagsProp.InsertArrayElementAtIndex(0);
			SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
			newTagProp.stringValue = newTag;
		}
	}
}
