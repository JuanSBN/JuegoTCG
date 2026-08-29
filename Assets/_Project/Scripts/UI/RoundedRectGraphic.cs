using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class RoundedRectGraphic : MaskableGraphic
    {
        [Header("Rounding & Shape")]
        [SerializeField] private bool isCapsule = false;
        [SerializeField] private float cornerRadius = 16f;
        [Range(4, 32)]
        [SerializeField] private int cornerSegments = 16;

        [Header("Border")]
        [SerializeField] private float borderWidth = 0f;
        [SerializeField] private Color borderColor = Color.clear;

        public bool IsCapsule
        {
            get => isCapsule;
            set { isCapsule = value; SetVerticesDirty(); }
        }

        public float CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = value; SetVerticesDirty(); }
        }

        public float BorderWidth
        {
            get => borderWidth;
            set { borderWidth = value; SetVerticesDirty(); }
        }

        public Color BorderColor
        {
            get => borderColor;
            set { borderColor = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;

            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float rOut = isCapsule ? maxRadius : Mathf.Clamp(cornerRadius, 0f, maxRadius);
            float bWidth = Mathf.Clamp(borderWidth, 0f, maxRadius);
            float rIn = Mathf.Max(0f, rOut - bWidth);

            bool hasBorder = (bWidth > 0f && borderColor.a > 0f);
            bool hasFill = (color.a > 0f);

            // 4 Corner centers
            Vector2[] cornerCenters = new Vector2[4]
            {
                new Vector2(rect.xMax - rOut, rect.yMax - rOut), // Top-Right (0 to 90 deg)
                new Vector2(rect.xMin + rOut, rect.yMax - rOut), // Top-Left (90 to 180 deg)
                new Vector2(rect.xMin + rOut, rect.yMin + rOut), // Bottom-Left (180 to 270 deg)
                new Vector2(rect.xMax - rOut, rect.yMin + rOut)  // Bottom-Right (270 to 360 deg)
            };

            float[] startAngles = new float[4] { 0f, 90f, 180f, 270f };

            int pointsPerCorner = cornerSegments + 1;
            int totalPoints = pointsPerCorner * 4;

            Vector2[] outerPoints = new Vector2[totalPoints];
            Vector2[] innerPoints = new Vector2[totalPoints];

            int idx = 0;
            for (int c = 0; c < 4; c++)
            {
                Vector2 cCenter = cornerCenters[c];
                float startAng = startAngles[c];

                for (int i = 0; i <= cornerSegments; i++)
                {
                    float angleDeg = startAng + (90f / cornerSegments) * i;
                    float angleRad = angleDeg * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                    outerPoints[idx] = cCenter + dir * rOut;
                    innerPoints[idx] = hasBorder ? (cCenter + dir * rIn) : outerPoints[idx];
                    idx++;
                }
            }

            // 1. Draw Hollow Border (Quad strip between outer and inner ring)
            if (hasBorder)
            {
                int baseVert = vh.currentVertCount;
                for (int i = 0; i < totalPoints; i++)
                {
                    UIVertex vOut = UIVertex.simpleVert;
                    vOut.position = outerPoints[i];
                    vOut.color = borderColor;
                    vh.AddVert(vOut);

                    UIVertex vIn = UIVertex.simpleVert;
                    vIn.position = innerPoints[i];
                    vIn.color = borderColor;
                    vh.AddVert(vIn);
                }

                for (int i = 0; i < totalPoints; i++)
                {
                    int next = (i + 1) % totalPoints;

                    int outA = baseVert + i * 2;
                    int inA = baseVert + i * 2 + 1;
                    int outB = baseVert + next * 2;
                    int inB = baseVert + next * 2 + 1;

                    vh.AddTriangle(outA, outB, inA);
                    vh.AddTriangle(outB, inB, inA);
                }
            }

            // 2. Draw Fill (Center fan inside inner ring)
            if (hasFill)
            {
                int baseFillVert = vh.currentVertCount;
                Vector2[] fillPoints = hasBorder ? innerPoints : outerPoints;

                // Center vertex
                UIVertex cVert = UIVertex.simpleVert;
                cVert.position = rect.center;
                cVert.color = color;
                vh.AddVert(cVert);

                for (int i = 0; i < totalPoints; i++)
                {
                    UIVertex fv = UIVertex.simpleVert;
                    fv.position = fillPoints[i];
                    fv.color = color;
                    vh.AddVert(fv);
                }

                for (int i = 0; i < totalPoints; i++)
                {
                    int next = (i + 1) % totalPoints;
                    vh.AddTriangle(baseFillVert, baseFillVert + 1 + i, baseFillVert + 1 + next);
                }
            }
        }
    }
}
