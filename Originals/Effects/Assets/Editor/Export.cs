using UnityEditor;
using System;
using System.IO;

public class CreateAssetBundles
{
	const string bundleName = "effects";
	const string destinationFolder = "Resources";

	[MenuItem("Assets/Export To Camera+")]
	public static void BuildStandaloneAssetBundles()
	{
		Build("Win64", BuildTarget.StandaloneWindows64);
		Build("Linux", BuildTarget.StandaloneLinux64);
		Build("MacOS", BuildTarget.StandaloneOSX);
	}

	static void Build(string arch, BuildTarget target)
	{
		var src = Path.Combine("Assets", "AssetBundles", arch);
		if (!Directory.Exists(src))
			Directory.CreateDirectory(src);

		BuildPipeline.BuildAssetBundles(src, BuildAssetBundleOptions.None, target);

		var dest = Path.Combine("..", "..", destinationFolder, arch);
		if (!Directory.Exists(dest))
			Directory.CreateDirectory(dest);

		File.Copy(Path.Combine(src, bundleName), Path.Combine(dest, bundleName), true);
	}
}