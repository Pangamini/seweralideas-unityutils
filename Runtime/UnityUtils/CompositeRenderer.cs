using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.UnityUtils
{
    public class CompositeRenderer : MonoBehaviour, IEnumerable<Renderer>
    {
        [SerializeField, EditorOnly] private List<Renderer> m_renderers = new();
        private HashSet<IRenderMethod> m_renderMethods = new HashSet<IRenderMethod>();

        public int Count => m_renderers.Count;
        public Renderer this[int i] => m_renderers[i];

        public List<Renderer>.Enumerator GetEnumerator()
        {
            return m_renderers.GetEnumerator();
        }

        public interface IRenderMethod
        {
            void EnableOnRenderer(Renderer renderer);
            void DisableOnRenderer(Renderer renderer);
        }

        private void Reset()
        {
            FindRenderers();
        }

        public void SetRenderers(IList<Renderer> newRenderers)
        {
            using (UnityEngine.Pool.ListPool<Renderer>.Get(out var oldRenderers))
            {
                // copy m_renders to oldRenderers
                oldRenderers.Capacity = m_renderers.Count;
                for (int i = 0; i < m_renderers.Count; ++i)
                    oldRenderers.Add(m_renderers[i]);

                // call RemoveRenderer for every oldRenderer not present int newRenderers
                for (int i= 0; i < oldRenderers.Count; ++i)
                {
                    if (!newRenderers.Contains(oldRenderers[i]))
                        RemoveRenderer(oldRenderers[i]);
                }
            }

            for (int i = 0; i < newRenderers.Count; ++i)
            {
                AddRenderer(newRenderers[i]);
            }
        }

        public bool AddRenderMethod(IRenderMethod renderMethod)
        {
            if (!m_renderMethods.Add(renderMethod)) return false;
            foreach (var renderer in m_renderers)
                renderMethod.EnableOnRenderer(renderer);
            return true;
        }

        public bool RemoveRenderMethod(IRenderMethod renderMethod)
        {
            if (!m_renderMethods.Remove(renderMethod)) return false;
            foreach (var renderer in m_renderers)
                renderMethod.DisableOnRenderer(renderer);
            return true;
        }

        public bool AddRenderer(Renderer renderer)
        {
            if (m_renderers.Contains(renderer)) return false;
            m_renderers.Add(renderer);
            foreach (var renderMethod in m_renderMethods)
                renderMethod.EnableOnRenderer(renderer);
            return true;
        }

        public bool RemoveRenderer(Renderer renderer)
        {
            if (!m_renderers.Remove(renderer)) return false;
            foreach (var renderMethod in m_renderMethods)
                renderMethod.DisableOnRenderer(renderer);
            return true;
        }

        IEnumerator<Renderer> IEnumerable<Renderer>.GetEnumerator()
        {
            return ((IEnumerable<Renderer>)m_renderers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Renderer>)m_renderers).GetEnumerator();
        }

        public void RenderMeshRenderers(Material material)
        {
            using (MaterialPropertyPool.Get(out var block))
            {
                foreach (var renderer in m_renderers)
                {
                    var meshRenderer = renderer as MeshRenderer;
                    if (!meshRenderer)
                        continue;

                    var filter = meshRenderer.GetComponent<MeshFilter>();
                    var mesh = filter.sharedMesh;
                    var matrix = meshRenderer.transform.localToWorldMatrix;
                    var layer = renderer.gameObject.layer;
                    renderer.GetPropertyBlock(block);
                    for (int i = 0; i < mesh.subMeshCount; ++i)
                        Graphics.DrawMesh(mesh, matrix, material, layer, null, i, block);
                }
            }
        }

        /// <summary>
        /// Adds a material to the renderers, as a layer: after the materials the renderer had, and in the order of
        /// <see cref="Priority"/> among the layers added by other methods (a lower priority first; layers of the same priority
        /// in the order they were added). Materials are drawn in the order of the renderer's list, so a layer is drawn after
        /// the layers with a lower priority, whenever they are added. Layers can be added and removed in any order.
        /// </summary>
        public class AddMaterialMethod : IRenderMethod
        {
            public AddMaterialMethod(Material material, int priority = 0)
            {
                Material = material;
                Priority = priority;
            }

            public Material Material { get; private set; }
            public int Priority { get; private set; }

            // The layers on each renderer, in the order of the list they make at its end. Kept apart from the renderer, so that
            // its own materials stay what they were, whatever the layers are.
            private static readonly ConditionalWeakTable<Renderer, List<AddMaterialMethod>> s_layers = new();

            void IRenderMethod.EnableOnRenderer(Renderer rend) => AddLayer(rend);
            void IRenderMethod.DisableOnRenderer(Renderer rend) => RemoveLayer(rend);

            private void AddLayer(Renderer renderer)
            {
                var layers = s_layers.GetOrCreateValue(renderer);
                using (ListPool<Material>.Get(out var materials))
                {
                    renderer.GetSharedMaterials(materials);
                    TakeLayersOff(materials, layers);

                    int index = layers.Count; // after the last layer that has a priority not greater than ours
                    while (index > 0 && layers[index - 1].Priority > Priority)
                        --index;
                    layers.Insert(index, this);

                    PutLayersOn(renderer, materials, layers);
                }
            }

            private void RemoveLayer(Renderer renderer)
            {
                if (renderer == null || !s_layers.TryGetValue(renderer, out var layers)) // (a destroyed one has nothing to take off)
                    return;

                using (ListPool<Material>.Get(out var materials))
                {
                    renderer.GetSharedMaterials(materials);
                    TakeLayersOff(materials, layers);

                    if (!layers.Remove(this))
                        return;

                    PutLayersOn(renderer, materials, layers);
                }
            }

            // What is left is the materials the renderer had. The layers are at the end, but are looked for from there instead
            // of cut off, as the renderer's materials may also have been set by someone else since.
            private static void TakeLayersOff(List<Material> materials, List<AddMaterialMethod> layers)
            {
                for (int i = layers.Count - 1; i >= 0; --i)
                {
                    int index = materials.LastIndexOf(layers[i].Material);
                    if (index >= 0)
                        materials.RemoveAt(index);
                }
            }

            private static void PutLayersOn(Renderer renderer, List<Material> materials, List<AddMaterialMethod> layers)
            {
                foreach (var layer in layers)
                    materials.Add(layer.Material);
                renderer.sharedMaterials = materials.ToArray(); // is ths a joke? No renderer.SetSharedMaterials(List<>) ?
            }
        }

        [ContextMenu("Find Renderers")]
        public void FindRenderers()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Find Renderers");
#endif
            using (ListPool<Renderer>.Get(out var renderers))
            {
                GetComponentsInChildren(renderers);
                for (int i = 0; i < renderers.Count;)
                {
                    var renderer = renderers[i];
                    if (renderer.GetComponentInParent<CompositeRenderer>() != this)
                    {
                        renderers[i] = renderers[^1];
                        renderers.RemoveAt(renderers.Count - 1);
                        continue;
                    }

                    ++i;
                }

                SetRenderers(renderers);
            }
        }

    }
}