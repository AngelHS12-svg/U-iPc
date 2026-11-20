using UnityEditor;
using UnityEditor.SceneManagement;

public static class WorkshopMenu
{
    [MenuItem("PC FIX LAB/1 - Abrir taller")]
    public static void OpenWorkshop()
    {
        if(EditorApplication.isPlaying)return;
        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/Workshop.unity");
    }
    [MenuItem("PC FIX LAB/2 - Jugar en Unity")]
    public static void PlayWorkshop()
    {
        OpenWorkshop();
        if(EditorSceneManager.GetActiveScene().path=="Assets/Scenes/Workshop.unity")EditorApplication.isPlaying=true;
    }
    [MenuItem("PC FIX LAB/3 - Compilar para Windows")]
    public static void BuildWorkshop()
    {
        if(EditorApplication.isPlaying)
        { EditorUtility.DisplayDialog("PC FIX LAB","Detén Play Mode antes de compilar el juego.","Aceptar");return; }
        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())BuildAutomation.BuildWindows();
    }

    [MenuItem("PC FIX LAB/4 - Verificar recorridos y capturas")]
    public static void VerifyWorkshop()
    {
        if(EditorApplication.isPlaying)return;
        OpenWorkshop();
        if(EditorSceneManager.GetActiveScene().path!="Assets/Scenes/Workshop.unity")return;
        SessionState.SetBool("PcFixLab.RunVerification",true);
        EditorApplication.isPlaying=true;
    }
}
