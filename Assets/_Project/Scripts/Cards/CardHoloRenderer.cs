using UnityEngine;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Renderiza la carta con el shader holográfico (HolographicFoilShader.shader)
    /// a una RenderTexture para ser proyectada en tiempo real dentro de UI Toolkit.
    /// Soporta física de inclinación 3D e interactividad táctil (GDD 7.1, prototipo docs).
    /// </summary>
    public class CardHoloRenderer : MonoBehaviour
    {
        [Header("Shader & Material")]
        [SerializeField] private Shader holoShader;
        [SerializeField] private Material baseMaterial;
        private Material instanceMaterial;

        [Header("Render Texture Settings")]
        [SerializeField] private int textureWidth = 720;
        [SerializeField] private int textureHeight = 1080;
        private RenderTexture renderTexture;
        private Texture2D defaultFoilTexture;

        [Header("Off-Screen Rig")]
        private Camera offscreenCamera;
        private GameObject quadObject;
        private Transform quadTransform;

        [Header("Tilt Configuration")]
        [SerializeField] private float maxTiltAngle = 16f;
        [SerializeField] private float returnSpeed = 12f;
        private static readonly int TiltPosID = Shader.PropertyToID("_TiltPos");
        private static readonly int HoloIntensityID = Shader.PropertyToID("_HoloIntensity");
        private static readonly int ShimmerSpeedID = Shader.PropertyToID("_ShimmerSpeed");
        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

        private Vector2 targetTilt = Vector2.zero;
        private Vector2 currentTilt = Vector2.zero;
        private bool isHoloActive = true;

        public RenderTexture TargetTexture => renderTexture;

        private void Awake()
        {
            InitializeRig();
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (instanceMaterial != null)
            {
                Destroy(instanceMaterial);
            }

            if (defaultFoilTexture != null)
            {
                Destroy(defaultFoilTexture);
            }
        }

        private void InitializeRig()
        {
            // 1. Crear RenderTexture ARGB32 con canal alfa
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32);
                renderTexture.name = "HoloCard_RenderTexture";
                renderTexture.antiAliasing = 2;
                renderTexture.Create();
            }

            // 2. Localizar Shader
            if (holoShader == null)
            {
                holoShader = Shader.Find("Shader Graphs/HolographicFoilShader");
                if (holoShader == null)
                {
                    holoShader = Shader.Find("Sprites/Default");
                }
            }

            // 3. Crear material instanciado
            if (instanceMaterial == null)
            {
                if (baseMaterial != null)
                {
                    instanceMaterial = new Material(baseMaterial);
                }
                else if (holoShader != null)
                {
                    instanceMaterial = new Material(holoShader);
                }
            }

            // 4. Crear textura base con alfa para que el shader calcule el foil sobre ella
            if (defaultFoilTexture == null)
            {
                defaultFoilTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[128 * 128];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color(1f, 1f, 1f, 0.45f); // Base blanca traslúcida para brillo prismático
                }
                defaultFoilTexture.SetPixels(pixels);
                defaultFoilTexture.Apply();
            }

            if (instanceMaterial != null)
            {
                instanceMaterial.SetTexture(MainTexID, defaultFoilTexture);
                instanceMaterial.SetFloat(HoloIntensityID, 0.75f);
                instanceMaterial.SetFloat(ShimmerSpeedID, 1.6f);
            }

            // 5. Configurar Cámara dedicada en posición aislada (Y = -500)
            Vector3 rigOrigin = new Vector3(0, -500f, 0);
            GameObject camGO = new GameObject("CardHolo_OffscreenCam");
            camGO.transform.SetParent(transform, false);
            camGO.transform.position = rigOrigin + new Vector3(0, 0, -3f);

            offscreenCamera = camGO.AddComponent<Camera>();
            offscreenCamera.clearFlags = CameraClearFlags.SolidColor;
            offscreenCamera.backgroundColor = new Color(0, 0, 0, 0); // Fondo transparente
            offscreenCamera.orthographic = false;
            offscreenCamera.fieldOfView = 38f;
            offscreenCamera.targetTexture = renderTexture;
            offscreenCamera.nearClipPlane = 0.1f;
            offscreenCamera.farClipPlane = 10f;

            // 6. Crear Quad 3D
            quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = "HoloCard_Quad";
            quadObject.transform.SetParent(transform, false);
            quadObject.transform.position = rigOrigin;
            quadTransform = quadObject.transform;
            quadTransform.localScale = new Vector3(1.35f, 1.88f, 1f); // Proporción tarjeta 2:3

            Collider col = quadObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = quadObject.GetComponent<MeshRenderer>();
            if (mr != null && instanceMaterial != null)
            {
                mr.material = instanceMaterial;
            }
        }

        private void Update()
        {
            if (quadTransform == null) return;

            // Suavizado continuo hacia targetTilt
            currentTilt = Vector2.Lerp(currentTilt, targetTilt, Time.deltaTime * returnSpeed);

            // Inclinación 3D
            float rotX = -currentTilt.y * maxTiltAngle;
            float rotY = currentTilt.x * maxTiltAngle;
            quadTransform.localRotation = Quaternion.Euler(rotX, rotY, 0f);

            // Desplazamiento de vector en Shader
            if (instanceMaterial != null)
            {
                instanceMaterial.SetVector(TiltPosID, new Vector4(currentTilt.x * 0.5f, currentTilt.y * 0.5f, 0, 0));
            }
        }

        public void SetTilt(float normX, float normY)
        {
            targetTilt = new Vector2(Mathf.Clamp(normX, -1f, 1f), Mathf.Clamp(normY, -1f, 1f));
        }

        public void ResetTilt()
        {
            targetTilt = Vector2.zero;
        }

        public void SetHoloActive(bool active, float intensity = 0.85f)
        {
            isHoloActive = active;
            if (instanceMaterial != null)
            {
                instanceMaterial.SetFloat(HoloIntensityID, active ? intensity : 0f);
            }
        }

        public void SetCardTexture(Texture2D texture)
        {
            if (instanceMaterial != null && texture != null)
            {
                instanceMaterial.SetTexture(MainTexID, texture);
            }
        }
    }
}
