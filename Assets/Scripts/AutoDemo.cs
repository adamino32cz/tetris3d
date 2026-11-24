using System.Collections;
using System.IO;
using UnityEngine;

// Demo režim pro screenshoty a automatické hraní:
//   -autoplay       hraje samo
//   -screenshots    uloží videojumpscare, pak skončí
//   -shotdir <cesta>
//   -demo           start s předvyplněnou šachtou
//   -jumpscare      vynutí jumpscare aktuálního stylu
//   -skinshots      uloží snímky menu a skinů (skins_*.png)
//   -fixshots       snímky opravy EXTRA/Astolfo (fix_*.png)
//   -fix2shots      nový Astolfo portrét, jumpscare a menu na konci
//   -fix4shots      otočená MIKU EXTRA a nový portrét Miku
//   -openskins      otevře menu SKINY
//   -skin <id>      vybere vzhled (pastel/neon/mono/candy/ocean/forest/67/miku)
public class AutoDemo : MonoBehaviour
{
    bool ridit;

    void Start()
    {
        if (CmdArgs.Has("-autoplay") || CmdArgs.Has("-screenshots") || CmdArgs.Has("-demo") || CmdArgs.Has("-jumpscare") || CmdArgs.Has("-skinshots") || CmdArgs.Has("-fixshots") || CmdArgs.Has("-fix2shots") || CmdArgs.Has("-fix4shots") || CmdArgs.Has("-openskins"))
            StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return null;
        yield return null;

        if (CmdArgs.Has("-fix4shots"))
        {
            yield return TakeFix4Shots();
            yield break;
        }
        if (CmdArgs.Has("-fix2shots"))
        {
            yield return TakeFix2Shots();
            yield break;
        }
        if (CmdArgs.Has("-fixshots"))
        {
            yield return TakeFixShots();
            yield break;
        }
        if (CmdArgs.Has("-skinshots"))
        {
            yield return TakeSkinShots();
            yield break;
        }

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
        yield return new WaitForSeconds(0.9f);

        // Videojumpscare (výchozí styl) – snímek v momentu skoku do tváře
        if (GameManager.I != null)
        {
            GameManager.I.jumpscareStyl = GameManager.StylVideo;
            GameManager.I.StartCoroutine(GameManager.I.ForceJumpscare());
        }
        float t0 = Time.unscaledTime;
        while (JumpscarePavouk.VideoFrameIndex < 31 && Time.unscaledTime - t0 < 3.0f)
            yield return null;
        yield return Shot(dir, "05_pavouk_video.png");
        yield return new WaitForSeconds(0.8f);

        try { File.WriteAllText(Path.Combine(dir, "shots_done.txt"), "ok"); }
        catch { /* ignore */ }

        yield return new WaitForSeconds(0.3f);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator TakeSkinShots()
    {
        string dir = CmdArgs.Value("-shotdir", Path.Combine(Application.dataPath, "..", "Screenshots"));
        try { Directory.CreateDirectory(dir); }
        catch { /* ignore */ }

        if (CameraOrbit.I != null)
        {
            CameraOrbit.I.autoOrbit = false;
            CameraOrbit.I.yaw = 38f;
            CameraOrbit.I.targetYaw = 38f;
            CameraOrbit.I.pitch = 42f;
        }
        yield return new WaitForSeconds(0.4f);

        // 1) menu SKINY
        if (GameManager.I != null) GameManager.I.OpenSkinMenu();
        yield return new WaitForSeconds(0.25f);
        yield return Shot(dir, "skins_menu.png");

        // 2) paleta Neon ve hře s předvyplněnou šachtou
        if (GameManager.I != null)
        {
            GameManager.I.SelectSkinId("neon");
            GameManager.I.CloseSkinMenu();
            GameManager.I.StartGame(true);
        }
        yield return new WaitForSeconds(0.7f);
        yield return Shot(dir, "skins_palette.png");

        // 3) tajný 67 – animovaná textura na kostkách
        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("67");
            GameManager.I.SelectSkinId("67");
            GameManager.I.StartGame(true);
        }
        yield return new WaitForSeconds(0.85f); // pár snímků animace
        yield return Shot(dir, "skins_67.png");

        // 4) MIKU – portrét na stěnách kostek
        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("MIKU");
            GameManager.I.SelectSkinId("miku");
            GameManager.I.StartGame(true);
        }
        yield return new WaitForSeconds(0.85f);
        yield return Shot(dir, "skins_miku.png");

