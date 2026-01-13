using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RandomForestMultiEditor : EditorWindow
{
    [MenuItem("Tools/Create Random Forest Multi")] 
    public static void ShowWindow() 
    { 
        GetWindow<RandomForestMultiEditor>("Random Forest"); 
    }

    // Tree prefabs
    public GameObject tree1;
    public GameObject tree2;
    public GameObject tree3;
    public GameObject tree4;
    public GameObject tree5;

    // Tree counts
    public int treeCount1 = 20;
    public int treeCount2 = 20;
    public int treeCount3 = 20;
    public int treeCount4 = 20;
    public int treeCount5 = 20;

    // Scale settings per tree
    public float minScale1 = 2f;
    public float maxScale1 = 3.5f;
    public float minScale2 = 2f;
    public float maxScale2 = 3.5f;
    public float minScale3 = 2f;
    public float maxScale3 = 3.5f;
    public float minScale4 = 2f;
    public float maxScale4 = 3.5f;
    public float minScale5 = 2f;
    public float maxScale5 = 3.5f;

    // Slope settings per tree
    public float maxSlope1 = 40f;
    public bool slopeDensityFade1 = true;
    public float maxSlope2 = 40f;
    public bool slopeDensityFade2 = true;
    public float maxSlope3 = 40f;
    public bool slopeDensityFade3 = true;
    public float maxSlope4 = 40f;
    public bool slopeDensityFade4 = true;
    public float maxSlope5 = 40f;
    public bool slopeDensityFade5 = true;

    // Colors
    public Color tree1LeafColor = Color.green;
    public Color tree2LeafColor = Color.green;
    public Color tree3LeafColor = Color.green;
    public Color tree4LeafColor = Color.green;
    public Color tree5LeafColor = Color.green;

    // Alpha cutoff per tree type
    public float tree1AlphaCutoff = 0.0f;
    public float tree2AlphaCutoff = 0.0f;
    public float tree3AlphaCutoff = 0.0f;
    public float tree4AlphaCutoff = 0.0f;
    public float tree5AlphaCutoff = 0.0f;

    // Forest generation area
    public Vector2 forestSize = new Vector2(500, 500);
    public Terrain targetTerrain;
    
    // Center point options
    public bool useSelectedObject = false;

    private GameObject forestParent;
    private Vector2 scrollPos;

    // Helper class for tree data
    private class TreeData
    {
        public GameObject prefab;
        public int count;
        public float minScale;
        public float maxScale;
        public float maxSlope;
        public bool slopeDensityFade;
        public Color leafColor;
        public float alphaCutoff;

        public TreeData(GameObject prefab, int count, float minScale, float maxScale, float maxSlope, bool slopeDensityFade, Color leafColor, float alphaCutoff)
        {
            this.prefab = prefab;
            this.count = count;
            this.minScale = minScale;
            this.maxScale = maxScale;
            this.maxSlope = maxSlope;
            this.slopeDensityFade = slopeDensityFade;
            this.leafColor = leafColor;
            this.alphaCutoff = alphaCutoff;
        }
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (targetTerrain == null) return;

        Vector3 generationCenter;
        if (useSelectedObject && Selection.activeGameObject != null)
        {
            generationCenter = Selection.activeGameObject.transform.position;
        }
        else
        {
            generationCenter = targetTerrain.transform.position + new Vector3(targetTerrain.terrainData.size.x/2f, 0, targetTerrain.terrainData.size.z/2f);
        }

        // Sample terrain height at center and adjust Y position
        float terrainHeight = targetTerrain.SampleHeight(generationCenter) + targetTerrain.transform.position.y;
        generationCenter.y = terrainHeight;

        // Draw wireframe box showing generation area
        Handles.color = Color.yellow;
        Vector3 size = new Vector3(forestSize.x, 100f, forestSize.y);
        Handles.DrawWireCube(generationCenter, size);

        // Draw corner markers
        float halfX = forestSize.x / 2f;
        float halfZ = forestSize.y / 2f;
        Handles.color = Color.red;
        Handles.SphereHandleCap(0, generationCenter + new Vector3(halfX, 0, halfZ), Quaternion.identity, 5f, EventType.Repaint);
        Handles.SphereHandleCap(0, generationCenter + new Vector3(-halfX, 0, halfZ), Quaternion.identity, 5f, EventType.Repaint);
        Handles.SphereHandleCap(0, generationCenter + new Vector3(halfX, 0, -halfZ), Quaternion.identity, 5f, EventType.Repaint);
        Handles.SphereHandleCap(0, generationCenter + new Vector3(-halfX, 0, -halfZ), Quaternion.identity, 5f, EventType.Repaint);

        // Draw label
        Handles.Label(generationCenter + new Vector3(0, 50f, 0), "Forest Generation Area\n" + forestSize.x + " x " + forestSize.y);
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        forestSize = EditorGUILayout.Vector2Field("Forest Size (X,Z)", forestSize);
        
        EditorGUILayout.Space();
        useSelectedObject = EditorGUILayout.Toggle("Use Selected Object as Center", useSelectedObject);
        if (useSelectedObject)
        {
            if (Selection.activeGameObject != null)
            {
                EditorGUILayout.HelpBox("Center: " + Selection.activeGameObject.name + " at " + Selection.activeGameObject.transform.position, MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No object selected. Will use terrain center.", MessageType.Warning);
            }
        }

        EditorGUILayout.Space();
        GUILayout.Label("increase alpha cutoff to remove branches from the trees", EditorStyles.boldLabel);
        GUILayout.Label("=== Tree 1 ===", EditorStyles.boldLabel);
        tree1 = (GameObject)EditorGUILayout.ObjectField("Prefab", tree1, typeof(GameObject), true);
        treeCount1 = EditorGUILayout.IntField("Count", treeCount1);
        minScale1 = EditorGUILayout.FloatField("Min Scale", minScale1);
        maxScale1 = EditorGUILayout.FloatField("Max Scale", maxScale1);
        maxSlope1 = EditorGUILayout.FloatField("Max Slope", maxSlope1);
        slopeDensityFade1 = EditorGUILayout.Toggle("Fade Density on Slope", slopeDensityFade1);
        tree1LeafColor = EditorGUILayout.ColorField("Leaf Color", tree1LeafColor);
        tree1AlphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", tree1AlphaCutoff, 0, 1);

        EditorGUILayout.Space();
        GUILayout.Label("=== Tree 2 ===", EditorStyles.boldLabel);
        tree2 = (GameObject)EditorGUILayout.ObjectField("Prefab", tree2, typeof(GameObject), true);
        treeCount2 = EditorGUILayout.IntField("Count", treeCount2);
        minScale2 = EditorGUILayout.FloatField("Min Scale", minScale2);
        maxScale2 = EditorGUILayout.FloatField("Max Scale", maxScale2);
        maxSlope2 = EditorGUILayout.FloatField("Max Slope", maxSlope2);
        slopeDensityFade2 = EditorGUILayout.Toggle("Fade Density on Slope", slopeDensityFade2);
        tree2LeafColor = EditorGUILayout.ColorField("Leaf Color", tree2LeafColor);
        tree2AlphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", tree2AlphaCutoff, 0, 1);

        EditorGUILayout.Space();
        GUILayout.Label("=== Tree 3 ===", EditorStyles.boldLabel);
        tree3 = (GameObject)EditorGUILayout.ObjectField("Prefab", tree3, typeof(GameObject), true);
        treeCount3 = EditorGUILayout.IntField("Count", treeCount3);
        minScale3 = EditorGUILayout.FloatField("Min Scale", minScale3);
        maxScale3 = EditorGUILayout.FloatField("Max Scale", maxScale3);
        maxSlope3 = EditorGUILayout.FloatField("Max Slope", maxSlope3);
        slopeDensityFade3 = EditorGUILayout.Toggle("Fade Density on Slope", slopeDensityFade3);
        tree3LeafColor = EditorGUILayout.ColorField("Leaf Color", tree3LeafColor);
        tree3AlphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", tree3AlphaCutoff, 0, 1);

        EditorGUILayout.Space();
        GUILayout.Label("=== Tree 4 ===", EditorStyles.boldLabel);
        tree4 = (GameObject)EditorGUILayout.ObjectField("Prefab", tree4, typeof(GameObject), true);
        treeCount4 = EditorGUILayout.IntField("Count", treeCount4);
        minScale4 = EditorGUILayout.FloatField("Min Scale", minScale4);
        maxScale4 = EditorGUILayout.FloatField("Max Scale", maxScale4);
        maxSlope4 = EditorGUILayout.FloatField("Max Slope", maxSlope4);
        slopeDensityFade4 = EditorGUILayout.Toggle("Fade Density on Slope", slopeDensityFade4);
        tree4LeafColor = EditorGUILayout.ColorField("Leaf Color", tree4LeafColor);
        tree4AlphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", tree4AlphaCutoff, 0, 1);

        EditorGUILayout.Space();
        GUILayout.Label("=== Tree 5 ===", EditorStyles.boldLabel);
        tree5 = (GameObject)EditorGUILayout.ObjectField("Prefab", tree5, typeof(GameObject), true);
        treeCount5 = EditorGUILayout.IntField("Count", treeCount5);
        minScale5 = EditorGUILayout.FloatField("Min Scale", minScale5);
        maxScale5 = EditorGUILayout.FloatField("Max Scale", maxScale5);
        maxSlope5 = EditorGUILayout.FloatField("Max Slope", maxSlope5);
        slopeDensityFade5 = EditorGUILayout.Toggle("Fade Density on Slope", slopeDensityFade5);
        tree5LeafColor = EditorGUILayout.ColorField("Leaf Color", tree5LeafColor);
        tree5AlphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", tree5AlphaCutoff, 0, 1);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Forest", GUILayout.Height(30)))
        {
            GenerateForest();
        }

        EditorGUILayout.EndScrollView();
    }

    void GenerateForest()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("No terrain assigned.");
            return;
        }

        // Always create a new parent object with timestamp
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
        forestParent = new GameObject("Generated Forest " + timestamp);

        List<TreeData> trees = new List<TreeData>();
        trees.Add(new TreeData(tree1, treeCount1, minScale1, maxScale1, maxSlope1, slopeDensityFade1, tree1LeafColor, tree1AlphaCutoff));
        trees.Add(new TreeData(tree2, treeCount2, minScale2, maxScale2, maxSlope2, slopeDensityFade2, tree2LeafColor, tree2AlphaCutoff));
        trees.Add(new TreeData(tree3, treeCount3, minScale3, maxScale3, maxSlope3, slopeDensityFade3, tree3LeafColor, tree3AlphaCutoff));
        trees.Add(new TreeData(tree4, treeCount4, minScale4, maxScale4, maxSlope4, slopeDensityFade4, tree4LeafColor, tree4AlphaCutoff));
        trees.Add(new TreeData(tree5, treeCount5, minScale5, maxScale5, maxSlope5, slopeDensityFade5, tree5LeafColor, tree5AlphaCutoff));

        Vector3 generationCenter;
        if (useSelectedObject && Selection.activeGameObject != null)
        {
            generationCenter = Selection.activeGameObject.transform.position;
        }
        else
        {
            generationCenter = targetTerrain.transform.position + new Vector3(targetTerrain.terrainData.size.x/2f, 0, targetTerrain.terrainData.size.z/2f);
        }

        foreach (TreeData treeData in trees)
        {
            if (treeData.prefab == null || treeData.count <= 0) 
            {
                continue;
            }
            
            int attempts = 0;
            int placed = 0;

            while (placed < treeData.count && attempts < treeData.count * 10)
            {
                attempts++;
                float x = Random.Range(-forestSize.x/2f, forestSize.x/2f);
                float z = Random.Range(-forestSize.y/2f, forestSize.y/2f);
                Vector3 pos = generationCenter + new Vector3(x, 0, z);

                float terrainHeight = targetTerrain.SampleHeight(pos) + targetTerrain.transform.position.y;
                pos.y = terrainHeight;

                float normalizedX = (pos.x - targetTerrain.transform.position.x) / targetTerrain.terrainData.size.x;
                float normalizedZ = (pos.z - targetTerrain.transform.position.z) / targetTerrain.terrainData.size.z;
                Vector3 normal = targetTerrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
                float slope = Vector3.Angle(Vector3.up, normal);

                if (slope > treeData.maxSlope) 
                {
                    continue;
                }

                float densityMultiplier = 1f;
                if (treeData.slopeDensityFade && slope > 0)
                {
                    densityMultiplier = 1f - (slope / treeData.maxSlope);
                }
                if (Random.value > densityMultiplier)
                {
                    continue;
                }

                GameObject instance;
                
                // Check if it's a prefab or scene object
                if (PrefabUtility.GetPrefabType(treeData.prefab) != PrefabType.None)
                {
                    // It's a prefab
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(treeData.prefab);
                }
                else
                {
                    // It's a scene object, instantiate normally
                    instance = (GameObject)Instantiate(treeData.prefab);
                }
                
                instance.transform.position = pos;
                instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, Random.Range(0, 360), 0);
                float scale = Random.Range(treeData.minScale, treeData.maxScale);
                instance.transform.localScale = Vector3.one * scale;
                instance.transform.parent = forestParent.transform;

                // Apply material properties by creating material instances
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                for (int r = 0; r < renderers.Length; r++)
                {
                    Renderer rend = renderers[r];
                    Material[] sharedMats = rend.sharedMaterials;
                    
                    for (int i = 0; i < sharedMats.Length; i++)
                    {
                        if (sharedMats[i] != null)
                        {
                            // Create a new material instance
                            Material newMat = new Material(sharedMats[i]);
                            
                            // Try multiple common color properties
                            if (newMat.HasProperty("_Color"))
                            {
                                newMat.color = treeData.leafColor;
                            }
                            if (newMat.HasProperty("_MainColor"))
                            {
                                newMat.SetColor("_MainColor", treeData.leafColor);
                            }
                            if (newMat.HasProperty("_TintColor"))
                            {
                                newMat.SetColor("_TintColor", treeData.leafColor);
                            }
                            if (newMat.HasProperty("_HueVariation"))
                            {
                                // For Nature shaders, HueVariation is often RGBA where RGB is the color and A is strength
                                Color hueVar = treeData.leafColor;
                                hueVar.a = 0.5f; // 50% blend strength
                                newMat.SetColor("_HueVariation", hueVar);
                            }
                            if (newMat.HasProperty("_Cutoff"))
                            {
                                newMat.SetFloat("_Cutoff", treeData.alphaCutoff);
                            }
                            
                            // Create a temporary array to replace just this material
                            Material[] tempMats = rend.sharedMaterials;
                            tempMats[i] = newMat;
                            rend.sharedMaterials = tempMats;
                        }
                    }
                }

                placed++;
            }
        }

        Debug.Log("Forest generation complete.");
    }
}
