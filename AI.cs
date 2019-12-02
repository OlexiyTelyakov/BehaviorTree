using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KiwiKaleidoscope.AI
{
    /// <summary>
    /// General base for non-Pest AI.
    /// Handles a number of shared variables.
    /// </summary>
    public class AI : MonoBehaviour
    {
        [HideInInspector]
        protected NavMeshAgent navAgent;
        [HideInInspector]
        protected Transform pilferTarget;
        [HideInInspector]
        protected Transform chaseTarget;

        [Header("General AI")]
        [SerializeField]
        protected float maxIdleTime;
        [SerializeField]
        protected float minIdleTime;
        [HideInInspector]
        protected float idleTimer;
        [SerializeField]
        protected float maxWanderRange = 10f;
        [SerializeField]
        protected float minWanderRange = 4f;

        [SerializeField]
        protected float visionRadius;
        [SerializeField]
        protected int visionArc;

        [Tooltip("Range for picking up items and such.")]
        [SerializeField] protected float interactionRange;

        protected List<Item> items = new List<Item>();

        Vector3 destin;

        protected BTState Plunder()
        {
            //If pilfer is null, state failed.
            if (pilferTarget == null) return BTState.Failure;
            //If item is in the interaction range, pick up the litter.
            if (Vector3.Distance(pilferTarget.transform.position, transform.position) <= interactionRange)
            {
                PickUpItem();
                return BTState.Success;
            }
            //Otherwise attempt to path the navAgent there and update the state as running.
            else
            {
                if (navAgent.isStopped || !navAgent.hasPath)
                {
                    //Path towards the pilfer.
                    navAgent.SetDestination(pilferTarget.position + (transform.position - pilferTarget.position).normalized * interactionRange * 0.9f);
                    navAgent.isStopped = false;
                }
                return BTState.Running;
            }
        }

        protected BTState Wander()
        {
            //If MooMoo is currently idling, return failure.
            if (idleTimer != 0 && !navAgent.hasPath) return BTState.Failure;
            if (!navAgent.hasPath)
            {
                //Reset idle timer.
                idleTimer = Random.Range(minIdleTime, maxIdleTime);
                //Find a new random spot to go to.
                bool newPathGenerated = false;
                while (!newPathGenerated)
                {
                    //Pick a random point on the navMesh.
                    Vector3 dest = Random.insideUnitCircle.V2With().normalized * Random.Range(minWanderRange,maxWanderRange) + transform.position;
                    NavMeshHit hit;
                    //If it exists, go there.
                    if(NavMesh.SamplePosition(dest, out hit, 1f, NavMesh.AllAreas))
                    {
                        navAgent.SetDestination(hit.position);
                        destin = hit.position;
                        navAgent.isStopped = false;
                        break;
                    }
                }
            }
            return BTState.Running;
        }

        protected BTState Idle()
        {
            if (GameStateManager.CurrentGameState() != GameStateManager.GameState.InMenu)
            {
                //Increment the idle time
                idleTimer -= Time.deltaTime;
                idleTimer = Mathf.Clamp(idleTimer, 0, maxIdleTime);
            }
            //Idle is always running.
            return BTState.Running;
        }

        /// <summary>
        /// Resets the idle timer. Useful for higher priority states that might interrupt idling and not have it return to idle 0.245s.
        /// Returns a failure so can be safely added to Selectors.
        /// </summary>
        /// <returns></returns>
        protected BTState ResetIdle()
        {
            idleTimer = 0;
            return BTState.Failure;
        }

        protected void PickUpItem()
        {
            //Get item reference.
            PickUpItem litter = pilferTarget.GetComponent<PickUpItem>();
            items.Add(litter.itemTemplate);
            //Pocket it.
            litter.Plunder();
            //Reset pilfer target
            pilferTarget = null;
        }

        private void OnDrawGizmos()
        {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(destin, 0.25f);
        }
    }
}

