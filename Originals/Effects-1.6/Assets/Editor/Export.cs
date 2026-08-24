using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CameraPlus16AssetBundles
{
	const string bundleName = "effects";
	static readonly string[] assetNames =
	{
		"Assets/Bordered.shader",
		"Assets/OutlineMask.shader"
	};

	[MenuItem("Assets/Export Camera+ 1.6 Effects")]
	public static void BuildAll()
	{
		Build("Win64", BuildTarget.StandaloneWindows64);
		Build("Linux", BuildTarget.StandaloneLinux64);
		Build("MacOS", BuildTarget.StandaloneOSX);
	}

	static void Build(string architecture, BuildTarget target)
	{
		foreach (var assetName in assetNames)
			if (AssetDatabase.LoadMainAssetAtPath(assetName) == null)
				throw new FileNotFoundException("Cannot add asset to CameraPlus 1.6 bundle", assetName);

		var output = Path.Combine("Assets", "AssetBundles", architecture);
		Directory.CreateDirectory(output);
		var builds = new[]
		{
			new AssetBundleBuild
			{
				assetBundleName = bundleName,
				assetNames = assetNames
			}
		};
		if (BuildPipeline.BuildAssetBundles(output, builds, BuildAssetBundleOptions.None, target) == null)
			throw new InvalidOperationException($"CameraPlus 1.6 effects build failed for {target}.");

		var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
		var destination = Path.Combine(repositoryRoot, "1.6", "Resources", architecture);
		Directory.CreateDirectory(destination);
		File.Copy(Path.Combine(output, bundleName), Path.Combine(destination, bundleName), true);
	}
}
