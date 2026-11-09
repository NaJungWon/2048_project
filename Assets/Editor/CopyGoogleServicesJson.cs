#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class CopyGoogleServicesJsonPrebuild : IPreprocessBuildWithReport
{
    public int callbackOrder => -9999; // 최대한 먼저

    public void OnPreprocessBuild(BuildReport report)
    {
        string src = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "google-services.json");
        if (!File.Exists(src))
        {
            UnityEngine.Debug.LogError($"[Firebase] google-services.json이 없습니다: {src}");
            return;
        }

        // Unity 2022 Bee 경로(네 로그와 동일)
        string gradleRoot = Path.Combine(Directory.GetCurrentDirectory(),
            "Library", "Bee", "Android", "Prj", "Mono2x", "Gradle");

        string launcherDir = Path.Combine(gradleRoot, "launcher");

        // 플러그인이 찾는 후보 경로들(로그 그대로)
        string[] targets =
        {
            Path.Combine(launcherDir, "google-services.json"),
            Path.Combine(launcherDir, "src", "google-services.json"),
            Path.Combine(launcherDir, "src", "release", "google-services.json"),
            Path.Combine(launcherDir, "src", "debug", "google-services.json"),
        };

        foreach (var dst in targets)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, true);
        }

        UnityEngine.Debug.Log("[Firebase] (Prebuild) Bee Gradle launcher에 google-services.json 선복사 완료");
    }
}
#endif