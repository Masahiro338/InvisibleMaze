using UnityEngine;

/// <summary>
/// プレイヤーの HP。画面左上に HP バーを表示する。
/// 回復は Heal()、ダメージは TakeDamage() を他のスクリプトから呼ぶ。
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Tooltip("最大 HP")]
    public int maxHealth = 100;
    [Tooltip("開始時の HP（回復を試しやすいように最大値より低くしてある）")]
    public int startHealth = 50;

    public int CurrentHealth { get; private set; }

    const float DamageFlashDuration = 0.3f;
    private float damageFlashTimer = 0f;

    void Update()
    {
        if (damageFlashTimer > 0f) damageFlashTimer -= Time.deltaTime;
    }

    void Awake()
    {
        CurrentHealth = Mathf.Clamp(startHealth, 0, maxHealth);
    }

    /// <summary>HP を回復する。すでに満タンなら何もせず false を返す。</summary>
    public bool Heal(int amount)
    {
        if (CurrentHealth >= maxHealth) return false;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        return true;
    }

    /// <summary>ダメージを受ける（罠や敵を作るとき用）。</summary>
    public void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        damageFlashTimer = DamageFlashDuration;
        if (CurrentHealth == 0)
        {
            Debug.Log("HP が 0 になりました。");
        }
    }

    // ---- HP バー表示 ----
    void OnGUI()
    {
        const float x = 20f, y = 20f, w = 200f, h = 20f;
        float ratio = maxHealth > 0 ? (float)CurrentHealth / maxHealth : 0f;

        Color prev = GUI.color;

        // ダメージを受けた直後は画面を赤くする
        if (damageFlashTimer > 0f)
        {
            GUI.color = new Color(1f, 0f, 0f, 0.35f * (damageFlashTimer / DamageFlashDuration));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        }

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, w + 4, h + 4), Texture2D.whiteTexture);
        GUI.color = Color.Lerp(Color.red, Color.green, ratio);
        GUI.DrawTexture(new Rect(x, y, w * ratio, h), Texture2D.whiteTexture);
        GUI.color = prev;

        GUI.Label(new Rect(x + 6, y + 1, w, h), $"HP {CurrentHealth} / {maxHealth}");
    }
}
