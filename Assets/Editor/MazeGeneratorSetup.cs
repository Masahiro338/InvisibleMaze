using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// メニュー「Tools > 迷路ジェネレーターをセットアップ」で、
/// MazeGenerator を作成（すでにあれば再設定）し、今ある外周の壁の内側にぴったり合わせる。
/// 迷路を広げたあとにもう一度実行すれば、新しい広さに合わせ直せる。
/// Ctrl+Z で元に戻せる。
/// </summary>
public static class MazeGeneratorSetup
{
    const float TargetCellSize = 2f; // 1マスの目安の大きさ (m)。大きいほどマスが減って簡単になる

    [MenuItem("Tools/迷路ジェネレーターをセットアップ")]
    static void Setup()
    {
        Undo.SetCurrentGroupName("Setup Maze Generator");
        int undoGroup = Undo.GetCurrentGroup();
        var scene = EditorSceneManager.GetActiveScene();

        // ---- 外周の壁を探す：高さがあり、片方の横幅が薄い箱 ----
        bool found = false;
        Bounds bounds = default;
        float thickness = 0.1f;
        Material wallMat = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<MazeGenerator>() != null) continue;
            var box = root.GetComponent<BoxCollider>();
            var renderer = root.GetComponent<MeshRenderer>();
            if (box == null || box.isTrigger || renderer == null) continue;

            Vector3 s = root.transform.lossyScale;
            float thin = Mathf.Min(Mathf.Abs(s.x), Mathf.Abs(s.z));
            float longSide = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
            if (Mathf.Abs(s.y) < 1f || thin > 0.5f || longSide < 1f) continue;

            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
            thickness = thin;
            if (wallMat == null) wallMat = renderer.sharedMaterial;
        }

        if (!found)
        {
            Debug.LogWarning("外周の壁が見つかりませんでした。MazeGenerator を作成したあと、位置とマス数を手動で設定してください。");
        }

        // ---- MazeGenerator を作る or 再利用 ----
        var gen = Object.FindFirstObjectByType<MazeGenerator>();
        if (gen == null)
        {
            var go = new GameObject("MazeGenerator");
            Undo.RegisterCreatedObjectUndo(go, "Create Maze Generator");
            gen = go.AddComponent<MazeGenerator>();
        }
        Undo.RecordObject(gen.transform, "Setup Maze Generator");
        Undo.RecordObject(gen, "Setup Maze Generator");

        if (found)
        {
            // 壁の内側のエリア
            float innerW = bounds.size.x - thickness * 2f;
            float innerD = bounds.size.z - thickness * 2f;
            gen.columns = Mathf.Max(2, Mathf.RoundToInt(innerW / TargetCellSize));
            gen.rows = Mathf.Max(2, Mathf.RoundToInt(innerD / TargetCellSize));
            gen.cellSize = new Vector2(innerW / gen.columns, innerD / gen.rows);
            gen.wallThickness = thickness;
            gen.wallHeight = bounds.size.y;
            gen.wallMaterial = wallMat;
            gen.transform.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            Debug.Log($"迷路ジェネレーターを外周の壁に合わせました：{gen.columns} × {gen.rows} マス、" +
                      $"1マス {gen.cellSize.x:0.##}m × {gen.cellSize.y:0.##}m。", gen);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = gen.gameObject;
        Debug.Log("再生すると内側の壁が毎回ランダムに生成されます。Ctrl+S でシーンを保存してください。", gen);
    }
}
