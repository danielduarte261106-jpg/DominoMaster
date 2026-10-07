using UnityEngine;

public class TestPhysics : MonoBehaviour
{
    private void Start()
    {
        // 1. Instanciar el GameObject "FichaDePrueba" en (0, 4, 0)
        GameObject ficha = new GameObject("FichaDePrueba");
        ficha.transform.position = new Vector3(0f, 4f, 0f);

        // 2. Añadir SpriteRenderer con textura blanca generada por código
        SpriteRenderer sr = ficha.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(128, 64, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        sr.sprite = sprite;

        // 3. Añadir Rigidbody2D para que caiga con gravedad
        Rigidbody2D rb = ficha.AddComponent<Rigidbody2D>();

        // 4. Añadir BoxCollider2D con tamaño (1, 2) para colisiones
        BoxCollider2D bc = ficha.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(1f, 2f);
    }
}
