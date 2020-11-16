using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour {

	public enum TagType { Tag = 0, Layer = 1 };

	[SerializeField]	
	int terrainLayer = -1;

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
	public enum VoronoiType { Linear = 0, Power = 1, Combined = 2, SinPow = 3 };
	public VoronoiType voronoiType = VoronoiType.Linear;


	// MIDPOINT DISPLACEMENT -----------------
	public float mpdMinHeight = -2.0f;
	public float mpdMaxHeight = 2.0f;
	public float mpdRoughness = 2.0f;
	public float mpdHeightDampenerPower = 2.0f;

	// SMOOTH -----------
	public int smoothAmount = 1;

	// SPLATMAPS -----------------------------------
	[System.Serializable]
	public class SplatHeights
	{
		public Texture2D texture = null;
		public float minHeight = 0.1f;
		public float maxHeight = 0.2f;
		public float minSlope = 0;
		public float maxSlope = 90;
		public Vector2 tileOffset = new Vector2(0, 0);
		public Vector2 tileSize = new Vector2(50, 50);
		public float splatOffset = 0.1f;
		public float splatNoiseXScale = 0.01f;
		public float splatNoiseYScale = 0.01f;
		public float splatNoiseScaler = 0.1f;
		public bool remove = false;
	}

	public List<SplatHeights> splatHeights = new List<SplatHeights>() {
		new SplatHeights()
	};


	// VEGETATION ------------------------------------
	[System.Serializable]
	public class Vegetation {
		public GameObject mesh;
		public float minHeight = 0.1f;
		public float maxHeight = 0.2f;
		public float minSlope = 0;
		public float maxSlope = 90;
		public float randomOffset = 5.0f;
		public bool remove = false;
	}

	public List<Vegetation> vegetation = new List<Vegetation>()
	{
		new Vegetation()
	};

	public int maxTrees = 5000;
	public int treeSpacing = 5;



	public Terrain terrain;
	public TerrainData terrainData;

	public float[,] GetHeightMap()
	{
		if (resetTerrain) {
			return new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
		} else {
			return terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
		}
	}

	private List<Vector2> GetNeighbors(Vector2 position, int width, int height)
	{
		List<Vector2> neighbors = new List<Vector2>();

		// all neighbors, from -1 to 1 in x and y
		for (int x = -1; x < 2; x++)
		{
			for (int y = -1; y < 2; y++)
			{
				// don't include self in neighbor list
				if (x == 0 && y == 0) continue;

				Vector2 neighborPosition = new Vector2(
					Mathf.Clamp(position.x + x, 0, width - 1),
					Mathf.Clamp(position.y + y, 0, height - 1)
				);

				if (!neighbors.Contains(neighborPosition)) {
					neighbors.Add(neighborPosition);
				}
			}
		}

		return neighbors;
	}

	public void Smooth()
	{
		// don't want to ever reset when smoothing, since we're always smoothing what was there before
		float [,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
		float smoothProgress = 0;
		EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress);

		for (int smoothIteration = 0; smoothIteration < smoothAmount; smoothIteration++)
		{
			for (int x = 0; x < terrainData.heightmapWidth; x++)
			{
				for (int y = 0; y < terrainData.heightmapHeight; y++)
				{
					float averageHeight = heightMap[x, y];
					List<Vector2> neighbors = GetNeighbors(new Vector2(x, y), terrainData.heightmapWidth, terrainData.heightmapHeight);

					foreach (Vector2 neighbor in neighbors)
					{
						averageHeight += heightMap[(int)neighbor.x, (int)neighbor.y];
					}

					heightMap[x, y] = averageHeight / (neighbors.Count + 1);
				}
			}
			smoothProgress++;
			EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress / smoothAmount);
		}

		terrainData.SetHeights(0, 0, heightMap);
		EditorUtility.ClearProgressBar();
	}

	public void MidPointDisplacement()
	{
		float[,] heightMap = GetHeightMap();

		// mpd algorithm likes power of 2-- 1 line of pixels will not be included (heightmapWidth is power of 2 + 1, e.g. 513)
		int width = terrainData.heightmapWidth - 1;
		int squareSize = width;

		// don't want to use values from inspector directly, or they will get reset by the code in here to something almost 0
		float minHeight = mpdMinHeight;
		float maxHeight = mpdMaxHeight;
		float heightDampener = (float)Mathf.Pow(mpdHeightDampenerPower, -1 * mpdRoughness);

		int cornerX, cornerY;
		int midX, midY;
		int pmidXL, pmidXR, pmidYU, pmidYD;

		// perform algorithm on smaller squares each time, until we have covered the entire mesh
		while (squareSize > 0)
		{
			// step out a whole square to each value (diamond stage)
			for (int x = 0; x < width; x += squareSize)
			{
				for (int y = 0; y < width; y += squareSize)
				{
					cornerX = (x + squareSize);
					cornerY = (y + squareSize);

					midX = (int)(x + squareSize / 2.0f);
					midY = (int)(y + squareSize / 2.0f);

					// set the height of the point in the middle of the square to the average of the height of each corner of the square
					heightMap[midX, midY] = 
						(
							heightMap[x, y] +
							heightMap[cornerX, y] +
							heightMap[x, cornerY] +
							heightMap[cornerX, cornerY]
						)
						/ 4.0f + UnityEngine.Random.Range(minHeight, maxHeight);
				}
			}

			// square stage
			for (int x = 0; x < width; x += squareSize)
			{
				for (int y = 0; y < width; y += squareSize)
				{
					cornerX = (x + squareSize);
					cornerY = (y + squareSize);

					midX = (int)(x + squareSize / 2.0f);
					midY = (int)(y + squareSize / 2.0f);

					pmidXR = (int)(midX + squareSize);
					pmidYU = (int)(midY + squareSize);
					pmidXL = (int)(midX - squareSize);
					pmidYD = (int)(midY - squareSize);

					// if we have pmid coordinates outside of the height map, skip this iteration of the square stage
					if (pmidXL <= 0 || pmidYD <= 0 || pmidXR >= width - 1 || pmidYU >= width - 1) continue;

					// calculate the square value for the bottom side
					heightMap[midX, y] = 
						(	
							heightMap[midX, midY] +
							heightMap[x, y] +
							heightMap[midX, pmidYD] +
							heightMap[cornerX, y] 
						)
						/ 4.0f + UnityEngine.Random.Range(minHeight, maxHeight);

					// calculate the square value for the left side
					heightMap[x, midY] = 
						(	
							heightMap[x, cornerY] +
							heightMap[pmidXL, midY] +
							heightMap[x, y] +
							heightMap[midX, midY] 
						)
						/ 4.0f + UnityEngine.Random.Range(minHeight, maxHeight);

					// calculate the square value for the top side
					heightMap[midX, cornerY] = 
						(	
							heightMap[midX, pmidYU] +
							heightMap[x, cornerY] +
							heightMap[midX, midY] +
							heightMap[cornerX, cornerY] 
						)
						/ 4.0f + UnityEngine.Random.Range(minHeight, maxHeight);

					// calculate the square value for the right side
					heightMap[cornerX, midY] = 
						(	
							heightMap[cornerX, cornerY] +
							heightMap[midX, midY] +
							heightMap[cornerX, y] +
							heightMap[pmidXR, midY] 
						)
						/ 4.0f + UnityEngine.Random.Range(minHeight, maxHeight);
				}
			}

			squareSize = (int)(squareSize / 2.0f);

			// need to dampen both min and max to make sure we get the rolloff effect
			minHeight *= mpdHeightDampenerPower;
			maxHeight *= mpdHeightDampenerPower;
		}

		terrainData.SetHeights(0, 0, heightMap);
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
					float height;

					// use different algorithm depending on voronoiType selected
					if (voronoiType == VoronoiType.Combined)
					{
						height = peak.z - distanceToPeak * voronoiFalloff - Mathf.Pow(distanceToPeak, voronoiDropoff);
					}
					else if (voronoiType == VoronoiType.Power)
					{
						height = peak.z - Mathf.Pow(distanceToPeak, voronoiDropoff) * voronoiFalloff;
					}
					else if (voronoiType == VoronoiType.SinPow)
					{
						height = peak.z - Mathf.Pow(distanceToPeak * 3, voronoiFalloff) - Mathf.Sin(distanceToPeak * 2 * Mathf.PI) / voronoiDropoff;
					}
					else {
						// VoronoiType.Linear
						height = peak.z - distanceToPeak * voronoiFalloff;
					}


					// set a height to the greater of: 1. the previous height, and 2. what we just calculated
					heightMap[x, y] = Mathf.Max(height, heightMap[x,y]);
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	// todo: missing height scale?
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

	public void AddNewSplatHeight()
	{
		splatHeights.Add(new SplatHeights());
	}

	public void RemoveSplatHeight()
	{
		List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
		for (int i = 0; i < splatHeights.Count; i++)
		{
			if (!splatHeights[i].remove)
			{
				keptSplatHeights.Add(splatHeights[i]);
			}
		}
		if (keptSplatHeights.Count == 0) // don't want to keep any
		{
			keptSplatHeights.Add(splatHeights[0]); // add at least 1
		}
		splatHeights = keptSplatHeights;	
	}

	public void ApplySplatMaps()
	{
		SplatPrototype[] newSplatPrototypes;
		newSplatPrototypes = new SplatPrototype[splatHeights.Count];
		int splatIndex = 0;
		foreach (SplatHeights sh in splatHeights)
		{
			newSplatPrototypes[splatIndex] = new SplatPrototype();
			newSplatPrototypes[splatIndex].texture = sh.texture;
			newSplatPrototypes[splatIndex].tileOffset = sh.tileOffset;
			newSplatPrototypes[splatIndex].tileSize = sh.tileSize;
			newSplatPrototypes[splatIndex].texture.Apply(true);
			splatIndex++;
		}
		terrainData.splatPrototypes = newSplatPrototypes;

		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
		float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

		for (int x = 0; x < terrainData.alphamapWidth; x++)
		{
			for (int y = 0; y < terrainData.alphamapHeight; y++)
			{
				float[] splat = new float[terrainData.alphamapLayers];
				for (int i = 0; i < splatHeights.Count; i++)
				{
					float noise = Mathf.PerlinNoise(
						x * splatHeights[i].splatNoiseXScale,
						y * splatHeights[i].splatNoiseYScale
					) * splatHeights[i].splatNoiseScaler;
					float offset = splatHeights[i].splatOffset + noise;
					float thisHeightStart = splatHeights[i].minHeight - offset;
					float thisHeightStop = splatHeights[i].maxHeight + offset;

					// splat map at 90deg to height map, so we flip x and y. not sure why!
					float steepness = terrainData.GetSteepness(y / (float)terrainData.alphamapHeight, x / (float)terrainData.alphamapWidth);

					bool heightWithinRange = heightMap[x, y] >= thisHeightStart && heightMap[x, y] <= thisHeightStop;
					bool steepnessWithinRange = steepness >= splatHeights[i].minSlope && steepness <= splatHeights[i].maxSlope;
					if (heightWithinRange && steepnessWithinRange)
					{
						splat[i] = 1;
					}
				}
				NormalizeVector(splat);
				for (int j = 0; j < splatHeights.Count; j++)
				{
					splatmapData[x, y, j] = splat[j];
				}
			}
		}
		terrainData.SetAlphamaps(0, 0, splatmapData);
	}

	public void AddNewVegetation()
	{
		vegetation.Add(new Vegetation());
	}

	public void RemoveVegetation()
	{
		List<Vegetation> keptVegetation = new List<Vegetation>();
		for (int i = 0; i < vegetation.Count; i++)
		{
			if (!vegetation[i].remove)
			{
				keptVegetation.Add(vegetation[i]);
			}
		}
		if (keptVegetation.Count == 0) // don't want to keep any
		{
			keptVegetation.Add(vegetation[0]); // add at least 1
		}
		vegetation = keptVegetation;	
	}

	public void ApplyVegetation()
	{
		// any thing that we want to apply multiples of (not necessarily a "tree")
		TreePrototype[] newTreePrototypes = new TreePrototype[vegetation.Count];

		for (int treeIndex = 0; treeIndex < vegetation.Count; treeIndex++)
		{
			newTreePrototypes[treeIndex] = new TreePrototype() {
				prefab = vegetation[treeIndex].mesh
			};
		}

		terrainData.treePrototypes = newTreePrototypes;

		List<TreeInstance> allVegetation = new List<TreeInstance>();

		// now working in world coordinates, not height map coordinates
		for (int x = 0; x < terrainData.size.x; x += treeSpacing)
		{
			for (int z = 0; z < terrainData.size.z; z += treeSpacing)
			{
				for (int treeProtoIndex = 0; treeProtoIndex < terrainData.treePrototypes.Length; treeProtoIndex++)
				{
					// todo: clean up syntax with {}?
					// float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
					TreeInstance instance = new TreeInstance();
					Vegetation tree = vegetation[treeProtoIndex];

					float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;

					// skip if we don't want this tree at this height 
					if (thisHeight > tree.maxHeight || thisHeight < tree.minHeight) continue;
			
					instance.position = new Vector3(
						(x + UnityEngine.Random.Range(-tree.randomOffset, tree.randomOffset))/ terrainData.size.x,
						terrainData.GetHeight(x, z) / terrainData.size.y,
						(z + UnityEngine.Random.Range(-tree.randomOffset, tree.randomOffset)) / terrainData.size.z
					);
					instance.rotation = UnityEngine.Random.Range(0, 360);
					instance.prototypeIndex = treeProtoIndex;
					instance.color = Color.white;
					instance.lightmapColor = Color.white;
					instance.heightScale = 0.95f;
					instance.widthScale = 0.95f;

					allVegetation.Add(instance);

					// todo: find alternative to goto
					if (allVegetation.Count >= maxTrees) goto TREESDONE;
				}
			}
		}

		TREESDONE:
			terrainData.treeInstances = allVegetation.ToArray();
	}

	void NormalizeVector(float[] vector)
	{
		float total = 0;
		for (int i = 0; i < vector.Length; i++)
		{
			total += vector[i];
		}

		for (int i = 0; i < vector.Length; i++)
		{
			vector[i] /= total;
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

		AddTag(tagsProp, "Terrain", TagType.Tag);
		AddTag(tagsProp, "Cloud", TagType.Tag);
		AddTag(tagsProp, "Shore", TagType.Tag);

		// apply tag changes to tag database
		tagManager.ApplyModifiedProperties();

		SerializedProperty layerProp = tagManager.FindProperty("layers");
		terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
		tagManager.ApplyModifiedProperties();

		// apply tag and layer
		this.gameObject.tag = "Terrain";
		this.gameObject.layer = terrainLayer;
	}

	int AddTag(SerializedProperty tagsProp, string newTag, TagType tType)
	{
		bool found = false;

		// ensure the tag doesn't already exist
		for (int i = 0; i < tagsProp.arraySize; i++)
		{
			SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
			if (t.stringValue.Equals(newTag)) {
				found = true;
				return i;
			}
		}

		// add new tag
		if (!found && tType == TagType.Tag)
		{
			tagsProp.InsertArrayElementAtIndex(0);
			SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
			newTagProp.stringValue = newTag;
		}

		// add new layer
		else if (!found && tType == TagType.Layer)
		{
			for (int i = 8; i < tagsProp.arraySize; i++)
			{
				SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(i);
				
				// add layer in next empty slot
				if (newLayer.stringValue == "")
				{
					newLayer.stringValue = newTag;
					return i;
				}
			}
		}

		// not found and wasn't able to create... perhaps all layer slots are used?
		return -1;
	}
}
