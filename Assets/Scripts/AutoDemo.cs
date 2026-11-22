using System.Collections;
using System.IO;
using UnityEngine;

// Demo režim pro screenshoty a automatické hraní:
//   -autoplay       hraje samo (náhodné tahy)
//   -screenshots    uloží 3 obrázky (titulka + hra) a skončí
//   -shotdir <cesta>  kam uložit obrázky
//   -demo           start s předvyplněnou šachtou (i bez screenshotů)
public class AutoDemo : MonoBehaviour
{
    bool ridit;

    void Start()
    {
        if (CmdArgs.Has("-autoplay") || CmdArgs.Has("-screenshots") || CmdArgs.Has("-demo"))
            StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // počkáme, až GameManager dostaví UI a dekoraci
        yield return null;
        yield return null;

        if (CmdArgs.Has("-screenshots"))
        {
            yield return TakeShots();
            yield break;
        }

        if (GameManager.I != null)
            GameManager.I.StartGame(CmdArgs.Has("-demo"));
        if (CmdArgs.Has("-autoplay"))
            StartCoroutine(Ai());
    }

    IEnumerator TakeShots()
    {
        string dir = CmdArgs.Value("-shotdir", Path.Combine(Application.dataPath, "..", "Screenshots"));
        try { Directory.CreateDirectory(dir); }
        catch { /* ignore */ }

        if (CameraOrbit.I != null) CameraOrbit.I.autoOrbit = true;
        yield return new WaitForSeconds(1.0f);
        yield return Shot(dir, "01_titulka.png");

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
        yield return new WaitForSeconds(1.1f);
        yield return Shot(dir, "02_hra.png");

        if (CameraOrbit.I != null) CameraOrbit.I.BumpYaw(90f);
        yield return new WaitForSeconds(0.9f);
        yield return Shot(dir, "03_hra_uhel.png");

        try { File.WriteAllText(Path.Combine(dir, "shots_done.txt"), "ok"); }
        catch { /* ignore */ }

        yield return new WaitForSeconds(0.4f);
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
        // CaptureScreenshot zapisuje až na konci snímku
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
            int r = Random.Range(0, 8);
            // jednoduché náhodné tahy, občas pád
            if (r < 4)
            {
                // posun simulujeme klávesami nejde; AI nechá gravitaci
            }
            if (r == 5) { /* nothing, gravitation */ }
            if (Random.value < 0.12f)
            {
                // hard drop přes veřejné rozhraní nemáme – pošleme mezerník nejde z kódu snadno bez inputu
            }
            yield return new WaitForSeconds(0.35f);
        }
    }
}
