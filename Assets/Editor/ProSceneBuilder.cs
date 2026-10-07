using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Script maestro de Editor para la construcción automática de la escena de Casino Pro.
/// Limpia la jerarquía, crea la cámara con post-procesado, la mesa, el Canvas escalable y prepara
/// la auto-carga de sprites desde la carpeta local de frutas.
/// </summary>
public static class ProSceneBuilder
{
    private const string MENU_PATH = "Domino/Construir Casino Pro";
    private const string FRUITS_FOLDER_PATH = "C:/Users/danie/OneDrive/Escritorio/Frutas/";

    [MenuItem(MENU_PATH)]
    public static void BuildCasinoProScene()
    {
        // 1. Limpieza de jerarquía y escena
        ClearHierarchy();

        // 2. Crear la escena base
        CreateSceneAssets();

        // 3. Crear la cámara con post-procesado básico
        CreateMainCamera();

        // 4. Crear la mesa de fieltro verde
        CreateTableFelt();

        // 5. Configurar colisionadores invisibles en bordes
        CreateBoundaryColliders();

        // 6. Configurar Canvas escalable 1920x1080
        CreateCanvas();

        // 7. Generar asientos cardinales para jugadores
        GeneratePlayerSeats();

        // 8. Preparar auto-carga de sprites desde carpeta local Frutas
        PrepareFruitSpriteLoad();

        // 9. Guardar cambios
        SceneView.lastActiveSceneView.Repaint();
        EditorUtility.SetDirty(SceneUtility.GetActiveScene());
        Debug.Log("[ProSceneBuilder] Escena de Casino Pro construida exitosamente.");
    }

    private static void ClearHierarchy()
    {
        // Eliminar todos los GameObjects de la escena actual (excepto el AssetDatabase)
        GameObject[] objects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in objects)
        {
            if (obj.name != "EditorOnlyCamera" && obj.hideFlags == HideFlags.None)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }

    private static void CreateSceneAssets()
    {
        // Crear objeto contenedor principal
        GameObject sceneRoot = new GameObject("CasinoSceneRoot");
        Undo.RegisterCreatedObjectUndo(sceneRoot, "Crear CasinoSceneRoot");
        GameObjectUtility.SetParentAndAlign(sceneRoot, null);
    }

    private static void CreateMainCamera()
    {
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();

        // Configuración de cámara
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.Color;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.cullingMask = ~0; // Todas las capas

        Undo.RegisterCreatedObjectUndo(cameraObj, "Crear Main Camera");

        // Post-procesado básico (usando el sistema de Post-Processing Stack)
        GameObject postProcess = new GameObject("PostProcessVolume");
        PostProcessVolume ppVolume = postProcess.AddComponent<PostProcessVolume>();
        ppVolume.isGlobal = true;
        ppVolume.priority = 100;
        Undo.RegisterCreatedObjectUndo(postProcess, "Crear PostProcessVolume");

        // Configurar efectos post-procesados
        PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();

        // Bloom (brillo suave para ambiente de casino)
        PostProcessLayer postProcessLayer = cameraObj.AddComponent<PostProcessLayer>();
        postProcessLayer.profile = profile;

        // Añadir efecto Bloom
        var bloom = profile.GetSettings<Bloom>();
        bloom.enabled.value = true;
        bloom.intensity.value = 0.5f;
        bloom.threshold.value = 1.2f;
        bloom.radius.value = 0.5f;
        bloom.quality.value = PostProcessQuality.High;

        // Color Adjustment (mejorar contraste y saturación)
        var colorAdj = profile.GetSettings<ColorAdjustments>();
        colorAdj.enabled.value = true;
        colorAdj.contrast.value = 10f;
        colorAdj.saturation.value = 5f;
        colorAdj.brightness.value = 0f;

        // Guardar el perfil
        string profilePath = "Assets/PostProcess/CasinoProfile.asset";
        Directory.CreateDirectory("Assets/PostProcess");
        AssetDatabase.CreateAsset(profile, profilePath);
        postProcessLayer.profile = profile;

        // Añadir cámara de efecto (no necesario si usamos PostProcessLayer directamente)
    }

