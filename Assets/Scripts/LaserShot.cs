using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserShot : MonoBehaviour {
    Rigidbody rb;
	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
        rb.velocity = transform.forward;
        Destroy(gameObject, 5);
    }

    private void OnCollisionEnter(Collision collision)
    {
        rb.velocity = rb.velocity.magnitude * collision.contacts[0].normal;
        transform.rotation = Quaternion.LookRotation(collision.contacts[0].normal);
    }
}
