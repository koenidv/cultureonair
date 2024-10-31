using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAccelerator : MonoBehaviour
{
    public Player player;
    private ParticleSystem particles;
    private ParticleSystem.ForceOverLifetimeModule fo;
    void Start()
    {
        particles = GetComponent<ParticleSystem>();
        fo = particles.forceOverLifetime;
        fo.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        fo.x = Util.MapfClamped(player.currentSpeed, -player.maxSpeed, player.maxSpeed, -1, 1);
    }
}
