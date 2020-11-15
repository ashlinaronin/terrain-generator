using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils {

	public static float fractalBrownianMotion(
		float x,
		float y,
		int octave,
		float persistence,
		float frequencyMultiplier
	)
	{
		float total = 0;
		float frequency = 1;
		float amplitude = 1;
		float maxValue = 0;
		for (int i = 0; i < octave; i++)
		{
			total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
			maxValue += amplitude;
			amplitude *= persistence;
			frequency *= frequencyMultiplier;
		}

		return total / maxValue;
	}

	public static float Map(float value, float originalMin, float originalMax, float targetMin, float targetMax)
	{
		return (value - originalMin) * (targetMax - targetMin) / (originalMax - originalMin) + targetMin;
	}

}
