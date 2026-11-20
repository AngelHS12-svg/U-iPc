using UnityEditor;

public static class ExportV2
{
    [MenuItem("PC FIX LAB/5 - Exportar U-iPC 2")]
    public static void Export()
    {
        if(EditorApplication.isPlaying)
        {EditorUtility.DisplayDialog("U-iPC","Detén Play Mode antes de exportar.","Aceptar");return;}
        BuildAutomation.BuildWindows();
    }
}
