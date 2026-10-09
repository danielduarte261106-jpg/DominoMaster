using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class DominoPiece : MonoBehaviour
{
    [SerializeField] private int sideA = 0;
    [SerializeField] private int sideB = 0;

    private Rigidbody2D rb;
    private BoxCollider2D bc;
    private SpriteRenderer spriteRenderer;
    private MatchController matchController;
    private bool inputEnabled = true;
    private int lastClickFrame = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (bc != null)
        {
            bc.size = Vector2.one;
            bc.enabled = true;
        }
    }

    private void Start()
    {
        matchController = GetComponentInParent<MatchController>();
        if (matchController == null)
            matchController = FindAnyObjectByType<MatchController>();
    }

    private void OnMouseDown()
    {
        TryPlayFromInput();
    }

    public bool TryPlayFromInput()
    {
        if (!inputEnabled || lastClickFrame == Time.frameCount)
            return false;

        lastClickFrame = Time.frameCount;

        if (matchController == null)
            matchController = FindAnyObjectByType<MatchController>();

        if (matchController == null)
            return false;

        return matchController.TryPlayPiece(this);
    }

    public void SetPlayerInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (bc == null)
            bc = GetComponent<BoxCollider2D>();
        if (bc != null)
            bc.enabled = enabled;
    }

    public void SetupVisuals(int leftVal, int rightVal, Sprite leftSprite, Sprite rightSprite)
    {
        sideA = leftVal;
        sideB = rightVal;

        // 2. Fondo de ficha estilo dominó real (marfil con borde sutil y línea divisoria)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sortingOrder = 0;

            const int w = 32;
            const int h = 64;
            Texture2D tex = new Texture2D(w, h);
            Color ivory = new Color(0.96f, 0.95f, 0.91f);
            Color border = new Color(0.35f, 0.35f, 0.35f);
            Color divider = new Color(0.45f, 0.45f, 0.45f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                    bool isDivider = (y == h / 2 - 1 || y == h / 2) && (x > 2 && x < w - 3);

                    if (isBorder)
                        tex.SetPixel(x, y, border);
                    else if (isDivider)
                        tex.SetPixel(x, y, divider);
                    else
                        tex.SetPixel(x, y, ivory);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();

            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            spriteRenderer.color = Color.white;
        }

        if (bc == null)
            bc = GetComponent<BoxCollider2D>();
        if (bc != null)
        {
            bc.size = Vector2.one;
            bc.enabled = inputEnabled;
        }

        Vector3 fruitLocalScale = GetCompensatedFruitLocalScale();

        // 3. Crear mitad superior (Fruta más pequeña)
        CreateFruitHalf("TopHalf", new Vector3(0f, 0.25f, -0.1f), fruitLocalScale, leftSprite);

        // 4. Crear mitad inferior (Fruta más pequeña)
        CreateFruitHalf("BottomHalf", new Vector3(0f, -0.25f, -0.1f), fruitLocalScale, rightSprite);
    }

    /// <summary>
    /// La ficha padre es rectangular (típicamente X=0.5, Y=1). Los hijos heredan esa escala,
    /// así que un localScale uniforme se ve aplastado en X. Invertimos la escala del padre
    /// para que el tamaño mundial de la fruta sea cuadrado.
    /// Ejemplo: padre (0.5, 1) → hijo (0.36, 0.18) → mundo (0.18, 0.18).
    /// </summary>
    private Vector3 GetCompensatedFruitLocalScale()
    {
        const float desiredWorldSize = 0.18f;
        Vector3 parentScale = transform.localScale;
        float absX = Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f);
        float absY = Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f);
        return new Vector3(desiredWorldSize / absX, desiredWorldSize / absY, 1f);
    }

    public void SetFaceDown(bool faceDown)
    {
        Transform top = transform.Find("TopHalf");
        Transform bottom = transform.Find("BottomHalf");
        if (top != null) top.gameObject.SetActive(!faceDown);
        if (bottom != null) bottom.gameObject.SetActive(!faceDown);
    }

    private void CreateFruitHalf(string name, Vector3 localPosition, Vector3 localScale, Sprite fruitSprite)
    {
        GameObject half = new GameObject(name);
        half.transform.SetParent(this.transform);
        half.transform.localPosition = localPosition;
        half.transform.localRotation = Quaternion.identity;
        half.transform.localScale = localScale;

        SpriteRenderer sr = half.AddComponent<SpriteRenderer>();
        sr.sprite = fruitSprite;
        sr.sortingOrder = 1;
    }

    // ======================
    // Lógica de Comodín Combinatorio
    // ======================
    public int SideA => sideA;
    public int SideB => sideB;
    public bool IsDouble => sideA == sideB;
    public bool HasRiskValue => sideA == 0 || sideA == 6 || sideB == 0 || sideB == 6;
    public int BasePlayValue => sideA + sideB;

    public bool MatchesValue(int value) => sideA == value || sideB == value;

    public int GetCombinatorialPlayValue()
    {
        int value = BasePlayValue;
        if (IsDouble) value *= 2;
        if (HasRiskValue) value += 3;
        return value;
    }
}