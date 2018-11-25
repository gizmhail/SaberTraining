using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingDestroyable : MonoBehaviour {
    public GameObject explosionPrefab;
    Rigidbody rb;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody>();
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (explosionPrefab != null)
        {
            InvokeRepeating("SpawnExplosion", 0, 0.8f);
            rb.useGravity = true;
            Destroy(gameObject, 1f);

        }
    }

    void SpawnExplosion()
    {
        var explosion = GameObject.Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosion.transform.localScale = 0.3f * explosion.transform.localScale;
    }
}
