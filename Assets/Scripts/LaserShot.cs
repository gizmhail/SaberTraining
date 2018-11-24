using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserShot : MonoBehaviour, EnergyLockable
{
    public float speed = 3;
    Rigidbody rb;
    AudioSource audioSource;

    // Use this for initialization
    void Start () {
        audioSource = GetComponent<AudioSource>();
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
        if (collision.collider.name == "HeadCollider")
        {
            Destroy(gameObject);
            return;
        }
        rb.velocity = rb.velocity.magnitude * collision.contacts[0].normal;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.LookRotation(collision.contacts[0].normal);
        audioSource.Play();
    }

    #region EnergyLockable
    void EnergyLockable.EnergyLocked(EnergyMove lockSource)
    {
    }

    void EnergyLockable.EnergyUnlocked(EnergyMove lockSource)
    {
    }

    bool EnergyLockable.IsImmunetoEnergyMove(EnergyMove source)
    {
        return true;
    }
    #endregion
}

