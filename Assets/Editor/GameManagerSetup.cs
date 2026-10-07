using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Editor utility to set up the base domino prefab and the GameManager in the scene.
/// </summary>
public static class GameManagerSetup
{
    [MenuItem("Domino/2. Configurar GameManager")]
    public static void SetupGameManager()
    {
        // 1. Create Prefabs folder if it doesn't exist
        string prefabFolder = "Assets/Prefabs";
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
        }

        // 2. Create temporary FichaBase GameObject
        GameObject fichaBase = new GameObject("FichaBase");
        fichaBase.AddComponent<SpriteRenderer>();
        fichaBase.AddComponent<Rigidbody2D>();
        fichaBase.AddComponent<BoxCollider2D>();
        fichaBase.AddComponent<DominoPiece>();
        fichaBase.transform.localScale = new Vector3(0.5f, 1f, 1f);

        // 3. Save as prefab
        string prefabPath = Path.Combine(prefabFolder, "FichaBase.prefab");
        PrefabUtility.SaveAsPrefabAsset(fichaBase, prefabPath);

        // 4. Destroy the temporary object
        DestroyImmediate(fichaBase);

        // 5. Create (or recreate) GameManager in the scene
        GameObject existingGM = GameObject.Find("GameManager");
        if (existingGM != null)
        {
            DestroyImmediate(existingGM);
        }
        GameObject gameManager = new GameObject("GameManager");

        // 6. Add required components
        gameManager.AddComponent<CasinoTableManager>();
        gameManager.AddComponent<DynamicUIManager>();
        gameManager.AddComponent<DominoSet>();

        // 7. Assign the prefab to DominoSet's dominoPrefab field
        DominoSet dominoSet = gameManager.GetComponent<DominoSet>();
        if (dominoSet != null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                dominoSet.dominoPrefab = prefab;
            }
            else
            {
                Debug.LogError("[GameManagerSetup] Failed to load prefab at " + prefabPath);
            }
        }
        else
        {
            Debug.LogError("[GameManagerSetup] DominoSet component not found on GameManager.");
        }

        // Mark scene as dirty to save changes
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[GameManagerSetup] GameManager configurado correctamente.");
    }
}