    private static void CreateTableFelt()
    {
        // Crear sprite de mesa de fieltro verde
        GameObject tableObj = new GameObject("TableFelt");
        SpriteRenderer sr = tableObj.AddComponent<SpriteRenderer>();

        // Generar un sprite verde para la mesa
        Texture2D greenTexture = new Texture2D(1024, 768, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[1024 * 768];
        for (int i = 0; i < pixels.Length; i++)
        {
            // Variación sutil de verde para simular textura de fieltro
            float variation = Random.Range(-15f, 15f);
            pixels[i] = new Color(0.15f + variation / 255f, 0.4f + variation / 255f, 0.15f + variation / 255f);
        }
        greenTexture.SetPixels(pixels);
        greenTexture.Apply();

        Sprite tableSprite = Sprite.Create(greenTexture, new Rect(0, 0, 1024, 768), new Vector2(0.5f, 0.5f));
        sr.sprite = tableSprite;

        // Configurar tamaño de la mesa
        tableObj.transform.position = Vector3.zero;
        tableObj.transform.localScale = new Vector3(2f, 1.5f, 1f);

        // Añadir colisionador
        BoxCollider2D collider = tableObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2f, 1.5f);
        collider.isTrigger = false;

        // Añadir script de DominoPiece para física en la mesa
        // tableObj.AddComponent<DominoPiece>();

        // Colocar debajo de la cámara
        GameObject tableContainer = new GameObject("TableContainer");
        Undo.RegisterCreatedObjectUndo(tableContainer, "Crear TableContainer");
        GameObjectUtility.SetParentAndAlign(tableObj, tableContainer);
        Undo.RegisterCreatedObjectUndo(tableObj, "Crear TableFelt");
    }

    private static void CreateBoundaryColliders()
    {
        // Crear colliders invisibles en los bordes de la mesa para que las fichas no caigan
        float tableWidth = 2f;
        float tableHeight = 1.5f;

        // Colocación de bordes
        Vector3[] positions = new Vector3[4];
        positions[0] = new Vector3(0, (tableHeight / 2) + 0.05f, 0); // Arriba
        positions[1] = new Vector3(0, -(tableHeight / 2) - 0.05f, 0); // Abajo
        positions[2] = new Vector3(-(tableWidth / 2) - 0.05f, 0, 0); // Izquierda
        positions[3] = new Vector3((tableWidth / 2) + 0.05f, 0, 0); // Derecha

        Vector2[] sizes = new Vector2[4];
        sizes[0] = new Vector2(tableWidth + 0.2f, 0.1f);
        sizes[1] = new Vector2(tableWidth + 0.2f, 0.1f);
        sizes[2] = new Vector2(0.1f, tableHeight + 0.2f);
        sizes[3] = new Vector2(0.1f, tableHeight + 0.2f);

        string[] names = new[] { "BoundaryTop", "BoundaryBottom", "BoundaryLeft", "BoundaryRight" };

        for (int i = 0; i < 4; i++)
        {
            GameObject boundary = new GameObject(names[i]);
            BoxCollider2D collider = boundary.AddComponent<BoxCollider2D>();
            collider.size = sizes[i];
            collider.isTrigger = true; // Solo detener, no rebotar
            collider.sharedMaterial = new PhysicsMaterial2D
            {
                friction = 0f,
                bounce = 1f
            };
            boundary.transform.position = positions[i];
            boundary.layer = LayerMask.NameToLayer("Default");
            Undo.RegisterCreatedObjectUndo(boundary, $"Crear {names[i]}");
            GameObjectUtility.SetParentAndAlign(boundary, tableContainer);
        }
    }

    private static void CreateCanvas()
    {
        // Crear Canvas con resolución 1920x1080
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.planeDistance = 100f;
        canvas.pixelPerfect = true;

        CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();

        // Añadir Panel de fondo
        GameObject panelObj = new GameObject("UIPanel");
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.3f); // Fondo semi-transparente

        GameObjectUtility.SetParentAndAlign(canvasObj, null);
        Undo.RegisterCreatedObjectUndo(canvasObj, "Crear Canvas");

        // Crear Textos de jugadores con TextAnchor
        string[] playerNames = new[] { "Jugador 1", "Jugador 2", "Jugador 3", "Jugador 4" };
        Vector2[] anchorPositions = new Vector2[4];
        anchorPositions[0] = new Vector2(0.5f, 0f); // Centro
        anchorPositions[1] = new Vector2(0f, 0.5f); // Arriba
        anchorPositions[2] = new Vector2(1f, 0f);   // Derecha
        anchorPositions[3] = new Vector2(0.5f, 1f); // Abajo

