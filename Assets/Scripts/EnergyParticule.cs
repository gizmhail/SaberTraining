using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyParticule : MonoBehaviour {
    public float moveTime = 1;
    public GameObject target;
    public Vector3 targetPosition;
    float startTime;
	// Use this for initialization
	void Start () {
        Destroy(gameObject, moveTime);
        startTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
        if (target != null) targetPosition = target.transform.position;
        transform.position = Vector3.Lerp(transform.position, targetPosition, (Time.time - startTime)/moveTime );
	}
}