        // 5) MIKU EXTRA – pózovaná figurka (téčko = T-póza)
        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("MIKU EXTRA");
            GameManager.I.SelectSkinId("miku_extra");
            GameManager.I.StartGame(true);
            GameManager.I.DemoNastavTvar(3);
        }
        yield return new WaitForSeconds(0.9f);
        yield return Shot(dir, "skins_miku_extra.png");

        // 6) ASTOLFO – portrét na stěnách
        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("ASTOLFO");
            GameManager.I.SelectSkinId("astolfo");
            GameManager.I.StartGame(true);
        }
        yield return new WaitForSeconds(0.85f);
        yield return Shot(dir, "skins_astolfo.png");

        // 7) ASTOLFO EXTRA – pózovaná figurka
        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("ASTOLFO EXTRA");
            GameManager.I.SelectSkinId("astolfo_extra");
            GameManager.I.StartGame(true);
            GameManager.I.DemoNastavTvar(3);
        }
        yield return new WaitForSeconds(0.9f);
        yield return Shot(dir, "skins_astolfo_extra.png");

        // 8) jumpscare obsah (DEV) – Miku vyskočí s portrétu
        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("DEV");
            KostkaSkin.SetJump(KostkaSkin.Current() != null ? KostkaSkin.Current().id : "pastel", "miku");
            GameManager.I.StartGame(true);
            GameManager.I.StartCoroutine(GameManager.I.ForceJumpscare());
        }
        yield return new WaitForSeconds(0.4f);
        yield return Shot(dir, "skins_jump_miku.png");
        yield return new WaitForSeconds(1.0f);

        // 9) panel DEV / modifikace
        if (GameManager.I != null) GameManager.I.DemoNavratNaTitulku();
        yield return null;
        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("DEV");
            GameManager.I.OpenSkinMenu();
        }
        yield return new WaitForSeconds(0.5f);
        yield return Shot(dir, "skins_dev.png");

        try { File.WriteAllText(Path.Combine(dir, "skins_done.txt"), "ok"); }
        catch { /* ignore */ }
        yield return new WaitForSeconds(0.2f);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator TakeFix4Shots()
    {
        string dir = CmdArgs.Value("-shotdir", Path.Combine(Application.dataPath, "..", "Screenshots"));
        try { Directory.CreateDirectory(dir); }
        catch { /* ignore */ }

        if (CameraOrbit.I != null)
        {
            CameraOrbit.I.autoOrbit = false;
            CameraOrbit.I.yaw = 38f;
            CameraOrbit.I.targetYaw = 38f;
            CameraOrbit.I.pitch = 42f;
        }

        // MIKU EXTRA: T-póza, několik rotací (Y, X, Z) — figurka musí sledovat kus
        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("MIKU EXTRA");
            GameManager.I.SelectSkinId("miku_extra");
            GameManager.I.StartGame(true);
            GameManager.I.DemoNastavTvar(3);
            GameManager.I.DemoRotace(1, 1);  // E / +Y
            GameManager.I.DemoRotace(1, 1);
            GameManager.I.DemoRotace(0, 1);  // +X
        }
        yield return new WaitForSeconds(0.85f);
        yield return Shot(dir, "fix4_miku_extra_rotated.png");

        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("MIKU");
            GameManager.I.SelectSkinId("miku");
            GameManager.I.StartGame(true);
        }
        yield return new WaitForSeconds(0.85f);
        yield return Shot(dir, "fix4_miku_face.png");

        try { File.WriteAllText(Path.Combine(dir, "fix4_done.txt"), "ok"); }
        catch { /* ignore */ }
        yield return new WaitForSeconds(0.2f);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator TakeFix2Shots()
    {
        string dir = CmdArgs.Value("-shotdir", Path.Combine(Application.dataPath, "..", "Screenshots"));
        try { Directory.CreateDirectory(dir); }
        catch { /* ignore */ }

        if (CameraOrbit.I != null)
        {
            CameraOrbit.I.autoOrbit = false;
            CameraOrbit.I.yaw = 38f;
            CameraOrbit.I.targetYaw = 38f;
            CameraOrbit.I.pitch = 42f;
        }

        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("ASTOLFO");
            GameManager.I.SelectSkinId("astolfo");
            GameManager.I.StartGame(true);
        }
        yield return new WaitForSeconds(0.85f);
        yield return Shot(dir, "fix2_astolfo.png");

        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("DEV");
            KostkaSkin.SetJump(KostkaSkin.Current() != null ? KostkaSkin.Current().id : "astolfo", "astolfo");
            GameManager.I.StartGame(true);
            GameManager.I.StartCoroutine(GameManager.I.ForceJumpscare());
        }
        yield return new WaitForSeconds(0.4f);
        yield return Shot(dir, "fix2_astolfo_jump.png");
        yield return new WaitForSeconds(1.0f);

        if (GameManager.I != null)
        {
            GameManager.I.StartGame(true);
            GameManager.I.DemoKonecHry();
        }
        yield return new WaitForSeconds(0.4f);
        yield return Shot(dir, "fix2_gameover_menu.png");

        try { File.WriteAllText(Path.Combine(dir, "fix2_done.txt"), "ok"); }
        catch { /* ignore */ }
        yield return new WaitForSeconds(0.2f);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator TakeFixShots()
    {
        string dir = CmdArgs.Value("-shotdir", Path.Combine(Application.dataPath, "..", "Screenshots"));
        try { Directory.CreateDirectory(dir); }
        catch { /* ignore */ }

        if (CameraOrbit.I != null)
        {
            CameraOrbit.I.autoOrbit = false;
            CameraOrbit.I.yaw = 38f;
            CameraOrbit.I.targetYaw = 38f;
            CameraOrbit.I.pitch = 42f;
        }

        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("MIKU EXTRA");
            GameManager.I.SelectSkinId("miku_extra");
            GameManager.I.StartGame(true);
            GameManager.I.DemoNastavTvar(3);
            GameManager.I.DemoZamkni();
        }
        yield return new WaitForSeconds(0.7f);
        yield return Shot(dir, "fix_miku_extra_landed.png");

        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("ASTOLFO EXTRA");
            GameManager.I.SelectSkinId("astolfo_extra");
            GameManager.I.StartGame(true);
            GameManager.I.DemoNastavTvar(3);
            GameManager.I.DemoZamkni();
        }
        yield return new WaitForSeconds(0.7f);
        yield return Shot(dir, "fix_astolfo_extra_landed.png");

        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("ASTOLFO");
            GameManager.I.SelectSkinId("astolfo");
            GameManager.I.StartGame(true);
        }
        yield return new WaitForSeconds(0.85f);
        yield return Shot(dir, "fix_astolfo_portrait.png");

        if (GameManager.I != null)
        {
            GameManager.I.TryUnlockCode("DEV");
            KostkaSkin.SetJump(KostkaSkin.Current() != null ? KostkaSkin.Current().id : "astolfo", "astolfo");
            GameManager.I.StartGame(true);
            GameManager.I.StartCoroutine(GameManager.I.ForceJumpscare());
        }
        yield return new WaitForSeconds(0.4f);
        yield return Shot(dir, "fix_astolfo_jumpscare.png");
        yield return new WaitForSeconds(1.0f);

        try { File.WriteAllText(Path.Combine(dir, "fix_done.txt"), "ok"); }
        catch { /* ignore */ }
        yield return new WaitForSeconds(0.2f);
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
        // CaptureScreenshot zapisuje až na konci snímku / se zpožděním
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 1.2f)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 1000) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
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