        for (int i = 0; i < 4; i++)
        {
            GameObject textObj = new GameObject($"PlayerName_{playerNames[i]}");
            Text text = textObj.AddComponent<Text>();
            text.text = playerNames[i];
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter; // Uso estricto de TextAnchor
            text.color = Color.white;
            text.font = Resources.Load<Font>("Fonts/ARIAL");
            text.rectTransform.SetParent(canvasObj.transform);
            text.rectTransform.localPosition = anchorPositions[i] * 500f; // Posición relativa
            text.rectTransform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(textObj, $"Crear {playerNames[i]}");
        }
    }

    private static void GeneratePlayerSeats()
    {
        // Generar asientos para 4 jugadores en puntos cardinales
        Vector3[] seatPositions = new Vector3[4];
        seatPositions[0] = new Vector3(0, 3f, 0);     // Arriba (IA o Jugador 2)
        seatPositions[1] = new Vector3(-3f, 0, 0);    // Izquierda (Jugador 1)
        seatPositions[2] = new Vector3(3f, 0, 0);     // Derecha (IA o Jugador 3)
        seatPositions[3] = new Vector3(0, -3f, 0);    // Abajo (Jugador 4)

        Color[] seatColors = new Color[4];
        seatColors[0] = Color.purple;
        seatColors[1] = Color.green;
        seatColors[2] = Color.cyan;
        seatColors[3] = Color.yellow;

        string[] seatLabels = new[] { "Sede Arriba", "Sede Izquierda", "Sede Derecha", "Sede Abajo" };

        for (int i = 0; i < 4; i++)
        {
            GameObject seatObj = new GameObject($"Seat_{seatLabels[i]}");
            GameObject seatCircle = new GameObject($"SeatCircle_{i}");
            SpriteRenderer sr = seatCircle.AddComponent<SpriteRenderer>();

            // Crear círculo para el asiento
            Texture2D circleTexture = new Texture2D(64, 64, TextureFormat.ARGB32, false);
            CircleTexture(circleTexture, seatColors[i]);
            sr.sprite = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            seatCircle.transform.localScale = Vector3.one * 0.5f;
            seatCircle.transform.SetParent(seatObj.transform);
            Undo.RegisterCreatedObjectUndo(seatCircle, $"Crear {seatLabels[i]}");

            seatObj.transform.position = seatPositions[i];
            Undo.RegisterCreatedObjectUndo(seatObj, $"Crear {seatLabels[i]}");
            GameObjectUtility.SetParentAndAlign(seatObj, tableContainer);

            // Añadir etiqueta de posición
            GameObject labelObj = new GameObject($"SeatLabel_{i}");
            Text label = labelObj.AddComponent<Text>();
            label.text = seatLabels[i];
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 18;
            label.color = seatColors[i];
            label.transform.SetParent(seatObj.transform);
            label.transform.localPosition = new Vector3(0, -0.6f, 0);
            Undo.RegisterCreatedObjectUndo(labelObj, $"Crear Label {i}");
        }
    }

    private static void PrepareFruitSpriteLoad()
    {
        // Preparar auto-carga de sprites desde carpeta local Frutas
        if (Directory.Exists(FRUITS_FOLDER_PATH))
        {
            // Copiar la carpeta de frutas a Assets/Resources/Frutas/
            string targetFolder = "Assets/Resources/Frutas/";
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string[] fruitFiles = Directory.GetFiles(FRUITS_FOLDER_PATH, "*.png");
            foreach (string fruitFile in fruitFiles)
            {
                string fileName = Path.GetFileName(fruitFile);
                string destPath = Path.Combine(targetFolder, fileName);
                if (!File.Exists(destPath))
                {
                    File.Copy(fruitFile, destPath, true);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[ProSceneBuilder] Auto-carga preparada. {fruitFiles.Length} sprites de frutas copiados a {targetFolder}");
        }
        else
        {
            Debug.LogWarning($"[ProSceneBuilder] La carpeta de frutas no existe en {FRUITS_FOLDER_PATH}. Se usará un sprite verde por defecto.");
        }
    }

    private static void CircleTexture(Texture2D tex, Color color)
    {
        int w = tex.width;
        int h = tex.height;
        float radius = Mathf.Min(w, h) * 0.45f;
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = Mathf.Sqrt(Mathf.Pow(x - w * 0.5f, 2) + Mathf.Pow(y - h * 0.5f, 2));
                pixels[y * w + x] = (dist <= radius) ? color : new Color(0, 0, 0, 0);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
    }

    // Variable de referencia al contenedor de la mesa (necesario para el scope)
    private static GameObject tableContainer;
}
