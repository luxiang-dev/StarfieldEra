using UnityEngine;

namespace StarfieldEra
{
    /// <summary>Drive the barbarian's in-place animation from actual horizontal travel.</summary>
    public sealed class BarbarianLocomotion : MonoBehaviour
    {
        static readonly int Speed = Animator.StringToHash("Speed");
        Animator animator;
        Transform actor;
        Vector3 previous;

        public static bool TryAttach(Transform player)
        {
            var prefab = Resources.Load<GameObject>("BarbarianPlayer");
            if (!prefab) return false;
            foreach (var renderer in player.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            var visual = Instantiate(prefab, player, false);
            visual.name = "Barbarian player";
            var locomotion = visual.GetComponent<BarbarianLocomotion>();
            locomotion.actor = player;
            locomotion.previous = player.position;
            return true;
        }

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
        }

        void LateUpdate()
        {
            if (!actor || Time.deltaTime <= 0) return;
            var delta = actor.position - previous;
            delta.y = 0;
            previous = actor.position;
            // Region travel is a teleport, not a step in the walk cycle.
            float speed = delta.sqrMagnitude > 4 ? 0 : delta.magnitude / Time.deltaTime;
            animator.SetFloat(Speed, speed, .12f, Time.deltaTime);
            animator.speed = speed > .15f ? Mathf.Clamp(speed / 2.5f, .75f, 1.8f) : 1;
        }
    }
}
