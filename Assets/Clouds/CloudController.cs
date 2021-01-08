using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudController : MonoBehaviour {

	ParticleSystem cloudSystem;
	public Color color;
	public Color lining;
	bool painted = false;
	public int numberOfParticles;
	public float minSpeed;
	public float maxSpeed;
	public float distance;
	Vector3 startPosition;
	float speed;

	void Start()
	{
		cloudSystem = this.GetComponent<ParticleSystem>();
		Spawn();
	}

	void Spawn()
	{
		// extend the range of the scale on either side of the cloud manager center
		float xpos = UnityEngine.Random.Range(-0.5f, 0.5f);
		float ypos = UnityEngine.Random.Range(-0.5f, 0.5f);
		float zpos = UnityEngine.Random.Range(-0.5f, 0.5f);
		this.transform.localPosition = new Vector3(xpos, ypos, zpos);
		speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
		startPosition = this.transform.position;
	}

	void Paint()
	{
		ParticleSystem.Particle[] particles = new ParticleSystem.Particle[cloudSystem.particleCount];
		cloudSystem.GetParticles(particles);

		// can't run paint on particle system until after you've pressed play
		if (particles.Length > 0)
		{
			for (int i = 0; i < particles.Length; i++)
			{
				// vertical blending of color
				particles[i].startColor = Color.Lerp(lining, color, particles[i].position.y / cloudSystem.shape.scale.y);
			}
			painted = true;
			cloudSystem.SetParticles(particles, particles.Length);
		}
	}

	void Update() {
		if (!painted)
		{
			Paint();
		}
		
		this.transform.Translate(0, 0, 0.01f);

		// if ye stray too far...
		if (Vector3.Distance(this.transform.position, startPosition) > distance)
		{
			Spawn();
		}
	}
}
