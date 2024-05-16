using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelfContact : MonoBehaviour
{
    [SerializeField] private Projectile projectile;
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (projectile.TeamIndex != -1)
        {
            if (collision.attachedRigidbody != null)
            {
                if (collision.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer player))
                {
                    if (player.TeamIndex.Value == projectile.TeamIndex)
                    {
                        return;
                    }
                }
            }
        }
        Destroy(gameObject);
    }
}
