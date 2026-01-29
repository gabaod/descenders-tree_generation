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

    // Dynamic tree list
    private List<TreeSettings> treeSettings = new List<TreeSettings>();

    // Forest generation area
    public Vector2 forestSize = new Vector2(500, 500);
    public Terrain targetTerrain;

    // Center point options
    public bool useSelectedObject = false;

    // Clustering settings
    public bool useNaturalClustering = false;
    public float clusterScale = 100f;
    public float clusterStrength = 0.7f;

    private GameObject forestParent;
    private Vector2 scrollPos;

    // Tree settings class
    [System.Serializable]
    private class TreeSettings
    {
        public GameObject prefab;
        public int count = 20;
        public float minScale = 2f;
        public float maxScale = 3.5f;
        public float maxSlope = 40f;
        public bool slopeDensityFade = true;
        public Gradient leafColorGradient;
        public float alphaCutoff = 0.0f;
        public bool foldout = true;
        public bool ignoreLeafCheck = false; // Apply color to all materials
        
        // Optional material assignments
        public Material leafMaterial;
        public Material branchMaterial;
        public Material trunkMaterial;
        
        // Rotation settings
        public bool randomRotationX = false;
        public float minRotationX = 0f;
        public float maxRotationX = 0f;
        public bool randomRotationZ = false;
        public float minRotationZ = 0f;
        public float maxRotationZ = 0f;

        public TreeSettings()
        {
            // Initialize with a default green gradient
            leafColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.6f, 0.2f), 0.0f);
            colorKeys[1] = new GradientColorKey(new Color(0.4f, 0.8f, 0.3f), 1.0f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            leafColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
        
        // Initialize with one tree slot if empty
        if (treeSettings.Count == 0)
        {
            treeSettings.Add(new TreeSettings());
        }
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
            generationCenter = targetTerrain.transform.position + new Vector3(targetTerrain.terrainData.size.x / 2f, 0, targetTerrain.terrainData.size.z / 2f);
        }

        float terrainHeight = targetTerrain.SampleHeight(generationCenter) + targetTerrain.transform.position.y;
        generationCenter.y = terrainHeight;

        Handles.color = Color.yellow;
        Vector3 size = new Vector3(forestSize.x, 100f, forestSize.y);
        Handles.DrawWireCube(generationCenter, size);

        float halfX = forestSize.x / 2f;
        float halfZ = forestSize.y / 2f;
        Handles.color = Color.red;
        Handles.SphereHandleCap(0, generationCenter + new Vector3(halfX, 0, halfZ), Quaternion.identity, 5f, EventType.Repaint);
        Handles.SphereHandleCap(0, generationCenter + new Vector3(-halfX, 0, halfZ), Quaternion.identity, 5f, EventType.Repaint);
        Handles.SphereHandleCap(0, generationCenter + new Vector3(halfX, 0, -halfZ), Quaternion.identity, 5f, EventType.Repaint);
        Handles.SphereHandleCap(0, generationCenter + new Vector3(-halfX, 0, -halfZ), Quaternion.identity, 5f, EventType.Repaint);

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
        GUILayout.Label("=== Clustering Settings ===", EditorStyles.boldLabel);
        useNaturalClustering = EditorGUILayout.Toggle("Natural Clustering", useNaturalClustering);
        if (useNaturalClustering)
        {
            EditorGUILayout.HelpBox("Creates natural patches where similar trees group together.", MessageType.Info);
            clusterScale = EditorGUILayout.Slider("Cluster Size", clusterScale, 30f, 300f);
            clusterStrength = EditorGUILayout.Slider("Cluster Strength", clusterStrength, 0f, 0.95f);
        }

        EditorGUILayout.Space();
        GUILayout.Label("=== Tree Types ===", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Gradient colors will be randomly sampled for each tree. Increase alpha cutoff to remove branches.", MessageType.Info);

        // Display all tree settings
        for (int i = 0; i < treeSettings.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            treeSettings[i].foldout = EditorGUILayout.Foldout(treeSettings[i].foldout, "Tree Type " + (i + 1), true);
            
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                treeSettings.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (treeSettings[i].foldout)
            {
                EditorGUI.indentLevel++;
                
                treeSettings[i].prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", treeSettings[i].prefab, typeof(GameObject), true);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Optional Material Assignments", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Assign materials here if your model needs specific materials before generation. Leave empty to use prefab's materials.", MessageType.Info);
                treeSettings[i].leafMaterial = (Material)EditorGUILayout.ObjectField("Leaf Material", treeSettings[i].leafMaterial, typeof(Material), false);
                treeSettings[i].branchMaterial = (Material)EditorGUILayout.ObjectField("Branch Material", treeSettings[i].branchMaterial, typeof(Material), false);
                treeSettings[i].trunkMaterial = (Material)EditorGUILayout.ObjectField("Trunk Material", treeSettings[i].trunkMaterial, typeof(Material), false);
                
                EditorGUILayout.Space();
                treeSettings[i].count = EditorGUILayout.IntField("Count", treeSettings[i].count);
                treeSettings[i].minScale = EditorGUILayout.FloatField("Min Scale", treeSettings[i].minScale);
                treeSettings[i].maxScale = EditorGUILayout.FloatField("Max Scale", treeSettings[i].maxScale);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);
                
                treeSettings[i].randomRotationX = EditorGUILayout.Toggle("Random X Rotation", treeSettings[i].randomRotationX);
                if (treeSettings[i].randomRotationX)
                {
                    EditorGUI.indentLevel++;
                    treeSettings[i].minRotationX = EditorGUILayout.FloatField("Min X Angle", treeSettings[i].minRotationX);
                    treeSettings[i].maxRotationX = EditorGUILayout.FloatField("Max X Angle", treeSettings[i].maxRotationX);
                    EditorGUI.indentLevel--;
                }
                
                treeSettings[i].randomRotationZ = EditorGUILayout.Toggle("Random Z Rotation", treeSettings[i].randomRotationZ);
                if (treeSettings[i].randomRotationZ)
                {
                    EditorGUI.indentLevel++;
                    treeSettings[i].minRotationZ = EditorGUILayout.FloatField("Min Z Angle", treeSettings[i].minRotationZ);
                    treeSettings[i].maxRotationZ = EditorGUILayout.FloatField("Max Z Angle", treeSettings[i].maxRotationZ);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.HelpBox("Y rotation is always random (0-360°). X and Z can tilt/lean the tree.", MessageType.None);
                
                EditorGUILayout.Space();
                treeSettings[i].maxSlope = EditorGUILayout.FloatField("Max Slope", treeSettings[i].maxSlope);
                treeSettings[i].slopeDensityFade = EditorGUILayout.Toggle("Fade Density on Slope", treeSettings[i].slopeDensityFade);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Ignore Full Model", EditorStyles.boldLabel);
                treeSettings[i].ignoreLeafCheck = EditorGUILayout.Toggle("Apply color to full model", treeSettings[i].ignoreLeafCheck);
                
                if (!treeSettings[i].ignoreLeafCheck)
                {
                    EditorGUILayout.HelpBox("Color will only be applied to leaves (trunks stay brown)", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Color will be applied to the ENTIRE model (including trunks)", MessageType.Warning);
                }
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Start Color", GUILayout.Width(80));
                GradientColorKey[] colorKeys = treeSettings[i].leafColorGradient.colorKeys;
                if (colorKeys.Length > 0)
                {
                    Color newStartColor = EditorGUILayout.ColorField(colorKeys[0].color);
                    if (newStartColor != colorKeys[0].color)
                    {
                        colorKeys[0].color = newStartColor;
                        treeSettings[i].leafColorGradient.SetKeys(colorKeys, treeSettings[i].leafColorGradient.alphaKeys);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("End Color", GUILayout.Width(80));
                if (colorKeys.Length > 1)
                {
                    Color newEndColor = EditorGUILayout.ColorField(colorKeys[1].color);
                    if (newEndColor != colorKeys[1].color)
                    {
                        colorKeys[1].color = newEndColor;
                        treeSettings[i].leafColorGradient.SetKeys(colorKeys, treeSettings[i].leafColorGradient.alphaKeys);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.HelpBox("Each tree will randomly pick a color between Start and End Color", MessageType.None);
                
                treeSettings[i].alphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", treeSettings[i].alphaCutoff, 0, 1);
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // Add new tree button
        if (GUILayout.Button("+ Add Tree Type", GUILayout.Height(25)))
        {
            treeSettings.Add(new TreeSettings());
        }

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

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
        forestParent = new GameObject("Generated Forest " + timestamp);

        Vector3 generationCenter;
        if (useSelectedObject && Selection.activeGameObject != null)
        {
            generationCenter = Selection.activeGameObject.transform.position;
        }
        else
        {
            generationCenter = targetTerrain.transform.position + new Vector3(targetTerrain.terrainData.size.x / 2f, 0, targetTerrain.terrainData.size.z / 2f);
        }

        float noiseOffsetX = Random.Range(0f, 10000f);
        float noiseOffsetZ = Random.Range(0f, 10000f);

        int totalPlaced = 0;
        int totalExpected = 0;

        for (int treeIndex = 0; treeIndex < treeSettings.Count; treeIndex++)
        {
            TreeSettings settings = treeSettings[treeIndex];
            
            if (settings.prefab == null || settings.count <= 0)
            {
                continue;
            }

            totalExpected += settings.count;

            int attempts = 0;
            int placed = 0;

            while (placed < settings.count && attempts < settings.count * 10)
            {
                attempts++;
                float x = Random.Range(-forestSize.x / 2f, forestSize.x / 2f);
                float z = Random.Range(-forestSize.y / 2f, forestSize.y / 2f);
                Vector3 pos = generationCenter + new Vector3(x, 0, z);

                float terrainHeight = targetTerrain.SampleHeight(pos) + targetTerrain.transform.position.y;
                pos.y = terrainHeight;

                float normalizedX = (pos.x - targetTerrain.transform.position.x) / targetTerrain.terrainData.size.x;
                float normalizedZ = (pos.z - targetTerrain.transform.position.z) / targetTerrain.terrainData.size.z;
                Vector3 normal = targetTerrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
                float slope = Vector3.Angle(Vector3.up, normal);

                if (slope > settings.maxSlope)
                {
                    continue;
                }

                float densityMultiplier = 1f;
                if (settings.slopeDensityFade && slope > 0)
                {
                    densityMultiplier = 1f - (slope / settings.maxSlope);
                }

                if (useNaturalClustering)
                {
                    float noiseValue = GetClusteringNoise(pos.x, pos.z, treeIndex, noiseOffsetX, noiseOffsetZ);
                    float boostedNoise = Mathf.Pow(noiseValue, 0.7f);
                    float clusterMultiplier = Mathf.Lerp(1f, boostedNoise * 1.5f, clusterStrength);
                    densityMultiplier *= clusterMultiplier;
                }

                if (Random.value > densityMultiplier)
                {
                    continue;
                }

                GameObject instance = InstantiateTree(settings, pos, normal);
                if (instance != null)
                {
                    instance.transform.parent = forestParent.transform;
                    placed++;
                    totalPlaced++;
                }
            }
        }

        Debug.Log("Forest generation complete. Placed " + totalPlaced + "/" + totalExpected + " trees.");
    }

    GameObject InstantiateTree(TreeSettings settings, Vector3 pos, Vector3 normal)
    {
        GameObject instance;

        if (PrefabUtility.GetPrefabType(settings.prefab) != PrefabType.None)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(settings.prefab);
        }
        else
        {
            instance = (GameObject)Instantiate(settings.prefab);
        }

        instance.transform.position = pos;
        
        // Calculate rotation
        // Start with terrain normal alignment, then apply random Y rotation
        Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, Random.Range(0, 360), 0);
        
        // Add optional X and Z rotations
        float rotX = settings.randomRotationX ? Random.Range(settings.minRotationX, settings.maxRotationX) : 0f;
        float rotZ = settings.randomRotationZ ? Random.Range(settings.minRotationZ, settings.maxRotationZ) : 0f;
        
        // Apply additional rotations
        Quaternion additionalRotation = Quaternion.Euler(rotX, 0, rotZ);
        instance.transform.rotation = baseRotation * additionalRotation;
        
        float scale = Random.Range(settings.minScale, settings.maxScale);
        instance.transform.localScale = Vector3.one * scale;

        // Sample a random color from the gradient
        Color leafColor = settings.leafColorGradient.Evaluate(Random.Range(0f, 1f));

        // First pass: Apply optional material assignments if provided
        if (settings.leafMaterial != null || settings.branchMaterial != null || settings.trunkMaterial != null)
        {
            ApplyOptionalMaterials(instance, settings);
        }

        // Second pass: Apply color modifications (only albedo)
        ApplyColorToMaterials(instance, settings, leafColor);

        return instance;
    }

    // Apply optional material assignments based on material names
    void ApplyOptionalMaterials(GameObject instance, TreeSettings settings)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material[] sharedMats = rend.sharedMaterials;
            bool materialsChanged = false;

            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] != null)
                {
                    string matName = sharedMats[i].name.ToLower();
                    Material replacementMat = null;

                    // Check if this material should be replaced with a custom one
                    if (settings.leafMaterial != null && 
                        (matName.Contains("leaf") || matName.Contains("leaves") || 
                         matName.Contains("foliage") || matName.Contains("canopy")))
                    {
                        replacementMat = settings.leafMaterial;
                    }
                    else if (settings.branchMaterial != null && 
                             (matName.Contains("branch") || matName.Contains("twig")))
                    {
                        replacementMat = settings.branchMaterial;
                    }
                    else if (settings.trunkMaterial != null && 
                             (matName.Contains("trunk") || matName.Contains("bark") || 
                              matName.Contains("stem") || matName.Contains("wood")))
                    {
                        replacementMat = settings.trunkMaterial;
                    }

                    if (replacementMat != null)
                    {
                        sharedMats[i] = new Material(replacementMat);
                        materialsChanged = true;
                    }
                }
            }

            if (materialsChanged)
            {
                rend.sharedMaterials = sharedMats;
            }
        }
    }

    // Apply color modifications to materials (only modifying albedo/base color)
    void ApplyColorToMaterials(GameObject instance, TreeSettings settings, Color leafColor)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material[] sharedMats = rend.sharedMaterials;

            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] != null)
                {
                    // Check if this specific material is a leaf material
                    bool isLeafMaterial = IsLeafMaterial(sharedMats[i]);
                    
                    // Only modify the material if:
                    // 1. It's a leaf material, OR
                    // 2. We're ignoring the leaf check (apply to all)
                    if (isLeafMaterial || settings.ignoreLeafCheck)
                    {
                        // Create a new material instance to avoid modifying the shared material
                        Material newMat = new Material(sharedMats[i]);

                        // Only modify albedo/base color properties - nothing else
                        if (newMat.HasProperty("_Color"))
                        {
                            newMat.SetColor("_Color", leafColor);
                        }
                        if (newMat.HasProperty("_BaseColor"))
                        {
                            newMat.SetColor("_BaseColor", leafColor);
                        }
                        if (newMat.HasProperty("_MainColor"))
                        {
                            newMat.SetColor("_MainColor", leafColor);
                        }
                        
                        // Apply alpha cutoff if the material supports it
                        if (newMat.HasProperty("_Cutoff"))
                        {
                            newMat.SetFloat("_Cutoff", settings.alphaCutoff);
                        }

                        sharedMats[i] = newMat;
                    }
                    // If it's NOT a leaf material and we're NOT ignoring the check,
                    // we simply don't touch it - it keeps its original material
                }
            }
            
            rend.sharedMaterials = sharedMats;
        }
    }

    // Helper function to determine if a material is a leaf material
    bool IsLeafMaterial(Material material)
    {
        if (material == null)
            return false;
            
        string matName = material.name.ToLower();
        
        // Check for leaf keywords
        if (matName.Contains("leaf") || matName.Contains("leaves") || 
            matName.Contains("foliage") || matName.Contains("canopy"))
        {
            return true;
        }
        
        // Check for trunk/bark keywords - explicitly exclude these
        if (matName.Contains("trunk") || matName.Contains("bark") || 
            matName.Contains("wood") || matName.Contains("stem"))
        {
            return false;
        }

        // Default: assume it's NOT a leaf material if we can't determine
        // This is safer - it won't accidentally color bark materials
        return false;
    }

    float GetClusteringNoise(float worldX, float worldZ, int treeIndex, float offsetX, float offsetZ)
    {
        float treeOffset = treeIndex * 7777f;

        float noiseX = (worldX + offsetX + treeOffset) / clusterScale;
        float noiseZ = (worldZ + offsetZ + treeOffset) / clusterScale;
        float noiseValue = Mathf.PerlinNoise(noiseX, noiseZ);

        float noiseX2 = (worldX + offsetX + treeOffset * 1.7f) / (clusterScale * 0.5f);
        float noiseZ2 = (worldZ + offsetZ + treeOffset * 1.7f) / (clusterScale * 0.5f);
        float noiseValue2 = Mathf.PerlinNoise(noiseX2, noiseZ2);

        float noiseX3 = (worldX + offsetX + treeOffset * 2.3f) / (clusterScale * 0.25f);
        float noiseZ3 = (worldZ + offsetZ + treeOffset * 2.3f) / (clusterScale * 0.25f);
        float noiseValue3 = Mathf.PerlinNoise(noiseX3, noiseZ3);

        noiseValue = noiseValue * 0.5f + noiseValue2 * 0.3f + noiseValue3 * 0.2f;

        return noiseValue;
    }
}
