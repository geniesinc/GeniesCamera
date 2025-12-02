using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Genies.CreatorTools.utils.RenderFeature
{
    public class UVProjectRenderPassFeature : ScriptableRendererFeature
    {
        // Tattooenator will set these
        private Material _drawMaterial;
        [HideInInspector]
        public Material DrawMaterial
        {
            get { return _drawMaterial; }
            set
            {
                _drawMaterial = value;
                if (_scriptablePass != null)
                {
                    _scriptablePass.Material = _drawMaterial;
                }
            }
        }
        private Mesh _drawMesh;
        [HideInInspector]
        public Mesh DrawMesh
        {
            get { return _drawMesh; }
            set
            {
                _drawMesh = value;
                if (_scriptablePass != null)
                {
                    _scriptablePass.Mesh = _drawMesh;
                }
            }
        }
        private RenderTexture _renderTexture;
        [HideInInspector]
        public RenderTexture RenderTex
        {
            get { return _renderTexture; }
            set
            {
                _renderTexture = value;
                if (_scriptablePass != null)
                {
                    _scriptablePass.RenderTexture = _renderTexture;
                }
            }
        }


        public bool ShouldExecute {
            get
            {
                if (_scriptablePass == null)
                {
                    return false;
                }
                else
                {
                    return _scriptablePass.ShouldExecute;
                }
            }
            set
            {
                if (_scriptablePass == null)
                {
                    return;
                }

                _scriptablePass.ShouldExecute = value;
            }
        }
        private Action<Texture2D> _handler;


        public class CustomRenderPass : ScriptableRenderPass
        {
            private Material _material;
            public Material Material
            {
                get { return _material; }
                set { _material = value; }
            }
            private Mesh _mesh;
            public Mesh Mesh
            {
                get { return _mesh; }
                set { _mesh = value; }
            }
            private RenderTexture _renderTex;
            public RenderTexture RenderTexture
            {
                get { return _renderTex; }
                set { _renderTex = value; }
            }

            public bool CanExecute { get; private set; }
            public event Action<Texture2D> HasExecuted; // notify that rendertex has been drawn
            private bool _shouldExecute = false;
            public bool ShouldExecute { get { return _shouldExecute; } set { _shouldExecute = value; } }

            public CustomRenderPass(Material mat, Mesh mesh, RenderTexture rendertex) {
                _material = mat;
                _mesh = mesh;
                _renderTex = rendertex;
                Checks();
            }

            private void Checks()
            {
                CanExecute = true;
                if (_mesh == null)
                {
                    Debug.LogWarning("Mesh set in UVProjecthRenderFeature is null");
                    CanExecute = false;
                }
                if (_material == null)
                {
                    Debug.LogWarning("Draw material in UVProjectRenderFeature is null");
                    CanExecute = false;
                }
                if (_renderTex == null)
                {
                    Debug.LogWarning("Render texture set in UVProjectRenderFeature is null");
                    CanExecute = false;
                }
            }
#if !UNITY_6000_0_OR_NEWER

        /// <summary>
        /// This method is called before executing the render pass.
        /// It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        /// When empty this render pass will render to the active camera render target.
        /// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        /// The render pipeline will ensure target setup and clearing happens in a performant manner.
        ///
        /// IMPORTANT:
        /// This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="renderingData"></param>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var rtdesc = renderingData.cameraData.cameraTargetDescriptor;
            Debug.Log(rtdesc);
        }

        private static Texture2D TextureFromRenderTexture(RenderTexture renderTexture, string name) {
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height,
                GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None);
            texture.name = name;

            var current = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
            texture.Apply();
            RenderTexture.active = current;

            return texture;
        }

        /// <summary>
        /// Here you can implement the rendering logic.
        /// Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        /// https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        /// You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        ///
        /// IMPORTANT:
        /// This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!ShouldExecute || !CanExecute || !_renderTex.IsCreated())
            {
                return;
            }

            Camera camera = renderingData.cameraData.camera;
            if (camera.cameraType != CameraType.Game)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(name: "DrawPass");
            cmd.SetRenderTarget(_renderTex);
            cmd.ClearRenderTarget(false, true, Color.clear);
            cmd.DrawMesh(_mesh, Matrix4x4.identity, _material);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            //context.Submit();
            Texture2D tex = TextureFromRenderTexture(_renderTex, "tiledTex");
            HasExecuted?.Invoke(tex);
            //HasExecuted?.Invoke(null);
        }
#endif
            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
            }

            public void RemoveHasExecutedSubscribers()
            {
                var subscribers = HasExecuted?.GetInvocationList();
                if (subscribers != null) {
                    for (int i = 0; i < subscribers.Length; i++) {
                        HasExecuted -= subscribers[i] as Action<Texture2D>;
                    }
                }
            }
        }

        private CustomRenderPass _scriptablePass;

        /// <inheritdoc/>
        public override void Create()
        {
            if (RenderTex == null)
            {
                return;
            }
            //Debug.Log("<color=magenta>Creating UVProjectRenderPassFeature</color>");
            _scriptablePass = new CustomRenderPass(DrawMaterial, DrawMesh, RenderTex);
            _scriptablePass.HasExecuted += _handler;

            // Configures where the render pass should be injected.
            _scriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_scriptablePass == null)
            {
                return;
            }

            if (_scriptablePass.CanExecute && _scriptablePass.ShouldExecute)
            {
                //Debug.Log("Enqueuing Custom RenderPass");   // guess this happens once per frame
                renderer.EnqueuePass(_scriptablePass);
            }
        }

        public void SubscribeToRenderPassResultTexture(Action<Texture2D> handler) {
            if (_scriptablePass != null)
            {
                _scriptablePass.RemoveHasExecutedSubscribers();
                _scriptablePass.HasExecuted += handler;
            }
            //Debug.Log("<color=magenta>Setting handler for PostRenderPassResult</color>");
            _handler = handler;
        }

        protected override void Dispose(bool disposing) {
            if (_scriptablePass != null)
            {
                _scriptablePass.HasExecuted -= _handler;
            }
            //m_ScriptablePass.Dispose();
            base.Dispose(disposing);
        }
    }
}

