using UnityEditor;
using System.IO;

public class CreateAssetBundles
{
	[MenuItem("Assets/Export To Camera+")]
	public static void BuildStandaloneAssetBundles()
	{
		Build("Win64", BuildTarget.StandaloneWindows64);
		Build("Linux", BuildTarget.StandaloneLinux64);
		Build("MacOS", BuildTarget.StandaloneOSX);
	}


	static void Build(string arch, BuildTarget target)
	{
		var src = $"Assets\\AssetBundles\\{arch}";
		if (!Directory.Exists(src))
			Directory.CreateDirectory(src);

		BuildPipeline.BuildAssetBundles(src, BuildAssetBundleOptions.None, target);

		var dest = $"..\\..\\Resources\\{arch}";
		if (!Directory.Exists(dest))
			Directory.CreateDirectory(dest);

		File.Copy($"{src}\\effects", $"{dest}\\effects", true);
	}
}