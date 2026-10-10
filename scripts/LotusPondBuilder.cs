using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 程序化生成荷塘场景：荷叶（叶脉/缺口/卷边）+ 莲花（多层花瓣、花蕊、莲蓬）+ 水面、浮瓣、石块、芦苇
/// 使用方式：Unity 菜单 Tools > Lotus Pond > Build All
/// </summary>
public static class LotusPondBuilder
{
    const string Dir = "Assets/LotusPond";
    const string DirAbs = "Assets/LotusPond";

    // ------------------------------------------------------------------ 工具
    static Color Col(float r, float g, float b)
    {
        var c = new Color(r, g, b);
        return c.linear;
    }

    static string P(string file) { return DirAbs + "/" + file; }

    static Material LoadMat(string n)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(P(n + ".mat"));
    }

    static Mesh LoadMesh(string n)
    {
        return AssetDatabase.LoadAssetAtPath<Mesh>(P(n));
    }

    static Material MakeMat(string n, Color c, float smooth)
    {
        var m = new Material(Shader.Find("HDRP/Lit"));
        m.name = n;
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(m, P(n + ".mat"));
        return m;
    }

    /// <summary>把单面网格变成有厚度的双面网格（背面法线翻转，避免单面消失）</summary>
    static Mesh DoubleSided(Mesh src, float thickness)
    {
        src.RecalculateNormals();
        var v = new List<Vector3>(src.vertices);
        var n = new List<Vector3>(src.normals);
        var uv = new List<Vector2>(src.uv);
        var t = new List<int>(src.triangles);
        int cnt = v.Count;
        for (int i = 0; i < cnt; i++)
        {
            v.Add(v[i] - n[i] * thickness);
            n.Add(-n[i]);
            uv.Add(uv[i]);
        }
        int triCount = t.Count;
        for (int i = 0; i < triCount; i += 3)
        {
            t.Add(t[i + 2] + cnt);
            t.Add(t[i + 1] + cnt);
            t.Add(t[i] + cnt);
        }
        var m = new Mesh();
        m.name = src.name;
        m.SetVertices(v);
        m.SetNormals(n);
        m.SetUVs(0, uv);
        m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }

    static Mesh Finish(List<Vector3> verts, List<Vector2> uvs, List<int> tris, string name)
    {
        var m = new Mesh();
        m.name = name;
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        return DoubleSided(m, 0.006f);
    }

    // ------------------------------------------------------------- 网格：荷叶
    /// <summary>荷叶：碟状上卷 + 放射状叶脉起伏 + 边缘微波浪 + 一道 V 形缺口</summary>
    static Mesh BuildLeaf()
    {
        int rings = 14, seg = 84;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int r = 0; r <= rings; r++)
        {
            float rt = (float)r / rings;
            for (int s = 0; s <= seg; s++)
            {
                float a = (float)s / seg * Mathf.PI * 2f;
                float rad = 0.09f + 0.91f * rt;
                // 叶脉：18 条放射脊
                float ridge = 0.012f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 9f)), 8f) * rt;
                // 碟状上卷 + 轻微不对称起伏
                float y = 0.18f * rt * rt + ridge + 0.014f * Mathf.Sin(a * 3f) * rt;
                // 边缘细波浪
                float rim = 1f + 0.018f * Mathf.Sin(a * 26f) * Mathf.Clamp01((rt - 0.78f) * 4.5f);
                float rr = rad * rim;
                verts.Add(new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr));
                uvs.Add(new Vector2(0.5f + Mathf.Cos(a) * rr * 0.5f, 0.5f + Mathf.Sin(a) * rr * 0.5f));
            }
        }

        int stride = seg + 1;
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < seg; s++)
            {
                float a0 = (float)s / seg * Mathf.PI * 2f, a1 = (float)(s + 1) / seg * Mathf.PI * 2f;
                float am = (a0 + a1) * 0.5f;
                float d = Mathf.DeltaAngle(0f, am * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                if (Mathf.Abs(d) < 0.17f) continue;           // V 形缺口
                int i0 = r * stride + s, i1 = i0 + 1, i2 = (r + 1) * stride + s, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i1); tris.Add(i3); tris.Add(i2);
            }
        }
        return Finish(verts, uvs, tris, "LotusLeaf");
    }

    /// <summary>叶脉条：贴在叶面上的深色细条，从叶心放射到叶缘</summary>
    static Mesh BuildVeins(int count)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int k = 0; k < count; k++)
        {
            float a = (float)k / count * Mathf.PI * 2f;
            float d = Mathf.DeltaAngle(0f, a * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (Mathf.Abs(d) < 0.22f) continue;
            int L = 8;
            int start = verts.Count;
            for (int i = 0; i <= L; i++)
            {
                float t = (float)i / L;
                float rad = 0.08f + 0.93f * t;
                float y = 0.18f * rad * rad + 0.014f * Mathf.Sin(a * 3f) * rad + 0.008f;
                float w = 0.019f * (1f - 0.5f * t);
                verts.Add(new Vector3(Mathf.Cos(a) * rad - Mathf.Sin(a) * w, y, Mathf.Sin(a) * rad + Mathf.Cos(a) * w));
                verts.Add(new Vector3(Mathf.Cos(a) * rad + Mathf.Sin(a) * w, y, Mathf.Sin(a) * rad - Mathf.Cos(a) * w));
                uvs.Add(new Vector2(0f, t)); uvs.Add(new Vector2(1f, t));
            }
            for (int i = 0; i < L; i++)
            {
                int b = start + i * 2;
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
            }
        }
        return Finish(verts, uvs, tris, "LotusLeafVeins");
    }

    // ------------------------------------------------------------- 网格：花瓣
    /// <summary>花瓣：水滴形轮廓 + 沿长度外翻 + 横截面内凹成舟形</summary>
    static Mesh BuildPetal(float len, float wide, float bend, float cup)
    {
        int LS = 14, WS = 6;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int i = 0; i <= LS; i++)
        {
            float t = (float)i / LS;
            float z = t * len;
            float y = bend * t * t;
            float w = wide * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f));
            if (w < 0.002f) w = 0.002f;
            for (int j = 0; j <= WS; j++)
            {
                float u = -1f + 2f * (float)j / WS;
                verts.Add(new Vector3(u * w, y + cup * u * u * w, z - cup * 0.3f * u * u * w));
                uvs.Add(new Vector2((float)j / WS, t));
            }
        }
        int stride = WS + 1;
        for (int i = 0; i < LS; i++)
        {
            for (int j = 0; j < WS; j++)
            {
                int b = i * stride + j;
                tris.Add(b); tris.Add(b + stride); tris.Add(b + 1);
                tris.Add(b + 1); tris.Add(b + stride); tris.Add(b + stride + 1);
            }
        }
        return Finish(verts, uvs, tris, "LotusPetal");
    }

    /// <summary>花蕊：围绕花心的一圈细丝（交叉双面薄片）</summary>
    static Mesh BuildStamens(int count, float R, float H)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int k = 0; k < count; k++)
        {
            float a = (float)k / count * Mathf.PI * 2f + 0.2f;
            var baseP = new Vector3(Mathf.Cos(a) * 0.03f, 0.02f, Mathf.Sin(a) * 0.03f);
            var tip = new Vector3(Mathf.Cos(a) * R, H, Mathf.Sin(a) * R);
            var axis = (tip - baseP).normalized;
            var tang = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            var p1 = Vector3.Cross(axis, tang).normalized;
            var p2 = Vector3.Cross(axis, p1).normalized;
            float w0 = 0.013f, w1 = 0.005f;
            for (int q = 0; q < 2; q++)
            {
                var pp = (q == 0) ? p1 : p2;
                int st = verts.Count;
                verts.Add(baseP - pp * w0); verts.Add(baseP + pp * w0);
                verts.Add(tip - pp * w1); verts.Add(tip + pp * w1);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f));
                tris.Add(st); tris.Add(st + 1); tris.Add(st + 2);
                tris.Add(st + 1); tris.Add(st + 3); tris.Add(st + 2);
            }
        }
        return Finish(verts, uvs, tris, "LotusStamens");
    }

    // ------------------------------------------------------------------ 资源
    static void BuildAssets()
    {
        string abs = Application.dataPath + "/LotusPond";
        if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);

        MakeMat("M_LeafTop", Col(0.40f, 0.60f, 0.19f), 0.42f);
        MakeMat("M_LeafVein", Col(0.28f, 0.45f, 0.13f), 0.35f);
        MakeMat("M_Stem", Col(0.34f, 0.48f, 0.22f), 0.30f);
        MakeMat("M_PetalOuter", Col(0.95f, 0.56f, 0.65f), 0.38f);
        MakeMat("M_PetalMid", Col(0.99f, 0.74f, 0.79f), 0.40f);
        MakeMat("M_PetalInner", Col(1.00f, 0.91f, 0.92f), 0.42f);
        MakeMat("M_Stamen", Col(0.98f, 0.85f, 0.40f), 0.25f);
        MakeMat("M_Receptacle", Col(0.70f, 0.79f, 0.34f), 0.35f);
        MakeMat("M_Water", Col(0.05f, 0.30f, 0.38f), 0.97f);
        MakeMat("M_Rock", Col(0.38f, 0.36f, 0.33f), 0.25f);
        MakeMat("M_Reed", Col(0.33f, 0.44f, 0.26f), 0.30f);

        AssetDatabase.CreateAsset(BuildLeaf(), P("LotusLeaf.asset"));
        AssetDatabase.CreateAsset(BuildVeins(18), P("LotusLeafVeins.asset"));
        AssetDatabase.CreateAsset(BuildPetal(1f, 0.16f, 0.30f, 0.55f), P("LotusPetal_L.asset"));
        AssetDatabase.CreateAsset(BuildPetal(1f, 0.20f, 0.14f, 0.62f), P("LotusPetal_W.asset"));
        AssetDatabase.CreateAsset(BuildStamens(28, 0.20f, 0.13f), P("LotusStamens.asset"));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------ 场景
    static GameObject MeshObj(string name, Mesh mesh, Material mat, Transform parent)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        var mf = g.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = g.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        return g;
    }

    static GameObject Prim(PrimitiveType type, Transform parent)
    {
        var g = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        return g;
    }

    /// <summary>一朵莲花：stage 0=花蕾 1=半开 2=盛放</summary>
    static void BuildFlower(Transform parent, Vector3 pos, float scale, int stage, System.Random rnd)
    {
        var head = new GameObject(stage == 0 ? "LotusBud" : (stage == 1 ? "LotusFlower_Half" : "LotusFlower_Full"));
        head.transform.SetParent(parent);
        head.transform.position = pos;
        head.transform.rotation = Quaternion.Euler(
            (float)(rnd.NextDouble() * 8 - 4),
            (float)(rnd.NextDouble() * 360),
            (float)(rnd.NextDouble() * 8 - 4));
        head.transform.localScale = Vector3.one * scale;

        // 花茎（从水面伸到花朵）
        var stem = Prim(PrimitiveType.Cylinder, parent);
        stem.name = "Stem";
        stem.transform.position = new Vector3(pos.x, pos.y * 0.5f, pos.z);
        stem.transform.localScale = new Vector3(0.05f, pos.y, 0.05f);
        stem.transform.rotation = Quaternion.Euler((float)(rnd.NextDouble() * 6 - 3), 0, (float)(rnd.NextDouble() * 6 - 3));
        stem.GetComponent<Renderer>().sharedMaterial = LoadMat("M_Stem");

        // 各轮花瓣参数
        int ringCount = stage == 0 ? 2 : (stage == 1 ? 3 : 4);
        int[] counts = { 8, 8, 7, 5 };
        float[] lens = { 0.62f, 0.50f, 0.38f, 0.26f };
        float[] wides = { 0.17f, 0.15f, 0.12f, 0.09f };
        float[] tilts = { 74f, 52f, 32f, 16f };
        if (stage == 0) { tilts = new float[] { 16f, 8f, 8f, 8f }; lens = new float[] { 0.60f, 0.46f, 0.36f, 0.26f }; }
        if (stage == 1) { tilts = new float[] { 42f, 26f, 12f, 12f }; }

        Mesh petalL = LoadMesh("LotusPetal_L.asset");
        Mesh petalW = LoadMesh("LotusPetal_W.asset");

        for (int ring = 0; ring < ringCount; ring++)
        {
            Material m = ring == 0 ? LoadMat("M_PetalOuter")
                       : ring == 1 ? LoadMat("M_PetalOuter")
                       : ring == 2 ? LoadMat("M_PetalMid")
                                   : LoadMat("M_PetalInner");
            int n = counts[ring];
            float baseTilt = tilts[ring];
            for (int i = 0; i < n; i++)
            {
                float a = (float)i / n * 360f + ring * 21f + (float)(rnd.NextDouble() * 8 - 4);
                float tilt = baseTilt + (float)(rnd.NextDouble() * 8 - 4);
                var p = MeshObj("Petal_r" + ring + "_" + i, ring >= 2 ? petalW : petalL, m, head.transform);
                p.transform.localPosition = Vector3.zero;
                p.transform.localRotation = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(-tilt, 0f, 0f);
                float zs = lens[ring] * (0.92f + 0.16f * (float)rnd.NextDouble());
                float xs = wides[ring] / (ring >= 2 ? 0.20f : 0.16f);
                p.transform.localScale = new Vector3(xs, 1f, zs);
            }
        }

        // 花心：莲蓬 + 花蕊（花蕾不露花心）
        if (stage > 0)
        {
            var core = new GameObject("Core");
            core.transform.SetParent(head.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            var rec = Prim(PrimitiveType.Sphere, core.transform);
            rec.name = "Receptacle";
            rec.transform.localPosition = Vector3.zero;
            rec.transform.localScale = new Vector3(0.17f, 0.10f, 0.17f);
            rec.GetComponent<Renderer>().sharedMaterial = LoadMat("M_Receptacle");

            var stamens = MeshObj("Stamens", LoadMesh("LotusStamens.asset"), LoadMat("M_Stamen"), core.transform);
            stamens.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            stamens.transform.localScale = Vector3.one * (stage == 1 ? 0.85f : 1.15f);
        }
    }

    static void BuildLeafObject(Transform parent, Vector3 pos, float scale, System.Random rnd)
    {
        var g = new GameObject("LotusLeaf");
        g.transform.SetParent(parent);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(
            (float)(rnd.NextDouble() * 6 - 3),
            (float)(rnd.NextDouble() * 360),
            (float)(rnd.NextDouble() * 6 - 3));
        g.transform.localScale = Vector3.one * scale;

        MeshObj("Leaf", LoadMesh("LotusLeaf.asset"), LoadMat("M_LeafTop"), g.transform);
        MeshObj("Veins", LoadMesh("LotusLeafVeins.asset"), LoadMat("M_LeafVein"), g.transform);

        // 叶柄（水下）
        var stem = Prim(PrimitiveType.Cylinder, g.transform);
        stem.name = "Petiole";
        stem.transform.localPosition = new Vector3(0f, -0.28f, 0f);
        stem.transform.localScale = new Vector3(0.05f, 0.55f, 0.05f);
        stem.GetComponent<Renderer>().sharedMaterial = LoadMat("M_Stem");

        // 叶心小凸起
        var nub = Prim(PrimitiveType.Sphere, g.transform);
        nub.name = "LeafNub";
        nub.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        nub.transform.localScale = new Vector3(0.09f, 0.05f, 0.09f);
        nub.GetComponent<Renderer>().sharedMaterial = LoadMat("M_LeafVein");
    }

    [MenuItem("Tools/Lotus Pond/Build All")]
    public static string BuildAll()
    {
        BuildAssets();

        // 清理旧场景对象
        var old = GameObject.Find("LotusPond");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("LotusPond");
        var rnd = new System.Random(20261010);

        // ---- 水面 ----
        var water = Prim(PrimitiveType.Plane, root.transform);
        water.name = "Water";
        water.transform.position = Vector3.zero;
        water.transform.localScale = new Vector3(9f, 1f, 9f);   // 90 x 90
        water.GetComponent<Renderer>().sharedMaterial = LoadMat("M_Water");

        // ---- 荷叶 ----
        var leafGroup = new GameObject("LotusLeaves");
        leafGroup.transform.SetParent(root.transform);
        float[][] leafSpots = new float[][]
        {
            new float[]{-6.5f,-4.2f,2.1f}, new float[]{4.8f,-6.1f,1.6f}, new float[]{-2.2f,3.4f,1.35f},
            new float[]{8.6f,1.9f,1.9f},  new float[]{-9.4f,2.6f,2.3f}, new float[]{6.1f,7.2f,1.25f},
            new float[]{0.4f,-9.8f,1.8f}, new float[]{-3.8f,-1.6f,0.95f}, new float[]{11.5f,-3.4f,1.7f},
            new float[]{-12.2f,-5.8f,1.5f}, new float[]{2.6f,10.4f,2.0f}, new float[]{-7.8f,8.6f,1.4f},
            new float[]{13.8f,6.2f,1.55f}, new float[]{9.2f,-9.9f,1.3f}, new float[]{-14.0f,6.8f,1.8f},
            new float[]{5.4f,-2.4f,1.1f}
        };
        foreach (var s in leafSpots)
            BuildLeafObject(leafGroup.transform, new Vector3(s[0], 0.06f, s[1]), s[2], rnd);

        // ---- 莲花 ----
        var flowerGroup = new GameObject("LotusFlowers");
        flowerGroup.transform.SetParent(root.transform);
        BuildFlower(flowerGroup.transform, new Vector3(-1.2f, 0.95f, 0.6f), 1.00f, 2, rnd);
        BuildFlower(flowerGroup.transform, new Vector3(3.4f, 1.15f, -2.6f), 1.10f, 2, rnd);
        BuildFlower(flowerGroup.transform, new Vector3(-5.2f, 0.78f, 4.4f), 0.85f, 2, rnd);
        BuildFlower(flowerGroup.transform, new Vector3(6.9f, 0.62f, 5.1f), 0.90f, 1, rnd);
        BuildFlower(flowerGroup.transform, new Vector3(-8.4f, 0.70f, -3.8f), 0.80f, 1, rnd);
        BuildFlower(flowerGroup.transform, new Vector3(1.8f, 1.35f, 6.6f), 0.95f, 0, rnd);
        BuildFlower(flowerGroup.transform, new Vector3(-3.1f, 1.55f, -6.2f), 0.88f, 0, rnd);
        BuildFlower(flowerGroup.transform, new Vector3(9.9f, 0.55f, 0.2f), 0.75f, 1, rnd);

        // ---- 水面浮瓣 ----
        var petalGroup = new GameObject("FloatingPetals");
        petalGroup.transform.SetParent(root.transform);
        Mesh petal = LoadMesh("LotusPetal_L.asset");
        Material pMat = LoadMat("M_PetalOuter");
        for (int i = 0; i < 34; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float r = 1.5f + (float)rnd.NextDouble() * 11f;
            var p = MeshObj("Petal_" + i, petal, pMat, petalGroup.transform);
            p.transform.position = new Vector3(Mathf.Cos(a) * r, 0.022f, Mathf.Sin(a) * r);
            p.transform.rotation = Quaternion.Euler(
                (float)(rnd.NextDouble() * 10 - 5),
                (float)(rnd.NextDouble() * 360),
                (float)(rnd.NextDouble() * 10 - 5));
            float s = 0.30f + (float)rnd.NextDouble() * 0.22f;
            p.transform.localScale = new Vector3(1.1f, 1f, s);
        }

        // ---- 岸边石块 ----
        var rockGroup = new GameObject("Rocks");
        rockGroup.transform.SetParent(root.transform);
        Material rockMat = LoadMat("M_Rock");
        for (int i = 0; i < 12; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float r = 15f + (float)rnd.NextDouble() * 6f;
            var rock = Prim(PrimitiveType.Sphere, rockGroup.transform);
            rock.name = "Rock_" + i;
            rock.transform.position = new Vector3(Mathf.Cos(a) * r, -0.35f + (float)rnd.NextDouble() * 0.5f, Mathf.Sin(a) * r);
            float s = 0.9f + (float)rnd.NextDouble() * 1.8f;
            rock.transform.localScale = new Vector3(s * 1.3f, s * 0.6f, s);
            rock.transform.rotation = Quaternion.Euler(0f, (float)(rnd.NextDouble() * 360), 0f);
            rock.GetComponent<Renderer>().sharedMaterial = rockMat;
        }

        // ---- 远处芦苇 ----
        var reedGroup = new GameObject("Reeds");
        reedGroup.transform.SetParent(root.transform);
        Material reedMat = LoadMat("M_Reed");
        for (int i = 0; i < 26; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float r = 13f + (float)rnd.NextDouble() * 8f;
            float h = 1.8f + (float)rnd.NextDouble() * 2.2f;
            var reed = Prim(PrimitiveType.Cylinder, reedGroup.transform);
            reed.name = "Reed_" + i;
            reed.transform.position = new Vector3(Mathf.Cos(a) * r, h * 0.5f - 0.2f, Mathf.Sin(a) * r);
            reed.transform.localScale = new Vector3(0.035f, h, 0.035f);
            reed.transform.rotation = Quaternion.Euler((float)(rnd.NextDouble() * 16 - 8), 0f, (float)(rnd.NextDouble() * 16 - 8));
            reed.GetComponent<Renderer>().sharedMaterial = reedMat;
        }

        // ---- 相机与阳光 ----
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(-4.2f, 1.45f, -7.6f);
            cam.transform.LookAt(new Vector3(0.5f, 0.45f, 0.4f));
        }
        var sun = GameObject.Find("Sun");
        if (sun != null)
        {
            var l = sun.GetComponent<Light>();
            if (l != null) l.color = new Color(1f, 0.94f, 0.84f);
            sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
        }

        AssetDatabase.SaveAssets();
        return "LotusPond built: 16 leaves, 8 flowers (buds/half/full), 34 floating petals, rocks, reeds.";
    }
}
