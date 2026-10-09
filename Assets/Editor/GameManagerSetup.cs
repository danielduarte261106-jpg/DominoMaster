using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Configura la escena completa de DominoMaster con calidad visual de Casino:
/// - Tapete verde centrado con proporciones correctas.
/// - Cámara ortográfica ajustada (size = 5).
/// - GameManager con todos los sistemas conectados (DominoSet, HandManager, MatchController, etc.).
/// - Carga automática de los 7 sprites de frutas en orden (0 a 6).
/// - Canvas limpio y legible.
/// </summary>
public static class GameManagerSetup
{
    private static readonly string[] FruitNames = new string[]
    {
        "seed",       // 0
        "grape",      // 1
        "strawberry", // 2
        "lime",       // 3
        "apple",      // 4
        "pineapple",  // 5
        "watermelon"  // 6
    };

    [MenuItem("Domino/Configurar Escena Casino Completa")]
    public static void SetupCompleteCasinoScene()
    {
        SetupPrefab();
        SetupCamera();
        SetupTable();
        SetupGameManager();
        SetupHUD();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[GameManagerSetup] Escena de Casino configurada exitosamente y lista para Play.");
    }

    private static void SetupPrefab()
    {
        string prefabFolder = "Assets/Prefabs";
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }

        string prefabPath = prefabFolder + "/FichaBase.prefab";
        GameObject fichaBase = new GameObject("FichaBase");
        fichaBase.AddComponent<SpriteRenderer>();
        fichaBase.AddComponent<Rigidbody2D>();
        fichaBase.AddComponent<BoxCollider2D>();
        fichaBase.AddComponent<DominoPiece>();
        fichaBase.transform.localScale = new Vector3(0.5f, 1f, 1f);

        PrefabUtility.SaveAsPrefabAsset(fichaBase, prefabPath);
        Object.DestroyImmediate(fichaBase);
    }

    private static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj == null)
                camObj = new GameObject("Main Camera");
            cam = camObj.GetComponent<Camera>() ?? camObj.AddComponent<Camera>();
        }

        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.08f, 0.12f, 0.08f, 1f); // Verde oscuro casino
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void SetupTable()
    {
        GameObject tableContainer = GameObject.Find("TableContainer");
        if (tableContainer == null)
            tableContainer = new GameObject("TableContainer");

        tableContainer.transform.position = Vector3.zero;

        Transform feltTransform = tableContainer.transform.Find("TableFelt");
        GameObject feltObj = feltTransform != null ? feltTransform.gameObject : new GameObject("TableFelt");
        feltObj.transform.SetParent(tableContainer.transform);
        feltObj.transform.localPosition = Vector3.zero;
        feltObj.transform.localScale = new Vector3(16f, 9.5f, 1f);

        SpriteRenderer sr = feltObj.GetComponent<SpriteRenderer>() ?? feltObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -10;

        // Crear textura de fieltro verde casino con borde
        Texture2D feltTex = new Texture2D(16, 16);
        Color feltColor = new Color(0.12f, 0.38f, 0.18f); // Verde tapete clásico
        Color[] cols = new Color[16 * 16];
        for (int i = 0; i < cols.Length; i++) cols[i] = feltColor;
        feltTex.SetPixels(cols);
        feltTex.Apply();

        sr.sprite = Sprite.Create(feltTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void SetupGameManager()
    {
        GameObject existingGM = GameObject.Find("GameManager");
        if (existingGM != null)
            Object.DestroyImmediate(existingGM);

        GameObject gm = new GameObject("GameManager");

        // Añadir componentes en orden
        CasinoTableManager tableMgr = gm.AddComponent<CasinoTableManager>();
        MatchController matchCtrl   = gm.AddComponent<MatchController>();
        TableInputManager inputMgr  = gm.AddComponent<TableInputManager>();
        HandManager handMgr        = gm.AddComponent<HandManager>();
        EliteAIController aiCtrl   = gm.AddComponent<EliteAIController>();
        DominoSet dominoSet        = gm.AddComponent<DominoSet>();
        DynamicUIManager uiMgr     = gm.AddComponent<DynamicUIManager>();
        CasinoChipsUI chipsUI      = gm.AddComponent<CasinoChipsUI>();
        AIVisualFeedback aiVis     = gm.AddComponent<AIVisualFeedback>();
        GameSessionManager session = gm.AddComponent<GameSessionManager>();

        // Asignar Prefab de Ficha
        string prefabPath = "Assets/Prefabs/FichaBase.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        dominoSet.dominoPrefab = prefab;

        // Cargar los 7 sprites de frutas en orden estricto (0 a 6)
        Sprite[] sprites = new Sprite[7];
        for (int i = 0; i < FruitNames.Length; i++)
        {
            string path = $"Assets/Resources/Frutas/{FruitNames[i]}.png";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites[i] == null)
            {
                Debug.LogWarning($"[GameManagerSetup] No se encontro sprite en {path}");
            }
        }
        dominoSet.fruitSprites = sprites;
    }

    private static void SetupHUD()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Limpiar textos obsoletos del Canvas viejo si existen
        Transform uiPanel = canvasObj.transform.Find("UIPanel");
        if (uiPanel != null)
        {
            // Remover el panel oscuro invasivo
            Object.DestroyImmediate(uiPanel.gameObject);
        }

        // Eliminar textos viejos de Jugadores que flotaban
        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = 0; i < canvasObj.transform.childCount; i++)
        {
            Transform child = canvasObj.transform.GetChild(i);
            if (child.name.StartsWith("Jugador") || child.name == "UIPanel")
            {
                toDestroy.Add(child.gameObject);
            }
        }
        foreach (var obj in toDestroy)
        {
            Object.DestroyImmediate(obj);
        }
    }
}
