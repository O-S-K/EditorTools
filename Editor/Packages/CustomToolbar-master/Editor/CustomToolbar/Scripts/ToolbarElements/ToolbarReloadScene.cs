﻿using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityToolbarExtender;

[Serializable]
internal class ToolbarReloadScene : BaseToolbarElement
{
    private static GUIContent reloadSceneBtn;

    public override string NameInList => "[Button] Reload scene";
    public override int SortingGroup => 3;

    public override void Init()
    {
        // reloadSceneBtn =
        //     new GUIContent(
        //         (Texture2D)AssetDatabase.LoadAssetAtPath(
        //             $"{GetPackageRootPath}/Editor/CustomToolbar/Icons/LookDevResetEnv@2x.png", typeof(Texture2D)),
        //         "Reload scene");
    }

    protected override void OnDrawInList(Rect position)
    {
    }

    protected override void OnDrawInToolbar()
    {
        // EditorGUIUtility.SetIconSize(new Vector2(17, 17));
        // if (GUILayout.Button(reloadSceneBtn, ToolbarStyles.commandButtonStyle))
        // {
        //     SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //     EditorApplication.isPlaying = false;
        // }
    }
}