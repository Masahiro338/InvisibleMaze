using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// メニュー「Tools > 回復アイテムを配置」で、
/// Scene ビューの中心の真下の地面に回復アイテムを1つ置く。
/// ・Player に PlayerHealth がなければ自動で追加する
/// ・緑色に光るマテリアル (Assets/Materials/HealItem.mat) を自動作成する
/// Ctrl+Z で元に戻せる。
/// </summary>
public static class HealItemPlacer
{
    const string MaterialPath = "Assets/Materials/HealItem.mat";
    const float ItemSize = 0.4f;
    const float FloatHeight = 0.5f; // 地面からの高さ

    [MenuItem("Tools/回復アイテムを配置")]
    static void Place()
    {
        Undo.SetCurrentGroupName("Place Heal Item");
        int undoGroup = Undo.GetCurrentGroup();

        EnsurePlayerHealth();

        // ---- 置く場所：Scene ビューの中心の真下 ----
        Vector3 xz = Vector3.zero;
        if (SceneView.lastActiveSceneView != null) xz = SceneView.lastActiveSceneView.pivot;
        Vector3 ground = FindGroundPosition(xz);

        // ---- アイテム本体 ----
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = "HealItem";
        Undo.RegisterCreatedObjectUndo(item, "Create Heal Item");
        item.transform.position = ground + Vector3.up * FloatHeight;
        item.transform.localScale = Vector3.one * ItemSize;

        var col = item.GetComponent<Collider>();
        col.isTrigger = true;

        var rb = item.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        item.AddComponent<HealItem>();

        var mat = GetOrCreateMaterial();
        if (mat != null) item.GetComponent<MeshRenderer>().sharedMaterial = mat;

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(item.scene);
        Selection.activeGameObject = item;
        Debug.Log("回復アイテムを配置しました。移動ツール (W) で好きな場所に動かせます。Ctrl+S でシーンを保存してください。", item);
    }

    internal static void EnsurePlayerHealth()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning("Player が見つかりません。先に「Tools > FPS Player をセットアップ」を実行してください。");
            return;
        }
        if (pc.GetComponent<PlayerHealth>() == null)
        {
            Undo.AddComponent<PlayerHealth>(pc.gameObject);
            Debug.Log("Player に PlayerHealth を追加しました。", pc);
        }
    }

    /// <summary>指定 XZ の真下にある床の上面を探す。見つからなければ y=0。</summary>
    internal static Vector3 FindGroundPosition(Vector3 xz)
    {
        Physics.SyncTransforms();
        Vector3 origin = new Vector3(xz.x, 50f, xz.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        return new Vector3(xz.x, 0f, xz.z);
    }

    static Material GetOrCreateMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        Color green = new Color(0.2f, 1f, 0.4f);
        mat = new Material(shader);
        mat.SetColor("_BaseColor", green);
        mat.SetColor("_Color", green);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", green * 1.5f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
