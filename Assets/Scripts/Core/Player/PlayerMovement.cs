using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

using static UnityEngine.ParticleSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Referances")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform bodyTranform;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private ParticleSystem dustCloud;
    

    [Header("Settings")]
    [SerializeField] private float movementSpeed = 4f;
    [SerializeField] private float turningRate = 270f;
    [SerializeField] private float particleEmissionValue = 10f;

    private Vector2 previousMovementInput;
    private Vector3 previousPos;

    private const float ParticleStopThreshhold = 0.005f;

    private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        emissionModule = dustCloud.emission;
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { return; }
        inputReader.MovementEvent += HandleMovement;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsOwner) { return; }
        inputReader.MovementEvent -= HandleMovement;
    }
    private void Update()
    {
        if (!IsOwner) { return; }

        float zRotation = previousMovementInput.x * -turningRate * Time.deltaTime;
        bodyTranform.Rotate(0f, 0f, zRotation);
    }
    private void FixedUpdate()
    {
     
        if ((transform.position - previousPos).sqrMagnitude > ParticleStopThreshhold)
        {
            emissionModule.rateOverTime = particleEmissionValue;
        }
        else
        {
            emissionModule.rateOverTime = 0;
        }

        previousPos = transform.position;

        if (!IsOwner) { return; }

        rb.velocity = (Vector2)bodyTranform.up * previousMovementInput.y * movementSpeed;
    }
    private void HandleMovement(Vector2 movementInput)
    {
        previousMovementInput = movementInput;
    }
}
