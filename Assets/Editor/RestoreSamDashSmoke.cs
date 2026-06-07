using UnityEditor;
using UnityEngine;

public static class RestoreSamDashSmoke
{
	const string SourcePath = "Assets/Resources/Heroes/Sam/Sam_preV2_source.prefab";
	const string TargetPath = "Assets/Resources/Heroes/Sam/Sam.prefab";

	[MenuItem("Tools/Restore Sam Dash_Smoke")]
	public static void RestoreFromMenu()
	{
		Run(false);
	}

	public static void Restore()
	{
		Run(true);
	}

	static void Run(bool exitAfter)
	{
		var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
		if (sourcePrefab == null)
		{
			Debug.LogError("RestoreSamDashSmoke: source prefab not found at " + SourcePath);
			if (exitAfter) EditorApplication.Exit(1);
			return;
		}

		Transform smokeSource = FindDeepChild(sourcePrefab.transform, "Dash_Smoke");
		if (smokeSource == null)
		{
			Debug.LogError("RestoreSamDashSmoke: Dash_Smoke not found in source prefab");
			if (exitAfter) EditorApplication.Exit(1);
			return;
		}

		GameObject targetRoot = PrefabUtility.LoadPrefabContents(TargetPath);

		Transform existing = FindDeepChild(targetRoot.transform, "Dash_Smoke");
		if (existing != null)
			Object.DestroyImmediate(existing.gameObject);

		GameObject copy = Object.Instantiate(smokeSource.gameObject);
		copy.name = "Dash_Smoke";
		copy.transform.SetParent(targetRoot.transform, false);

		PrefabUtility.SaveAsPrefabAsset(targetRoot, TargetPath);
		PrefabUtility.UnloadPrefabContents(targetRoot);

		AssetDatabase.DeleteAsset(SourcePath);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log("RestoreSamDashSmoke: Dash_Smoke restored to " + TargetPath);
		if (exitAfter) EditorApplication.Exit(0);
	}

	static Transform FindDeepChild(Transform parent, string childName)
	{
		if (parent == null)
			return null;

		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if (child.name == childName)
				return child;

			Transform nested = FindDeepChild(child, childName);
			if (nested != null)
				return nested;
		}

		return null;
	}
}
