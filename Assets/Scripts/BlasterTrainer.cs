using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class BlasterTrainer : MonoBehaviour {
    public GameObject shotPrefab;
    public GameObject target;
    public GameObject explosionPrefab;
    public float timeToRotate = 0.6f;
    Quaternion endRotation = Quaternion.identity;

    float rotateTime;

	// Use this for initialization
	void Start () {
        if (target == null) {
            target = Player.instance.headCollider.gameObject;
        }
        InvokeRepeating("TargetPlayer", 1, 2.0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (endRotation != Quaternion.identity && endRotation != transform.rotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, endRotation, (Time.time - rotateTime) / timeToRotate);
        }
        else if (endRotation != Quaternion.identity && endRotation == transform.rotation)
        {
            endRotation = Quaternion.identity;
            Shot();
        }
    }

    void TargetPlayer() {
        rotateTime = Time.time;
        endRotation = Quaternion.LookRotation(target.transform.position - transform.position);
    }

    void Shot()
    {
        GameObject.Instantiate(shotPrefab, transform.position + 2 * transform.localScale.z * transform.forward, transform.rotation);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (explosionPrefab != null) {
            InvokeRepeating("SpawnExplosion", 0, 0.6f);
            GetComponent<Rigidbody>().useGravity = true;
            Destroy(gameObject, 0.8f);

        }
    }

    void SpawnExplosion() {
        var explosion = GameObject.Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosion.transform.localScale = 0.3f * explosion.transform.localScale;
    }
}
