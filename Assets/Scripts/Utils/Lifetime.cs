using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lifetime : MonoBehaviour
{
    [SerializeField] private float lifeTime;
    private void Start()
    {
        Destroy(gameObject,lifeTime);
    }
}
