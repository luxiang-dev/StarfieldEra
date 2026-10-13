using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace StarfieldEra
{
    [CreateAssetMenu(menuName = "StarfieldEra/Store visuals")]
    public sealed class StoreVisuals : ScriptableObject
    {
        public GameObject farmer, villager, tree, rock, market, workshop, library, guardian;
        public GameObject grass, flower, shrub, ore, fallenLog, quarry;
        public GameObject[] companions;
        public AnimationClip idle, walk;
        static StoreVisuals cached;
        public static StoreVisuals Current => cached ? cached : (cached = Resources.Load<StoreVisuals>("StoreVisuals"));

        public Transform Place(GameObject prefab, Transform parent, Vector3 position, float height, float yaw = 0, float maxWidth = 0)
        {
            if (!prefab) return null;
            var anchor = new GameObject("Store art - " + prefab.name).transform;
            anchor.SetParent(parent, false);
            anchor.position = position;
            var model = Instantiate(prefab, anchor);
            // Gameplay collision stays on the world's own stable shapes.
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            foreach (var script in model.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            renderers = Array.FindAll(renderers, r => r.enabled && r.gameObject.activeInHierarchy);
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float factor = height / Mathf.Max(.01f, bounds.size.y);
                if(maxWidth > 0) factor = Mathf.Min(factor,maxWidth/Mathf.Max(.01f,Mathf.Max(bounds.size.x,bounds.size.z)));
                model.transform.localScale *= factor;
                model.transform.position += new Vector3(position.x - bounds.center.x, position.y - bounds.min.y, position.z - bounds.center.z) * factor;
            }
            if(prefab.name.Contains("Pine_Tree") && prefab.name.Contains("green"))
                model.transform.localScale = Vector3.Scale(model.transform.localScale,new Vector3(1.3f,1,1.3f));
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
            return anchor;
        }

        public bool Replace(GameObject prefab, Transform root, float height, bool locomotion = false, float maxWidth = 0)
        {
            if (!prefab) return false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                if (!(renderer is TMPro.TMP_SubMesh) && !renderer.GetComponent<TMPro.TMP_Text>()) renderer.enabled = false;
            var visual = Place(prefab, root, root.position, height, 0, maxWidth);
            if (locomotion)
            {
                var animator = visual.GetComponentInChildren<Animator>();
                if (animator && animator.avatar && animator.avatar.isHuman && idle && walk)
                    visual.gameObject.AddComponent<StoreLocomotion>().Initialize(animator, idle, walk, root);
            }
            return true;
        }
    }

    public sealed class StoreLocomotion : MonoBehaviour
    {
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        Transform actor;
        Vector3 previous;
        float blend;
        public void Initialize(Animator animator, AnimationClip idle, AnimationClip walk, Transform actorRoot)
        {
            actor = actorRoot; previous = actor.position;
            graph = PlayableGraph.Create("Farmer locomotion");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            var a = AnimationClipPlayable.Create(graph, idle);
            var b = AnimationClipPlayable.Create(graph, walk);
            a.SetApplyFootIK(true); b.SetApplyFootIK(true);
            graph.Connect(a, 0, mixer, 0); graph.Connect(b, 0, mixer, 1);
            mixer.SetInputWeight(0, 1);
            AnimationPlayableOutput.Create(graph, "Humanoid", animator).SetSourcePlayable(mixer);
            graph.Play();
        }
        void Update()
        {
            if (!graph.IsValid() || !actor) return;
            var delta = actor.position - previous; delta.y = 0;
            previous = actor.position;
            blend = Mathf.MoveTowards(blend, delta.magnitude > Time.deltaTime * .15f ? 1 : 0, Time.deltaTime * 7);
            mixer.SetInputWeight(0, 1 - blend); mixer.SetInputWeight(1, blend);
        }
        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
