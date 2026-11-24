using System.Collections;
using System.IO;
using UnityEngine;

// Demo režim pro screenshoty a automatické hraní:
//   -autoplay       hraje samo
//   -screenshots    uloží animovaný jumpscare, pak skončí
//   -shotdir <cesta>
//   -demo           start s předvyplněnou šachtou
//   -jumpscare      vynutí jumpscare aktuálního stylu
public class AutoDemo : MonoBehaviour
{
    bool ridit;

    void Start()
    {
        if (CmdArgs.Has("-autoplay") || CmdArgs.Has("-screenshots") || CmdArgs.Has("-demo") || CmdArgs.Has("-jumpscare"))
            StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return null;
        yield return null;

        if (CmdArgs.Has("-screenshots"))
        {
            yield return TakeShots();
            yield break;
        }

        if (GameManager.I != null)
            GameManager.I.StartGame(CmdArgs.Has("-demo"));
        if (CmdArgs.Has("-jumpscare") && GameManager.I != null)
            yield return GameManager.I.ForceJumpscare();
        if (CmdArgs.Has("-autoplay"))
            StartCoroutine(Ai());
    }

    IEnumerator TakeShots()
    {
        string dir = CmdArgs.Value("-shotdir", Path.Combine(Application.dataPath, "..", "Screenshots"));
        try { Directory.CreateDirectory(dir); }
        catch { /* ignore */ }

        try
        {
            if (GameManager.I != null) GameManager.I.StartGame(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Kostka] StartGame (screenshot): " + e);
        }
        if (CameraOrbit.I != null)
        {
            CameraOrbit.I.autoOrbit = false;
            CameraOrbit.I.yaw = 38f;
            CameraOrbit.I.targetYaw = 38f;
            CameraOrbit.I.pitch = 42f;
        }
        yield return new WaitForSeconds(0.6f);

        // Animovaný fotojumpscare (výchozí styl) – snímek v momentu skoku do tváře
        if (GameManager.I != null)
        {
            GameManager.I.jumpscareStyl = GameManager.StylAnim;
            GameManager.I.StartCoroutine(GameManager.I.ForceJumpscare());
        }
        yield return new WaitForSeconds(0.36f);
        yield return Shot(dir, "04_pavouk_anim.png");
        yield return new WaitForSeconds(1.0f);

        try { File.WriteAllText(Path.Combine(dir, "shots_done.txt"), "ok"); }
        catch { /* ignore */ }

        yield return new WaitForSeconds(0.3f);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator Shot(string dir, string file)
    {
        string path = Path.Combine(dir, file);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[Kostka] Screenshot: " + path);
        yield return new WaitForSeconds(0.45f);
    }

    IEnumerator Ai()
    {
        ridit = true;
        while (ridit && GameManager.I != null)
        {
            if (GameManager.I.stav != GameManager.Stav.Hra)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }
            yield return new WaitForSeconds(0.35f);
        }
    }
}
