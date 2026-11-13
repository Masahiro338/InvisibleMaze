using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// メニュー「Tools > ダメージトラップを配置」で、
/// Scene ビューの中心の真下の地面に赤い板のトラップを1つ置く。
/// ・Player に PlayerHealth がなければ自動で追加する
/// ・赤いマテリアル (Assets/Materials/DamageTrap.mat) を自動作成する
/// Ctrl+Z で元に戻せる。
/// </summary>
public static class DamageTrapPlacer
{
    const string MaterialPath = "Assets/Materials/DamageTrap.mat";
    const float TrapSize = 1f;          // 板の一辺 (m)
    const float PlateThickness = 0.05f; // 板の厚さ (m)
    const float TriggerHeight = 0.5f;   // 踏んだと判定する高さ (m)

    [MenuItem("Tools/ダメージトラップを配置")]
    static void Place()
    {
        Undo.SetCurrentGroupName("Place Damage Trap");
        int undoGroup = Undo.GetCurrentGroup();

        HealItemPlacer.EnsurePlayerHealth();

        Vector3 xz = Vector3.zero;
        if (SceneView.lastActiveSceneView != null) xz = SceneView.lastActiveSceneView.pivot;
        Vector3 ground = HealItemPlacer.FindGroundPosition(xz);

        // ---- 本体：床の上に置く薄い赤い板 ----
        GameObject trap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trap.name = "DamageTrap";
        Undo.RegisterCreatedObjectUndo(trap, "Create Damage Trap");
        trap.transform.position = ground + Vector3.up * (PlateThickness / 2f + 0.005f);
        trap.transform.localScale = new Vector3(TrapSize, PlateThickness, TrapSize);

        // 判定は板より上に伸ばす（足が少し浮いていても踏んだと判定されるように）
        var box = trap.GetComponent<BoxCollider>();
        box.isTrigger = true;
        float h = TriggerHeight / PlateThickness; // ローカル座標での高さ
        box.size = new Vector3(1f, h, 1f);
        box.center = new Vector3(0f, h / 2f - 0.5f, 0f);

        var rb = trap.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        trap.AddComponent<DamageTrap>();

        var mat = GetOrCreateMaterial();
        if (mat != null) trap.GetComponent<MeshRenderer>().sharedMaterial = mat;

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(trap.scene);
        Selection.activeGameObject = trap;
        Debug.Log("ダメージトラップを配置しました。移動ツール (W) で好きな場所に動かせます。Ctrl+S でシーンを保存してください。", trap);
    }

    static Material GetOrCreateMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        Color red = new Color(1f, 0.15f, 0.1f);
        mat = new Material(shader);
        mat.SetColor("_BaseColor", red);
        mat.SetColor("_Color", red);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", red * 0.8f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
