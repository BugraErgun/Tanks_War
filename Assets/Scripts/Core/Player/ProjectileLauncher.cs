using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ProjectileLauncher : NetworkBehaviour
{
    [Header("Referances")]
    [SerializeField] private TankPlayer player;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private CoinWallet wallet;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private Collider2D playerCollider;

    [Header("Settings")]
    [SerializeField] private float projectileSpeed;
    [SerializeField] private float fireRate;
    [SerializeField] private float muzzleFlashDuration;
    [SerializeField] private int costToFire;

    private bool isPointerOverUI;
    private bool shouldFire;
    private float timer;
    private float muzzleFlashTimer;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        inputReader.PrimaryFireEvent += HandlePrimaryFire;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        inputReader.PrimaryFireEvent -= HandlePrimaryFire;

    } 

    private void Update()
    {
        if (muzzleFlashTimer > 0f)
        {
            muzzleFlashTimer -= Time.deltaTime;

            if (muzzleFlashTimer <= 0f)
            {
                muzzleFlash.SetActive(false);
            }
        }

        if (!IsOwner) { return; }

        isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }

        if (!shouldFire) { return; }

        if (timer > 0) { return; }

        if(wallet.TotalCoins.Value < costToFire) { return; }

        PrimaryFireServerRPC(projectileSpawnPoint.position, projectileSpawnPoint.up);

        SpawnProjectile(projectileSpawnPoint.position, projectileSpawnPoint.up,player.TeamIndex.Value);

        timer = 1 / fireRate;
    }
    private void HandlePrimaryFire(bool shouldFire)
    {
        if (shouldFire)
        {
            if (isPointerOverUI)
            {
                return;
            }
        }
        this.shouldFire = shouldFire;
    }

    [ServerRpc]
    private void PrimaryFireServerRPC(Vector3 spawnPos, Vector3 direction)
    {
        if (wallet.TotalCoins.Value < costToFire) { return; }

        wallet.SpendCoins(costToFire);

        GameObject projectileInstance = Instantiate(serverProjectilePrefab, spawnPos, Quaternion.identity);

        projectileInstance.transform.up = direction;

        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        if (projectileInstance.TryGetComponent<Projectile>(out Projectile projectile))
        {
            projectile.Initialise(player.TeamIndex.Value);
        }
        if (projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.velocity = rb.transform.up * projectileSpeed;
        }

        PrimaryFireClientRPC(spawnPos, direction);
    }
    [ClientRpc]
    private void PrimaryFireClientRPC(Vector3 spawnPos, Vector3 direction)
    {
        if (IsOwner) return;

        SpawnProjectile(spawnPos, direction, player.TeamIndex.Value);
    }
    private void SpawnProjectile(Vector3 spawnPos, Vector3 direction,int teamIndex)
    {
        muzzleFlash.SetActive(true);
        muzzleFlashTimer = muzzleFlashDuration;

        GameObject projectileInstance = Instantiate(clientProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        Physics2D.IgnoreCollision(playerCollider, projectileInstance.GetComponent<Collider2D>());

        if(projectileInstance.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.velocity = rb.transform.up * projectileSpeed;
        }
    }
 

}
