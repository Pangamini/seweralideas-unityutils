using System.Collections;
using System.Collections.Generic;
using SeweralIdeas.Pooling;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class CompositeRenderer : MonoBehaviour, IEnumerable<Renderer>
    {
        [SerializeField, EditorOnly] private List<Renderer> m_renderers;
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
            var newRenderers = GetComponentsInChildren<Renderer>();
            SetRenderers(newRenderers);
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
    }
}