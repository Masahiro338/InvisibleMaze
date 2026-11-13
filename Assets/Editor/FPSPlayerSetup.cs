using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// メニュー「Tools > FPS Player をセットアップ」で、
/// 開いているシーンに FPS 用の Player を自動作成する。
/// ・Player オブジェクト + CharacterController + PlayerController
/// ・Main Camera を Player の子にして目の高さへ配置
/// 何度実行しても Player は重複しない（既存の Player を再設定する）。
/// Ctrl+Z で元に戻せる。
/// </summary>
public static class FPSPlayerSetup
{
    const string PlayerName = "Player";
    const float EyeHeight = 1.2f;
    const float BodyHeight = 1.4f;
    static readonly Vector3 DefaultSpawnXZ = new Vector3(-3f, 0f, -3f);

    [MenuItem("Tools/FPS Player をセットアップ")]
    static void Setup()
    {
        Undo.SetCurrentGroupName("FPS Player Setup");
        int undoGroup = Undo.GetCurrentGroup();

        // ---- Player 本体 ----
        GameObject player = GameObject.Find(PlayerName);
        if (player == null)
        {
            player = new GameObject(PlayerName);
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            player.transform.position = FindGroundPosition(DefaultSpawnXZ);
        }
        else
        {
            Undo.RecordObject(player.transform, "Setup Player");
        }

        // ---- CharacterController ----
        var controller = player.GetComponent<CharacterController>();
        if (controller == null) controller = Undo.AddComponent<CharacterController>(player);
        Undo.RecordObject(controller, "Setup CharacterController");
        controller.height = BodyHeight;
        controller.radius = 0.3f;
        controller.center = new Vector3(0f, BodyHeight / 2f, 0f); // 足元 = Player の位置
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;
        controller.skinWidth = 0.03f;
        controller.minMoveDistance = 0f;

        // ---- PlayerController ----
        var pc = player.GetComponent<PlayerController>();
        if (pc == null) pc = Undo.AddComponent<PlayerController>(player);

        // ---- カメラを子にする ----
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGo, "Create Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }

        Undo.SetTransformParent(cam.transform, player.transform, "Parent Camera");
        Undo.RecordObject(cam.transform, "Setup Camera");
        cam.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
        cam.transform.localRotation = Quaternion.identity;
        cam.transform.localScale = Vector3.one;
        if (cam.nearClipPlane > 0.05f)
        {
            Undo.RecordObject(cam, "Setup Camera");
            cam.nearClipPlane = 0.05f; // 壁に近づいたとき壁が透けないように
        }

        Undo.RecordObject(pc, "Setup PlayerController");
        pc.playerCamera = cam.transform;

        // ---- 壁・床の Collider チェック ----
        WarnAboutColliders(player);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = player;
        Debug.Log("FPS Player をセットアップしました。Ctrl+S でシーンを保存してください。", player);
    }

    /// <summary>指定 XZ の真下にある床の上面を探す。見つからなければ y=0。</summary>
    static Vector3 FindGroundPosition(Vector3 xz)
    {
        Physics.SyncTransforms();
        Vector3 origin = new Vector3(xz.x, 50f, xz.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.05f;
        }
        return new Vector3(xz.x, 0f, xz.z);
    }

    static void WarnAboutColliders(GameObject player)
    {
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (r.transform.IsChildOf(player.transform)) continue;
            var col = r.GetComponent<Collider>();
            if (col == null)
                Debug.LogWarning($"'{r.name}' に Collider がありません。すり抜けます。", r);
            else if (col.isTrigger)
                Debug.LogWarning($"'{r.name}' の Collider が Is Trigger です。すり抜けます。", r);
        }
    }
}
