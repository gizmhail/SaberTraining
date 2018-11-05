using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Based on DigitalOpus article on "CONTROLLING OBJECTS WITH FORCES AND TORQUES":
 * http://digitalopus.ca/site/pd-controllers/
 * More precisly, based on David Wu’s Stable Backwards PD Controller, described in it 
 * (maybe (?) in http://www.pseudointeractive.com/files/physicsandanimationgdc2003.doc)
 * 
 * /!\ /!\ /!\ 
 * License currently undefined. Need to check sources (DigitalOpus and David Wu) licenses
 * /!\ /!\ /!\ 
 */

namespace ForceMove
{
    public static class RigidBodyForceMove
    {
        /*
         * 
         * According to http://digitalopus.ca/site/pd-controllers/ : 
         * - damping = 1, the system is critically damped
         * - damping > 1 the system is over damped (sluggish)
         * - damping is < 1 the system is under damped (it will oscillate a little)
         * - Frequency is the speed of convergence. If damping is 1, frequency is the 1/time taken to reach ~95% of the target value. i.e. a frequency of 6 will bring you very close to your target within 1/6 seconds.
         */
        public static void AddForceTowards(this Rigidbody rb, Vector3 initialPosition, Vector3 destinationPosition, float damping = 1.2f, float frequency = 2f)
        {
            float kp = (6f * frequency) * (6f * frequency) * 0.25f;
            float kd = 4.5f * frequency * damping;
            Vector3 Pdes = destinationPosition;
            Vector3 Vdes = Vector3.zero;
            float dt = Time.fixedDeltaTime;
            float g = 1 / (1 + kd * dt + kp * dt * dt);
            float ksg = kp * g;
            float kdg = (kd + kp * dt) * g;
            Vector3 Pt0 = initialPosition;
            Vector3 Vt0 = rb.velocity;
            Vector3 F = (Pdes - Pt0) * ksg + (Vdes - Vt0) * kdg;
            rb.AddForce(F);
        }

        public static void AddTorqueTowards(this Rigidbody rb, Quaternion initialRotation, Quaternion destinationRotation, float damping = 1.2f, float frequency = 2f)
        {
            float kp = (6f * frequency) * (6f * frequency) * 0.25f;
            float kd = 4.5f * frequency * damping;
            Vector3 x;
            float xMag;
            Quaternion q = destinationRotation * Quaternion.Inverse(initialRotation);
            q.ToAngleAxis(out xMag, out x);
            x.Normalize();
            x *= Mathf.Deg2Rad;
            Vector3 pidv = kp * x * xMag - kd * rb.angularVelocity;
            Quaternion rotInertia2World = rb.inertiaTensorRotation * initialRotation;
            pidv = Quaternion.Inverse(rotInertia2World) * pidv;
            pidv.Scale(rb.inertiaTensor);
            pidv = rotInertia2World * pidv;
            rb.AddTorque(pidv);
        }
    }
}

