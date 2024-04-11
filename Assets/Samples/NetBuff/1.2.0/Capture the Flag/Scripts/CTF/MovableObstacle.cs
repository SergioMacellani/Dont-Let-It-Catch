using System;
using NetBuff;
using NetBuff.Components;
using UnityEngine;

namespace CTF
{
    public class MovableObstacle : NetworkBehaviour
    {
        public Transform pointA;
        public Transform pointB;
        public float timer = 7;
        public float timeToWait = 2;
        private float t;
        private bool isWaiting;
        
        private void OnDrawGizmos()
        {
            var posA = pointA.position;
            var posB = pointB.position;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(posA, 0.25f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(posB, 0.25f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(posA, posB);
        }

        private void Update()
        {
            if (NetworkManager.Instance.IsServerRunning)
            {
                if(!isWaiting)
                {
                    t += Time.deltaTime / timer;
                    transform.position = Vector3.Lerp(pointA.position, pointB.position, t);
                    
                    if (t >= 1)
                    {
                        t = 0;
                        isWaiting = true;
                        (pointA.position, pointB.position) = (pointB.position, pointA.position);
                    }
                }else
                {
                    t += Time.deltaTime / timeToWait;
                    if (t >= 1)
                    {
                        t = 0;
                        isWaiting = false;
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            if (NetworkManager.Instance.IsServerRunning)
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    other.transform.parent = transform;
                }
            }
        }
        
        private void OnCollisionExit(Collision other)
        {
            if (NetworkManager.Instance.IsServerRunning)
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    other.transform.parent = null;
                }
            }
        }
    }
}