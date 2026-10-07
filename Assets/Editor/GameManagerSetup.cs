using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class GameManagerSetup
{
    [MenuItem("Domino/2. Configurar GameManager")]
    public static void SetupGameManager()
    {
        string prefabFolder = "Assets/Prefabs";
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }

        GameObject fichaBase = new GameObject("FichaBase");
        fichaBase.AddComponent<SpriteRenderer>();
        fichaBase.AddComponent<Rigidbody2D>();
        fichaBase.AddComponent<BoxCollider2D>();
        fichaBase.AddComponent<DominoPiece>();
        fichaBase.transform.localScale = new Vector3(0.5f, 1f, 1f);

        string prefabPath = prefabFolder + "/FichaBase.prefab";
        PrefabUtility.SaveAsPrefabAsset(fichaBase, prefabPath);

        Object.DestroyImmediate(fichaBase);

        GameObject existingGM = GameObject.Find("GameManager");
        if (existingGM != null)
        {
            Object.DestroyImmediate(existingGM);
        }
        GameObject gameManager = new GameObject("GameManager");

        gameManager.AddComponent<CasinoTableManager>();
        gameManager.AddComponent<DynamicUIManager>();
        gameManager.AddComponent<DominoSet>();

        DominoSet dominoSet = gameManager.GetComponent<DominoSet>();
        if (dominoSet != null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                dominoSet.dominoPrefab = prefab;
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[GameManagerSetup] GameManager y Prefab configurados correctamente.");
    }
}
