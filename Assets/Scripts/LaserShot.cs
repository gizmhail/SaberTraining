using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserShot : MonoBehaviour {
    public float speed = 3;
    Rigidbody rb;
	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
        rb.velocity = speed*transform.forward;
        Destroy(gameObject, 5);
    }

    private void FixedUpdate()
    {
        rb.velocity = speed * rb.velocity.normalized;
        transform.rotation = Quaternion.LookRotation(rb.velocity);
    }

    private void OnCollisionEnter(Collision collision)
    {
        rb.velocity = rb.velocity.magnitude * collision.contacts[0].normal;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.LookRotation(collision.contacts[0].normal);
    }

}

