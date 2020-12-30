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
		public float minScale = 0.5f;
		public float maxScale = 1.0f;
		public float randomOffset = 5.0f;
		public Color color1 = Color.white;
		public Color color2 = Color.white;
		public Color lightmapColor = Color.white;
		public float minRotation = 0;
		public float maxRotation = 360f;
		public float density = 0.5f;
		public bool remove = false;
	}

	public List<Vegetation> vegetation = new List<Vegetation>()
	{
		new Vegetation()
	};

	public int maxTrees = 5000;
	public int treeSpacing = 5;

	// DETAILS -------------------------------------------
	[System.Serializable]
	public class Detail
	{
		public GameObject prototype = null;
		public Texture2D prototypeTexture = null;
		public float minHeight = 0.1f;
		public float maxHeight = 0.2f;
		public float minSlope = 0;
		public float maxSlope = 1;
		public float overlap = 0.01f;
		public float feather = 0.05f;
		public float density = 0.5f;
		public float bendFactor = 0.5f;
		public Color dryColor = Color.white;
		public Color healthyColor = Color.white;
		public Vector2 heightRange = new Vector2(1, 1);
		public Vector2 widthRange = new Vector2(1, 1);
		public float noiseSpread = 0.5f;
		public bool remove = false;
	}

	public List<Detail> details = new List<Detail>()
	{
		new Detail()
	};

	public int maxDetails = 5000;
	public int detailSpacing = 5;


	// WATER ------------------------------------------------
	public float waterHeight = 0.5f;
	public GameObject waterGameObject;
	public Material shorelineMaterial;


	// EROSION ------------------------------------------------
	public enum ErosionType { Rain = 0, Thermal = 1, Tidal = 2, River = 3, Wind = 4 };
	public ErosionType erosionType = ErosionType.Rain;
	public float erosionStrength = 0.1f;
	public float erosionAmount = 0.01f;
	public int springsPerRiver = 5;
	public float solubility = 0.01f;
	public int droplets = 10;
	public int erosionSmoothAmount = 5;


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
					Vegetation tree = vegetation[treeProtoIndex];

					// skip all trees for this position if we are above desired density
					// this will be true more often the lower the density value is
					if (UnityEngine.Random.Range(0.0f, 1.0f) > tree.density) break;

					TreeInstance instance = new TreeInstance();

					float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
					float thisSteepness = terrainData.GetSteepness(x / (float)terrainData.size.x, z / (float)terrainData.size.z);

					// if we don't want this tree at this height or slope, then skip adding it
					if (
						thisHeight > tree.maxHeight ||
						thisHeight < tree.minHeight ||
						thisSteepness > tree.maxSlope ||
						thisSteepness < tree.minSlope
					) continue;
			
					instance.position = new Vector3(
						(x + UnityEngine.Random.Range(-tree.randomOffset, tree.randomOffset))/ terrainData.size.x,
						terrainData.GetHeight(x, z) / terrainData.size.y,
						(z + UnityEngine.Random.Range(-tree.randomOffset, tree.randomOffset)) / terrainData.size.z
					);

					// if this tree is set to be positioned off of the terrain mesh, then skip adding it
					if (
						instance.position.x > 1.0 ||
						instance.position.x < 0.0 ||
						instance.position.z > 1.0 ||
						instance.position.z < 0.0
					)
					{
						continue;
					}

					// raycast to re-position tree more accurately on surface of terrain
					Vector3 treeWorldPosition = new Vector3(
						instance.position.x * terrainData.size.x,
						instance.position.y * terrainData.size.y,
						instance.position.z * terrainData.size.z
					) + this.transform.position;
					RaycastHit hit;
					int layerMask = 1 << terrainLayer;
					if (
						Physics.Raycast(treeWorldPosition, -Vector3.up, out hit, 100, layerMask) ||
						Physics.Raycast(treeWorldPosition, Vector3.up, out hit, 100, layerMask)
					)
					{
						float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
						instance.position = new Vector3(instance.position.x, treeHeight, instance.position.z);
					}
				
					// adjust position to account for resolution of alphamap
					instance.position = new Vector3(
						instance.position.x * terrainData.size.x / terrainData.alphamapWidth,
                        instance.position.y,
                        instance.position.z * terrainData.size.z / terrainData.alphamapHeight
					);

					instance.rotation = UnityEngine.Random.Range(tree.minRotation, tree.maxRotation);
					instance.prototypeIndex = treeProtoIndex;
					instance.color = Color.Lerp(tree.color1, tree.color2, UnityEngine.Random.Range(0.0f, 1.0f));
					instance.lightmapColor = tree.lightmapColor;

					float scale = UnityEngine.Random.Range(-tree.minScale, tree.maxScale);
					instance.heightScale = scale;
					instance.widthScale = scale;

					allVegetation.Add(instance);

					// todo: find alternative to goto
					if (allVegetation.Count >= maxTrees) goto TREESDONE;
				}
			}
		}

		TREESDONE:
			terrainData.treeInstances = allVegetation.ToArray();
	}

	public void AddNewDetails()
	{
		details.Add(new Detail());
	}

	public void RemoveDetails()
	{
		List<Detail> keptDetails = new List<Detail>();
		for (int i = 0; i < details.Count; i++)
		{
			if (!details[i].remove)
			{
				keptDetails.Add(details[i]);
			}
		}
		if (keptDetails.Count == 0) // don't want to keep any
		{
			keptDetails.Add(details[0]); // add at least 1
		}
		details = keptDetails;	
	}

	public void ApplyDetails()
	{
		DetailPrototype[] newDetailPrototypes = new DetailPrototype[details.Count];

		int detailIndex = 0;

		foreach (Detail d in details)
		{
			newDetailPrototypes[detailIndex] = new DetailPrototype();
			newDetailPrototypes[detailIndex].prototype = d.prototype;
			newDetailPrototypes[detailIndex].prototypeTexture = d.prototypeTexture;
			newDetailPrototypes[detailIndex].dryColor = d.dryColor;
			newDetailPrototypes[detailIndex].healthyColor = d.healthyColor;
			newDetailPrototypes[detailIndex].minHeight = d.heightRange[0];
			newDetailPrototypes[detailIndex].maxHeight = d.heightRange[1];
			newDetailPrototypes[detailIndex].minWidth = d.widthRange[0];
			newDetailPrototypes[detailIndex].maxWidth = d.widthRange[1];
			newDetailPrototypes[detailIndex].noiseSpread = d.noiseSpread;

			if (newDetailPrototypes[detailIndex].prototype)
			{
				newDetailPrototypes[detailIndex].usePrototypeMesh = true;
				newDetailPrototypes[detailIndex].renderMode = DetailRenderMode.VertexLit;
			}
			else
			{
				newDetailPrototypes[detailIndex].usePrototypeMesh = false;
				newDetailPrototypes[detailIndex].renderMode = DetailRenderMode.GrassBillboard;
			}

			detailIndex++;

		}
		terrainData.detailPrototypes = newDetailPrototypes;

		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

		for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
		{
			int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];

			for (int x = 0; x < terrainData.detailWidth; x += detailSpacing)
			{
				for (int y = 0; y < terrainData.detailHeight; y += detailSpacing)
				{
					if (UnityEngine.Random.Range(0.0f, 1.0f) > details[i].density) continue;

					int xHM = (int)(x/(float)terrainData.detailWidth * terrainData.heightmapWidth);
					int yHM = (int)(y/(float)terrainData.detailHeight * terrainData.heightmapHeight);

					float thisNoise = Utils.Map(
						Mathf.PerlinNoise(x * details[i].feather, y * details[i].feather),
						0,
						1,
						0.5f,
						1
					);

					float thisHeightStart = details[i].minHeight * thisNoise - details[i].overlap * thisNoise;
					float thisHeightEnd = details[i].maxHeight * thisNoise + details[i].overlap * thisNoise;

					float thisHeight = heightMap[yHM, xHM]; // detail map flips x and y

					// remember-- terrainData heights are in y, so detail's (flipped) y,x = terrainData's x,z :0
					float steepness = terrainData.GetSteepness(xHM / (float)terrainData.size.x, yHM / (float)terrainData.size.z);

					bool withinHeightRange = thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd;
					bool withinSlopeRange = steepness >= details[i].minSlope && steepness <= details[i].maxSlope;
					if (withinHeightRange && withinSlopeRange)
					{
						// x and y are swapped in detail map (rotated at 90deg to terrain data)
						detailMap[y, x] = 1;
					}
				}
			}
			terrainData.SetDetailLayer(0, 0, i, detailMap);
		}
	}

	public void ApplyWater()
	{
		GameObject water = GameObject.Find("water");
		if (!water)
		{
			// instantiate water at terrain position and rotation
			water = Instantiate(waterGameObject, this.transform.position, this.transform.rotation);
			water.name = "water";
		}

		// then re-position and scale based on terrain size
		water.transform.position =
			this.transform.position +
			new Vector3(terrainData.size.x / 2, waterHeight * terrainData.size.y, terrainData.size.z / 2);
		water.transform.localScale = new Vector3(terrainData.size.x, 1, terrainData.size.z);
	}

	public void DrawShoreline()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

		int quadCount = 0;
		// GameObject quads = new GameObject("QUADS");

		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				// find spot on shore
				Vector2 thisLocation = new Vector2(x, y);
				List<Vector2> neighbors = GetNeighbors(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

				foreach (Vector2 neighbor in neighbors)
				{
					if (heightMap[x, y] < waterHeight && heightMap[(int)neighbor.x, (int)neighbor.y] > waterHeight)
					{
						// if (quadCount > 1000)
						// {
						// 	continue;
						// }

						quadCount++;
						// must be a position of the shore (if self and neighbor are above water level)
						GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
						go.transform.localScale *= 20.0f;
						go.transform.position =
							this.transform.position +
							new Vector3(
								y / (float)terrainData.heightmapHeight * terrainData.size.z,
								waterHeight * terrainData.size.y,
								x / (float)terrainData.heightmapWidth * terrainData.size.x
							);

						go.transform.LookAt(
							new Vector3(
								neighbor.y / (float)terrainData.heightmapHeight * terrainData.size.z,
								waterHeight * terrainData.size.y,
								neighbor.x / (float)terrainData.heightmapWidth * terrainData.size.x
							)
						);
						go.transform.Rotate(90, 0, 0);
						go.tag = "Shore";
						// go.transform.parent = quads.transform;
					}
				}
			}
		}

		// todo: is there a more efficient way to do this? keep references when creating?
		GameObject[] shoreQuads = GameObject.FindGameObjectsWithTag("Shore");
		MeshFilter[] meshFilters = new MeshFilter[shoreQuads.Length];
		for (int m = 0; m < shoreQuads.Length; m++)
		{
			meshFilters[m] = shoreQuads[m].GetComponent<MeshFilter>();
		}
		CombineInstance[] combine = new CombineInstance[shoreQuads.Length];

		// todo: why isn't this a for loop?
		int i = 0;
		while (i < meshFilters.Length)
		{
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
			meshFilters[i].gameObject.SetActive(false);
			i++;
		}

		GameObject currentShoreline = GameObject.Find("Shoreline");
		if (currentShoreline)
		{
			DestroyImmediate(currentShoreline);
		}
		GameObject shoreline = new GameObject();
		shoreline.name = "Shoreline";
		shoreline.AddComponent<WaveAnimation>();
		shoreline.transform.position = this.transform.position;
		shoreline.transform.rotation = this.transform.rotation;
		MeshFilter thisMF = shoreline.AddComponent<MeshFilter>();
		thisMF.mesh = new Mesh();

		// todo: dont we have a reference to thisMF above?
		shoreline.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);

		MeshRenderer shorelineRenderer = shoreline.AddComponent<MeshRenderer>();
		shorelineRenderer.sharedMaterial = shorelineMaterial;

		for (int shoreQuadIndex = 0; shoreQuadIndex < shoreQuads.Length; shoreQuadIndex++)
		{
			DestroyImmediate(shoreQuads[shoreQuadIndex]);
		}
	}

	public void Erode()
	{
		// todo: use switch/case?
		if (erosionType == ErosionType.Rain) Rain();
		else if (erosionType == ErosionType.Thermal) Thermal();
		else if (erosionType == ErosionType.Tidal) Tidal();
		else if (erosionType == ErosionType.River) River();
		else if (erosionType == ErosionType.Wind) Wind();

		// smoothAmount = erosionSmoothAmount;
		// Smooth();
	}

	void Rain()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

		for (int i = 0; i < droplets; i++)
		{
			int x = UnityEngine.Random.Range(0, terrainData.heightmapWidth);
			int y = UnityEngine.Random.Range(0, terrainData.heightmapHeight);
			heightMap[x, y] -= erosionStrength;
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	void Thermal()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
		
		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				Vector2 thisLocation = new Vector2(x, y);
				List<Vector2> neighbors = GetNeighbors(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

				foreach (Vector2 neighbor in neighbors)
				{
					if (heightMap[x, y] > heightMap[(int)neighbor.x, (int)neighbor.y] + erosionStrength)
					{
						float currentHeight = heightMap[x, y];
						heightMap[x, y] -= currentHeight * erosionAmount;
						heightMap[(int)neighbor.x, (int)neighbor.y] += currentHeight * erosionAmount;
					}
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	void Tidal()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

		for (int x = 0; x < terrainData.heightmapWidth; x++)
		{
			for (int y = 0; y < terrainData.heightmapHeight; y++)
			{
				// find spot on shore
				Vector2 thisLocation = new Vector2(x, y);
				List<Vector2> neighbors = GetNeighbors(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

				foreach (Vector2 neighbor in neighbors)
				{
					if (heightMap[x, y] < waterHeight && heightMap[(int)neighbor.x, (int)neighbor.y] > waterHeight)
					{
						heightMap[x, y] = waterHeight;
						heightMap[(int)neighbor.x, (int)neighbor.y] = waterHeight;
					}
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	void River()
	{

	}

	void Wind()
	{

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
