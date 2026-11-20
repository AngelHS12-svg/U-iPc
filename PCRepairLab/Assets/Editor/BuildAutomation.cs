using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using PcFixLab;

public static class BuildAutomation
{
    public static void BuildWindows()
    {
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..",".."));
        Directory.CreateDirectory(Path.Combine(Application.dataPath,"Scenes"));
        const string scenePath="Assets/Scenes/Workshop.unity";
        var scene=File.Exists(scenePath)?EditorSceneManager.OpenScene(scenePath):EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        if(UnityEngine.Object.FindFirstObjectByType<RepairGame>()==null)new GameObject("PC FIX LAB · Game controller").AddComponent<RepairGame>();
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
        const string materialPath="Assets/Resources/WorkshopStandard.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(materialPath)==null)
        {
            var material=new Material(Shader.Find("Standard"));material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);
            AssetDatabase.CreateAsset(material,materialPath);
        }
        EditorSceneManager.SaveScene(scene,scenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scenePath,true)};
        PlayerSettings.companyName="Lizbeth Valerio Garcia";PlayerSettings.productName="U-iPC";PlayerSettings.bundleVersion="2.0.0";
        PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ManagedStrippingLevel.Disabled);
        string path=Path.Combine(root,"Builds","U-iPC","U-iPC.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
        AssetDatabase.SaveAssets();
        BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
        Debug.Log("PC FIX LAB BUILD: "+report.summary.result+" · "+report.summary.totalSize+" bytes · "+report.summary.totalErrors+" errors");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");
    }
}
