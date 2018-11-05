using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class BlasterTrainer : MonoBehaviour {
    public GameObject shotPrefab;
    public GameObject target;
    public float timeToRotate = 0.2f;
    Quaternion sourceRotation = Quaternion.identity;
    Quaternion endRotation = Quaternion.identity;

    float rotateTime;

	// Use this for initialization
	void Start () {
        if (target == null) {
            target = Player.instance.headCollider.gameObject;
        }
        InvokeRepeating("Shot", 0.5f, 2);
        InvokeRepeating("TargetPlayer", 1, 2.0f);
    }

    // Update is called once per frame
    void Update () {
        if (endRotation != Quaternion.identity && endRotation != transform.rotation) {
            transform.rotation = Quaternion.Slerp(transform.rotation, endRotation, timeToRotate * (Time.time - rotateTime));
        }
    }

    void TargetPlayer() {
        sourceRotation = transform.rotation;
        Quaternion deltaRotation = Quaternion.FromToRotation(transform.forward, (target.transform.position - transform.position).normalized);
        rotateTime = Time.time;
        endRotation = deltaRotation * transform.rotation;
    }

    void Shot()
    {
        GameObject.Instantiate(shotPrefab, transform.position + 2 * transform.localScale.z * transform.forward, transform.rotation);
    }
}
