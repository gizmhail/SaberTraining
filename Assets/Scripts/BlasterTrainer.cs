using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using ForceBasedMove;

public class BlasterTrainer : MonoBehaviour {
    public GameObject shotPrefab;
    public GameObject target;
    public float timeToRotate = 0.6f;
    Rigidbody rb;
    Quaternion endRotation = Quaternion.identity;
    bool isMoving;
    Vector3 moveTarget;

    float rotateTime;

	// Use this for initialization
	void Start () {
        if (target == null) {
            target = Player.instance.headCollider.gameObject;
        }
        rb = GetComponent<Rigidbody>();
        PlanTargetPlayer(initialDelay: 1);
        InvokeRepeating("Move", 4, 5.0f);
    }

    void PlanTargetPlayer(float initialDelay, float repeatTime = 2.0f) {
        CancelInvoke("TargetPlayer");
        InvokeRepeating("TargetPlayer", initialDelay, repeatTime);
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

    private void FixedUpdate()
    {
        if (Vector3.Distance(moveTarget, transform.position) < 0.05f && isMoving)
        {
            isMoving = false;
            rb.velocity = Vector3.zero;
            PlanTargetPlayer(initialDelay: 0);
        }
        if (isMoving) {
            rb.AddForceTowards(transform.position, moveTarget);
        }
    }

    void TargetPlayer() {
        if (endRotation != Quaternion.identity) return;
        rotateTime = Time.time;
        endRotation = Quaternion.LookRotation(target.transform.position - transform.position);
    }

    void Shot()
    {
        GameObject.Instantiate(shotPrefab, transform.position + 2 * transform.localScale.z * transform.forward, transform.rotation);
    }

    private void Move()
    {
        float moveAmplitude = 0.5f;
        // See https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
        var trainerToTarget = target.transform.position - transform.position;
        var trainerToRandomAroundTarget = (target.transform.position + Random.insideUnitSphere) - transform.position;
        var normalToTarget = Vector3.Cross(trainerToTarget, trainerToRandomAroundTarget);
        moveTarget = transform.position + moveAmplitude * normalToTarget.normalized;
        isMoving = true;
        //transform.position = moveTarget;
    }
}
