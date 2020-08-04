using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace MyUI
{
    public class FlexibleScollBar : MaskableGraphic
    {
        #region 辅助类
        [Serializable]
        public class Point
        {
            public Vector2 position;
            public Vector2 tanget;

            public Vector2 normal
            {
                get
                {
                    Vector2 normalizedTanget = tanget.normalized;
                    return new Vector2(-normalizedTanget.y, normalizedTanget.x);
                }
            }
        }
        #endregion

        #region 共有变量
        public Sprite sprite
        {
            get { return m_Sprite; }
            set
            {
                if (m_Sprite != null)
                {
                    if (m_Sprite != value)
                    {
                        m_SkipLayoutUpdate = m_Sprite.rect.size.Equals(value ? value.rect.size : Vector2.zero);
                        m_SkipMaterialUpdate = m_Sprite.texture == (value ? value.texture : null);
                        m_Sprite = value;

                        SetAllDirty();
                        TrackSprite();
                    }
                }
                else if (value != null)
                {
                    m_SkipLayoutUpdate = value.rect.size == Vector2.zero;
                    m_SkipMaterialUpdate = value.texture == null;
                    m_Sprite = value;

                    SetAllDirty();
                    TrackSprite();
                }
            }
        }

        public Sprite overrideSprite
        {
            get { return activeSprite; }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_OverrideSprite, value))
                {
                    SetAllDirty();
                    TrackSprite();
                }
            }
        }

        public static Material defaultETC1GraphicMaterial
        {
            get
            {
                if (s_ETC1DefaultUI == null)
                {
                    s_ETC1DefaultUI = Canvas.GetETC1SupportedCanvasMaterial();
                }
                return s_ETC1DefaultUI;
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (activeSprite == null)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        return material.mainTexture;
                    }
                    return s_WhiteTexture;
                }
                return activeSprite.texture;
            }
        }

        public bool hasBorder
        {
            get
            {
                if(activeSprite != null)
                {
                    Vector4 v = activeSprite.border;
                    return v.sqrMagnitude > 0f;
                }
                return false;
            }
        }

        public float pixelsPerUnit
        {
            get
            {
                float spritePixelsPerUnit = 100;
                if (activeSprite)
                {
                    spritePixelsPerUnit = activeSprite.pixelsPerUnit;
                }
                if (canvas)
                {
                    m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
                }
                return spritePixelsPerUnit / m_CachedReferencePixelsPerUnit;
            }
        }

        public override Material material
        {
            get
            {
                if (m_Material != null)
                    return m_Material;
#if UNITY_EDITOR
                if (Application.isPlaying && activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                    return defaultETC1GraphicMaterial;
#else

                if (activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                    return defaultETC1GraphicMaterial;
#endif

                return defaultMaterial;
            }

            set
            {
                base.material = value;
            }
        }
        #endregion

        #region 私有变量
        static protected Material s_ETC1DefaultUI = null;

        [NonSerialized] protected bool m_SkipLayoutUpdate;
        [NonSerialized] protected bool m_SkipMaterialUpdate;

        [SerializeField] private Sprite m_Sprite;

        [NonSerialized] private Sprite m_OverrideSprite;

        private Sprite activeSprite { get { return m_OverrideSprite != null ? m_OverrideSprite : sprite; } }

        private float m_CachedReferencePixelsPerUnit = 100;

        [SerializeField] private List<Point> m_Points;
        #endregion

        #region 公有方法

        #endregion

        #region 私有方法
        private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect)
        {
            Rect originalRect = rectTransform.rect;

            for (int axis = 0; axis <= 1; axis++)
            {
                float borderScaleRatio;

                // The adjusted rect (adjusted for pixel correctness)
                // may be slightly larger than the original rect.
                // Adjust the border to match the adjustedRect to avoid
                // small gaps between borders (case 833201).
                if (originalRect.size[axis] != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
                // In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
                float combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }

        private void TrackSprite()
        {
            // wait to do
        }

        /// <summary>
        /// 获得曲线上的点的数据，t取[0,1]
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="t"></param>
        public static Point GetHermiteCurvePoint(Point start, Point end, float t)
        {
            Vector4 factory = GetHermiteCurveFactory(t);
            float positionX = Vector4.Dot(factory, new Vector4(start.position.x, end.position.x, start.tanget.x, end.tanget.x));
            float positionY = Vector4.Dot(factory, new Vector4(start.position.y, end.position.y, start.tanget.y, end.tanget.y));

            Vector4 tangetFactory = GetHermiteCurveTangentFactory(t);
            float tangentX = Vector4.Dot(tangetFactory, new Vector4(start.position.x, end.position.x, start.tanget.x, end.tanget.x));
            float tangentY = Vector4.Dot(tangetFactory, new Vector4(start.position.y, end.position.y, start.tanget.y, end.tanget.y));

            return new Point { position = new Vector2(positionX, positionY), tanget = new Vector2(tangentX, tangentY) };
        }

        /// <summary>
        /// 获得直线上的点的数据，t的有效范围为[0,1]
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        static Point GetLinearCurvePoint(Point start, Point end, float t)
        {
            t = Mathf.Clamp01(t);

            Vector2 position = start.position + (end.position - start.position) * t;
            return new Point { position = position };
        }

        /// <summary>
        /// 计算三次Hermite插值基函数系数值，返回值的结构为[alpha0, alpha1, beta0, beta1]
        /// f(t) = alpha0*f(0)+alpha1*f(1)+beta0*f'(0)+beta1*f'(1)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector4 GetHermiteCurveFactory(float t)
        {
            return new Vector4((t - 1) * (t - 1) * (1 + 2 * t), t * t * (3 - 2 * t), t * (t - 1) * (t - 1), t * t * (t - 1));
        }

        /// <summary>
        /// 计算三次Hermite插值基函数导数值，返回值的结构为[alpha0, alpha1, beta0, beta1]
        /// f'(t) = alpha0*f(0)+alpha1*f(1)+beta0*f'(0)+beta1*f'(1)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector4 GetHermiteCurveTangentFactory(float t)
        {
            return new Vector4(6 * t * (t - 1), 6 * t * (1 - t), (2 * t + 1) * (t - 1), t * (3 * t - 2));
        }

        static void AddQuad(VertexHelper vertexHelper, Vector3[] quadPositions, Color32 color, Vector3[] quadUVs)
        {
            int startIndex = vertexHelper.currentVertCount;

#if UNITY_EDITOR
            string addVectexLog = "Add Quad: \n" ;
#endif

            for (int i = 0; i < 4; ++i)
            {
#if UNITY_EDITOR
                addVectexLog += "Add Vertex: " + quadPositions[i] + "\n";
#endif
                vertexHelper.AddVert(quadPositions[i], color, quadUVs[i]);
            }
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);

#if UNITY_EDITOR
            Debug.Log(addVectexLog);
#endif
        }

        static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax)
        {
            int startIndex = vertexHelper.currentVertCount;

            vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
            vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
            vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
            vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        static void AddVertex(VertexHelper vertexHelper, Point start, Point end, Color32 color, Vector2 startUV, Vector2 endUV, float width)
        {
            Vector2 tanget = (start.position - end.position).normalized;
            Vector2 normal = new Vector2(-tanget.y, tanget.x);

            Vector3[] vertexes = new Vector3[4];

            vertexes[0] = start.position - normal * width;
            vertexes[1] = start.position + normal * width;
            vertexes[2] = end.position + normal * width;
            vertexes[3] = end.position - normal * width;

            Vector3[] uvs = new Vector3[4];
            uvs[0] = new Vector3(startUV.x, startUV.y, 0);
            uvs[1] = new Vector3(startUV.x, endUV.y, 0);
            uvs[2] = new Vector3(endUV.x, endUV.y, 0);
            uvs[3] = new Vector3(endUV.x, startUV.y, 0);

            AddQuad(vertexHelper, vertexes, color, uvs);
        }
        #endregion

        #region 重写方法
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            // 点的数量不够，不进行重新绘制
            if (m_Points.Count <= 1)
            {
                return;
            }

            // 获取图片基本信息
            Vector4 outer, inner, border;
            Vector2 spriteSize;

            if (activeSprite != null)
            {
                outer = DataUtility.GetOuterUV(activeSprite);
                inner = DataUtility.GetInnerUV(activeSprite);
                border = activeSprite.border;
                spriteSize = activeSprite.rect.size;
            }
            else
            {
                outer = Vector4.zero;
                inner = Vector4.zero;
                border = Vector4.zero;
                spriteSize = Vector2.one * 100;
            }

            Rect rect = GetPixelAdjustedRect();
            float tileWidth = (spriteSize.x - border.x - border.z) / pixelsPerUnit;
            float tileHeight = (spriteSize.y - border.y - border.w) / pixelsPerUnit;
            border = GetAdjustedBorders(border / pixelsPerUnit, rect);

            var uvMin = new Vector2(inner.x, inner.y);
            var uvMax = new Vector2(inner.z, inner.w);

            // Min to max max range for tiled region in coordinates relative to lower left corner.
            float xMin = border.x;
            float xMax = rect.width - border.z;
            float yMin = border.y;
            float yMax = rect.height - border.w;

            string imageData = string.Empty;
            imageData += string.Format("uvMin: {0}\n", uvMin);
            imageData += string.Format("uvMax: {0}\n", uvMax);
            imageData += string.Format("tileWidth: {0}\n", tileWidth);
            imageData += string.Format("tileHeight: {0}\n", tileHeight);
            Debug.Log(imageData);
            
            if (tileWidth <= 0)
                tileWidth = xMax - xMin;

            if (tileHeight <= 0)
                tileHeight = yMax - yMin;

            float length = 0;
            for(int i = 1; i < m_Points.Count; ++i)
            {

                float segmentLength = Vector2.Distance(m_Points[i - 1].position, m_Points[i].position);

                float startUV = length % tileWidth;

                //绘制开头
                float startLine = 0;
                while (startLine < segmentLength)
                {
                    float nextLine = startLine + tileWidth - startUV;
                    if (nextLine > segmentLength)
                    {
                        nextLine = segmentLength;
                    }

                    Point subStartPoint = GetHermiteCurvePoint(m_Points[i - 1], m_Points[i], startLine / segmentLength);
                    Point subEndPoint = GetHermiteCurvePoint(m_Points[i - 1], m_Points[i], nextLine / segmentLength);

                    float startU = startUV / tileWidth;
                    float endU = startU + (nextLine - startLine) / tileWidth;

                    AddVertex(vertexHelper, subStartPoint, subEndPoint, color, new Vector2(startU, uvMin.y), new Vector2(endU, uvMax.y), tileHeight/2);

                    startUV = 0f;
                    startLine = nextLine;
                }

                length += segmentLength;
            }

        }

        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();

            if (activeSprite == null)
            {
                canvasRenderer.SetAlphaTexture(null);
                return;
            }

            Texture2D alphaTex = activeSprite.associatedAlphaSplitTexture;

            if (alphaTex != null)
            {
                canvasRenderer.SetAlphaTexture(alphaTex);
            }
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (canvas == null)
            {
                m_CachedReferencePixelsPerUnit = 100;
            }
            else if (canvas.referencePixelsPerUnit != m_CachedReferencePixelsPerUnit)
            {
                m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
            }
        }


        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }
        #endregion
    }
}