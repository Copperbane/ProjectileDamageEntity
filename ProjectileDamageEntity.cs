using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class ProjectileDamageEntity : MissileDamageEntity
    {
        
        private Vector3 inipos;
        private bool collided;

        [Header("Configuration")]
        public bool isOffline = false;
        public LayerMask hitLayers;
        [Tooltip("if you don't set it, you better don't change destroy delay.")]
        public GameObject ProjectileObject;
        [Space]
        public bool hasGravity = false;
        [Tooltip("If customGravity is zero, its going to use physics.gravity")]
        public Vector3 customGravity;
        [Space]
        [Tooltip("Angle of shoot.")]
        public bool useAngle = false;
        [Range(0, 89)]
        public float angle;
        [Space]
        [Tooltip("Calculate the speed needed for the arc. Perfect for lock on targets.")]
        public bool recalculateSpeed = false;

        [Header("Prediction Steps")]
        [Tooltip("How many ray casts per frame to detect collisions.")]
        public int predictionStepPerFrame = 6;
        private Vector3 bulletVelocity;

        [Header("Extra Effects")]
        [Tooltip("If you want to activate an effect that is child or instantiate it on client. For 'child' effect, use destroy delay.")]
        public bool instantiateImpact = false;
        [Tooltip("Change direction of the impact effect based on hit normal.")]
        public bool useNormal = false;
        public GameObject ImpactEffect;
        [Tooltip("Perfect for arrows. If you are using 'Child effect', when the projectile despawn, the effect too.")]
        public bool stickTo;
        [Space]
        [Tooltip("This is the effect that spawn if don't hit anything and the end of the max distance.")]
        public bool instantiateDisappear = false;
        public GameObject disappearEffect;
        
        private Vector3 normal;
        private Vector3 hitPos;
        private Vector3 iniImpactEffectPos;

        public override void Setup(
            IGameEntity attacker,
            CharacterItem weapon,
            Dictionary<DamageElement, MinMaxFloat> damageAmounts,
            BaseSkill skill,
            short skillLevel,
            float missileDistance,
            float missileSpeed,
            IDamageableEntity lockingTarget)
        {
            base.Setup(attacker, weapon, damageAmounts, skill, skillLevel, missileDistance, missileSpeed, lockingTarget);

            //Initial configuration
            inipos = this.transform.position;
            collided = false;

            //Configuration bullet and effects
            if (ProjectileObject) ProjectileObject.SetActive(true);
            if (ImpactEffect && !instantiateImpact)
            {
                ImpactEffect.SetActive(false);
                iniImpactEffectPos = ImpactEffect.transform.localPosition;
            }
            if (disappearEffect && !instantiateDisappear) disappearEffect.SetActive(false);

            //Movement
            Vector3 targetPos = inipos + (this.transform.forward * missileDistance);

            if (lockingTarget != null && lockingTarget.CurrentHp > 0) targetPos = lockingTarget.GetTransform().position;

            float dist = Vector3.Distance(inipos, targetPos);
            float yOffset = -transform.forward.y;

            if (recalculateSpeed) missileSpeed = LaunchSpeed(dist, yOffset, Physics.gravity.magnitude, angle * Mathf.Deg2Rad);

            if (useAngle) this.transform.eulerAngles = new Vector3(-angle, this.transform.eulerAngles.y, this.transform.eulerAngles.z);

            bulletVelocity = this.transform.forward * missileSpeed;
           
        }

        public float LaunchSpeed(float distance, float yOffset, float gravity, float angle)
        {
            float speed = (distance * Mathf.Sqrt(gravity) * Mathf.Sqrt(1 / Mathf.Cos(angle))) / Mathf.Sqrt(2 * distance * Mathf.Sin(angle) + 2 * yOffset * Mathf.Cos(angle));
            return speed;
        }

        protected override void Update()
        {
            /* clear up Missile Duration */
        }

        protected override void FixedUpdate()
        {
            // Don't move if exploded or collided
            if (isExploded || collided) return;

            Vector3 point1 = this.transform.position;
            float stepSize = 1.0f / predictionStepPerFrame;
            for (float step = 0; step < 1; step += stepSize)
            {
                if (hasGravity)
                {
                    Vector3 gravity = Physics.gravity;
                    if (customGravity != Vector3.zero) gravity = customGravity;
                    bulletVelocity += gravity * stepSize * Time.deltaTime;
                }

                Vector3 point2 = point1 + bulletVelocity * stepSize * Time.deltaTime;
                    float dist = Vector3.Distance(inipos, transform.position);
                    if (dist > missileDistance)
                    {
                        NoImpact();
                        return;
                    }

                    Ray ray = new Ray(point1, point2 - point1);
                    RaycastHit hit;
                    if (Physics.Raycast(point1, point2 - point1, out hit, missileSpeed * Time.deltaTime, hitLayers))
                    {
                        if (destroying) return;

                        if (useNormal) normal = hit.normal;
                        hitPos = hit.point;

                        if (attacker != null && attacker.GetGameObject() == hit.transform.gameObject)
                        {
                            point1 = point2;
                            continue;
                        }

                        //check if is already death
                        if (hit.transform.GetComponent<DamageableEntity>() && hit.transform.GetComponent<DamageableEntity>().CurrentHp <= 0)
                        {
                            point1 = point2;
                            continue;
                        }

                        Impact(hit.transform.gameObject);
                        break;
                    }
                point1 = point2;
            }
            this.transform.rotation = Quaternion.LookRotation(bulletVelocity);
            this.transform.position = point1;

        }

        protected void NoImpact()
        {
            if (!IsServer && disappearEffect || isOffline && disappearEffect)
            {
                if (ProjectileObject) ProjectileObject.SetActive(false);
                if (instantiateDisappear)
                {
                    GameObject noimpact = Instantiate(disappearEffect, transform.position, Quaternion.identity);
                }
                else
                {
                    disappearEffect.SetActive(true);
                }
                PushBack(destroyDelay);
                destroying = true;
                return;
            }
            PushBack();
            destroying = true;
            return;
        }

        protected void Impact(GameObject hitted)
        {
            
            //Spawn. 
            if (!IsServer && ImpactEffect || isOffline && ImpactEffect)
            {
                if (ProjectileObject) ProjectileObject.SetActive(false);
                if (instantiateImpact)
                {
                    Quaternion rot = Quaternion.identity;
                    if (useNormal) rot = Quaternion.FromToRotation(Vector3.forward, normal);
                    GameObject impact = Instantiate(ImpactEffect, hitPos, rot);
                    if (stickTo) impact.transform.parent = hitted.transform;

                }
                else
                {
                    if (useNormal) ImpactEffect.transform.rotation = Quaternion.FromToRotation(Vector3.forward, normal);
                    ImpactEffect.transform.position = hitPos;
                    if(stickTo)ImpactEffect.transform.parent = hitted.transform;
                    ImpactEffect.SetActive(true);
                }

            }

            //Check target
            DamageableHitBox target = null;
            if (FindTargetHitBox(hitted, out target))
            {
                if (explodeDistance > 0f)
                {
                    // Explode immediately when hit something
                    Explode();
                }
                else
                {
                    // If this is not going to explode, just apply damage to target
                    ApplyDamageTo(target);
                }
                collided = true;
                PushBack(destroyDelay);
                destroying = true;
                return;
            }

            // Hit walls or grounds → Explode
            if (hitted.layer != CurrentGameInstance.characterLayer &&
                hitted.layer != CurrentGameInstance.itemDropLayer &&
                !CurrentGameInstance.IgnoreRaycastLayersValues.Contains(hitted.layer))
            {
                if (explodeDistance > 0f)
                {
                    // Explode immediately when hit something
                    Explode();
                }
                collided = true;
                PushBack(destroyDelay);
                destroying = true;
                return;
            }
        }

        protected override void OnPushBack()
        {
            if (ImpactEffect && stickTo && !instantiateImpact)
            {
                ImpactEffect.transform.parent = this.transform;
                ImpactEffect.transform.localPosition = iniImpactEffectPos;
            }
            if (onDestroy != null)
                onDestroy.Invoke();
            base.OnPushBack();
        }
    }
}
