using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Mini-game: collect all coins and the key, and avoid standing in the red splats.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerController player;
    public FollowCamera cameraRig;
    public RectTransform hpFill;
    public Text hpText, scoreText, messageText;
    public RawImage keyIcon;
    public float maxHp = 100f;
    public float hpBarFullWidth = 496f;
    public float pickupRadius = 0.9f;

    public bool Finished { get; private set; }

    float hp, startTime;
    int coins, totalCoins;
    bool hasKey;
    Vector3 startPos;
    Pickup[] pickups;
    Hazard[] hazards;

    void Awake() { Instance = this; }

    void Start()
    {
        hp = maxHp;
        startPos = player.transform.position;
        pickups = FindObjectsByType<Pickup>(FindObjectsSortMode.None);
        hazards = FindObjectsByType<Hazard>(FindObjectsSortMode.None);
        foreach (var p in pickups) if (p.kind == Pickup.Kind.Coin) totalCoins++;
        startTime = Time.time;
        messageText.text = "";
        Refresh();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (kb.tabKey.wasPressedThisFrame) cameraRig.Toggle();
        }
        if (Finished) return;

        Vector3 pos = player.transform.position;
        player.speedMultiplier = 1f;
        foreach (var h in hazards)
        {
            if (!h.Contains(pos)) continue;
            if (h.kind == Hazard.Kind.Damage) hp -= h.damagePerSecond * Time.deltaTime;
            else player.speedMultiplier = Mathf.Min(player.speedMultiplier, h.slowFactor);
        }

        foreach (var p in pickups)
        {
            if (p == null || !p.gameObject.activeSelf) continue;
            if ((p.transform.position - (pos + Vector3.up)).sqrMagnitude > pickupRadius * pickupRadius) continue;
            if (p.kind == Pickup.Kind.Coin) coins++; else hasKey = true;
            p.gameObject.SetActive(false);
        }

        if (hp <= 0f) { hp = 0f; Finish("Поразка. Червоні плями небезпечні!\nR — спробувати ще раз"); }
        else if (hasKey && coins == totalCoins)
            Finish("Перемога! Час: " + (Time.time - startTime).ToString("F1") + " с\nR — зіграти ще раз");
        Refresh();
    }

    void Finish(string message) { Finished = true; messageText.text = message; }

    public void Respawn()
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false; player.transform.position = startPos; cc.enabled = true;
    }

    void Refresh()
    {
        hpFill.sizeDelta = new Vector2(hpBarFullWidth * Mathf.Clamp01(hp / maxHp), hpFill.sizeDelta.y);
        hpText.text = "HP " + Mathf.CeilToInt(hp);
        float t = Finished ? 0 : Time.time - startTime;
        scoreText.text = "Монети " + coins + "/" + totalCoins + "   Ключ " + (hasKey ? "є" : "—") + (Finished ? "" : "   " + t.ToString("F0") + " с");
        keyIcon.color = hasKey ? Color.white : new Color(1, 1, 1, 0.3f);
    }
}
