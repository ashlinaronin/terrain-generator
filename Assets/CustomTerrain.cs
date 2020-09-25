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

	public Terrain terrain;
	public TerrainData terrainData;

	public void Perlin()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
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

	public void RandomTerrain()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
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
		float[,] heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];

		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				heightMap[x, y] = heightMapImage.GetPixel(
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
