using System;
using NetBuff.Components;
using UnityEngine;

namespace CTF
{
    public class Shot : NetworkBehaviour
    {
        public float lifeTime = 5.0f;
        public new Rigidbody rigidbody;

        public override void OnSpawned(bool isRetroactive)
        {
            base.OnSpawned(isRetroactive);
            rigidbody.velocity = transform.forward * 25;
            if (TryGetComponent(out Renderer r))
            {
                r.material.color = CTFManager.instance.GetServerListSide(OwnerId) ? Color.blue : new Color(1, 0.5f, 0);
            }
        }

        private void Update()
        {
            if(!HasAuthority)
                return;
            
            lifeTime -= Time.deltaTime;
            
            if (lifeTime <= 0)
                Despawn();
        }

        private void OnCollisionEnter(Collision other)
        {
            other.rigidbody?.AddExplosionForce(100, other.contacts[0].point, 10);
            other.rigidbody?.AddForceAtPosition(transform.forward * 15, other.contacts[0].point);
            Despawn();
        }
        
    }
}