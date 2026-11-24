using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

// Hlavní smyčka hry KOSTKA: stavy, ovládání, skóre, UI.
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public enum Stav { Titulka, Skiny, Hra, Pauza, Konec, Mazani }

    [Header("Písma a materiály (doplní scéna)")]
    public Font fontTitleSrc;
    public Font fontUiSrc;
    public TMP_FontAsset fontTitle;
    public TMP_FontAsset fontUi;
    public Material litTemplate;
    public Material unlitTemplate;

    public Stav stav = Stav.Titulka;
    public Board board = new Board();

    const string PrefRekord = "Kostka.HighScore";
    const string PrefJumpscare = "Kostka.Jumpscare";
    const string PrefJumpscareStyl = "Kostka.JumpscareStylV3";
    public const int Styl3D = 0;
    public const int StylKlasik = 1;
    public const int StylAnim = 2;
    public const int StylVideo = 3;
    const int RadNaLevel = 8;

    int skore, rekord, level = 1, rady;
    public bool jumpscareZapnuto = true;
    public int jumpscareStyl = StylVideo;
    Texture2D pavoukFoto;
    Texture2D[] pavoukAnim;
    Texture2D[] pavoukVideo;
    AudioClip pavoukVideoZvuk;
    int tvar, dalsiTvar;
    Vector3Int puvod;
    Vector3Int[] lokal;
    Vector3 vizPuvod;
    float padani;
    float das;
    Vector3Int dasDir;
    bool busy;

    readonly List<int> pytel = new List<int>();
    readonly Dictionary<Vector3Int, Transform> lezici = new Dictionary<Vector3Int, Transform>();
    readonly List<FiguraLezici> leziciFigury = new List<FiguraLezici>();

    class FiguraLezici
    {
        public GameObject go;
        public readonly List<Vector3Int> cells = new List<Vector3Int>();
        public readonly List<Transform> markery = new List<Transform>();
        public int tvar;
        public Color barva;
    }

    readonly List<GameObject> dekor = new List<GameObject>();

    PieceView aktivni, duch, nahled;
    Transform svet, leziciRoot, dekorRoot;
    Camera nahledCam;
    RenderTexture nahledRt;

    Canvas canvas;
    RectTransform titulka, hud, pauzaPanel, konecPanel, skinyPanel;
    TextMeshProUGUI txtSkore, txtRekord, txtLevel, txtRady, txtKonecSkore, txtTitulRekord, txtCombo, txtTabulka, txtNapis;
    TextMeshProUGUI txtSkinyKod, txtSkinyHint;
    Transform skinySloty;
    RectTransform devPanel;
    int skinKurzor;
    string skinKod = "";
    Image jumpscareStin;
    RawImage nahledImg;

    public Color BarvaAktivni => KostkaSkin.ColorOf(tvar);

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.names.Length - 1), true);
        rekord = PlayerPrefs.GetInt(PrefRekord, 0);
        jumpscareZapnuto = PlayerPrefs.GetInt(PrefJumpscare, 1) != 0;
        jumpscareStyl = PlayerPrefs.GetInt(PrefJumpscareStyl, StylVideo);
        pavoukFoto = Resources.Load<Texture2D>("PavoukKlasik");
        var anim = new System.Collections.Generic.List<Texture2D>();
        for (int i = 0; i < 4; i++)
        {
            var f = Resources.Load<Texture2D>("PavoukAnim" + i);
            if (f != null) anim.Add(f);
        }
        if (anim.Count == 0 && pavoukFoto != null) anim.Add(pavoukFoto);
        pavoukAnim = anim.ToArray();
        var vid = Resources.LoadAll<Texture2D>("PavoukVideo");
        if (vid != null && vid.Length > 1)
            System.Array.Sort(vid, (a, b) => string.CompareOrdinal(a.name, b.name));
        pavoukVideo = vid;
        pavoukVideoZvuk = Resources.Load<AudioClip>("PavoukVideoZvuk");
        KostkaVisual.SetTemplates(litTemplate, unlitTemplate);
        KostkaVisual.Init();
        KostkaSkin.LoadFromPrefsAndArgs(CmdArgs.Get());
        if (GetComponent<KostkaSkinHub>() == null) gameObject.AddComponent<KostkaSkinHub>();
        ZajistiPisma();
    }

    void Start()
    {
        PostavUi();
        PostavSvet();
        if (CameraOrbit.I != null) CameraOrbit.I.autoOrbit = true;
        PostavDekoraci();
        ObnovHud();
        if (CmdArgs.Has("-openskins")) OpenSkinMenu();
    }

    void ZajistiPisma()
    {
        string znaky = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
                       "ÁČĎÉĚÍŇÓŘŠŤÚŮÝŽáčďéěíňóřšťúůýž .,;:!?-+*/=()[]{}%#@\"'`’–—/\\<>|_°";
        if (fontTitle == null && fontTitleSrc != null)
        {
            fontTitle = TMP_FontAsset.CreateFontAsset(fontTitleSrc, 90, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fontTitle.TryAddCharacters(znaky);
        }
        if (fontUi == null && fontUiSrc != null)
        {
            fontUi = TMP_FontAsset.CreateFontAsset(fontUiSrc, 80, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fontUi.TryAddCharacters(znaky);
        }
        if (fontUi == null) fontUi = fontTitle;
        if (fontTitle == null) fontTitle = fontUi;
    }

    void Update()
    {
        if (stav == Stav.Skiny)
        {
            SkinMenuInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.J))
            PrepnoutJumpscare();
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (stav == Stav.Titulka) OpenSkinMenu();
            else PrepnoutStyl();
        }

        if (Input.GetKeyDown(KeyCode.P) && (stav == Stav.Hra || stav == Stav.Pauza))
            PrepnoutPauzu();

        if (stav == Stav.Titulka && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
            StartGame(false);

        if (stav == Stav.Konec && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
            StartGame(false);

        if (stav != Stav.Hra || busy) return;

        ObsluzPohyb();
        ObsluzRotaci();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HardDrop();
            return;
        }

        float interval = IntervalPadani();
        bool soft = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (soft) interval = 0.05f;

        padani += Time.deltaTime;
        if (padani >= interval)
        {
            padani = 0f;
            if (!ZkusPosun(Vector3Int.down))
                Zamknout();
            else if (soft)
            {
                skore += 1;
                ObnovHud();
            }
        }
    }

    void LateUpdate()
    {
        if (aktivni == null || stav == Stav.Titulka) return;
        vizPuvod = Vector3.Lerp(vizPuvod, (Vector3)puvod, 1f - Mathf.Exp(-22f * Time.deltaTime));
        // vizuální plynulý posun: dočasně kreslíme interpolovaný puvod jen ve světě
        var temp = new Vector3Int(Mathf.RoundToInt(vizPuvod.x), Mathf.RoundToInt(vizPuvod.y), Mathf.RoundToInt(vizPuvod.z));
        // přesnější: nastavíme pozice ručně
        if (stav == Stav.Hra || stav == Stav.Pauza || stav == Stav.Mazani)
            NakresliAktivniPlynule();
    }

    void NakresliAktivniPlynule()
    {
        if (aktivni == null || lokal == null) return;
        aktivni.SetSmooth(vizPuvod, lokal, BarvaAktivni, false, tvar);
        AktualizujDucha();
    }

    float IntervalPadani()
    {
        return Mathf.Max(0.12f, 0.85f - (level - 1) * 0.07f);
    }

    public void StartGame(bool predplnit)
    {
        StopAllCoroutines();
        busy = false;
        Time.timeScale = 1f;
        SmazDekoraci();
        SmazLezici();
        board.Reset();
        skore = 0;
        level = 1;
        rady = 0;
        pytel.Clear();
        dalsiTvar = TahniTvar();
        if (predplnit) PredplnDemo();
        stav = Stav.Hra;
        if (titulka != null) titulka.gameObject.SetActive(false);
        if (skinyPanel != null) skinyPanel.gameObject.SetActive(false);
        if (hud != null) hud.gameObject.SetActive(true);
        if (pauzaPanel != null) pauzaPanel.gameObject.SetActive(false);
        if (konecPanel != null) konecPanel.gameObject.SetActive(false);
        if (CameraOrbit.I != null) CameraOrbit.I.autoOrbit = false;
        if (SoundFX.I != null) SoundFX.I.StartGame();
        Spawn();
        ObnovHud();
    }

    void PrepnoutPauzu()
    {
        if (stav == Stav.Hra)
        {
            stav = Stav.Pauza;
            Time.timeScale = 0f;
            if (pauzaPanel != null) pauzaPanel.gameObject.SetActive(true);
        }
        else if (stav == Stav.Pauza)
        {
            stav = Stav.Hra;
            Time.timeScale = 1f;
            if (pauzaPanel != null) pauzaPanel.gameObject.SetActive(false);
        }
    }

    int TahniTvar()
    {
        if (pytel.Count == 0)
        {
            for (int i = 0; i < PieceShapes.All.Length; i++) pytel.Add(i);
            for (int i = pytel.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int t = pytel[i]; pytel[i] = pytel[j]; pytel[j] = t;
            }
        }
        int v = pytel[pytel.Count - 1];
        pytel.RemoveAt(pytel.Count - 1);
        return v;
    }

    void Spawn()
    {
        tvar = dalsiTvar;
        dalsiTvar = TahniTvar();
        lokal = PieceShapes.Copy(PieceShapes.All[tvar].bunky);
        int maxY = 0;
        for (int i = 0; i < lokal.Length; i++) if (lokal[i].y > maxY) maxY = lokal[i].y;
        puvod = new Vector3Int(Board.W / 2, Board.H - 1 - maxY, Board.D / 2);
        vizPuvod = puvod;
        padani = 0f;
        if (!board.CanPlace(puvod, lokal))
        {
            KonecHry();
            return;
        }
        if (aktivni == null) aktivni = new PieceView("Aktivni", svet, false);
        if (duch == null) duch = new PieceView("Duch", svet, true);
        aktivni.Set(puvod, lokal, BarvaAktivni, false, tvar);
        AktualizujDucha();
        AktualizujNahled();
    }

    void AktualizujDucha()
    {
        if (duch == null || lokal == null) return;
        var g = puvod;
        while (board.CanPlace(g + Vector3Int.down, lokal)) g += Vector3Int.down;
        duch.Set(g, lokal, BarvaAktivni, true, tvar);
        // když je duch na stejném místě jako kus, trochu ho schovej
        bool same = g == puvod;
        if (same) duch.Hide();
        else duch.Show();
    }

    bool ZkusPosun(Vector3Int d)
    {
        if (lokal == null) return false;
        var n = puvod + d;
        if (!board.CanPlace(n, lokal)) return false;
        puvod = n;
        if (aktivni != null) aktivni.Set(puvod, lokal, BarvaAktivni, false, tvar);
        AktualizujDucha();
        return true;
    }

    bool ZkusRotaci(int osa, int smer)
    {
        if (lokal == null) return false;
        var rot = PieceShapes.RotateAll(lokal, osa, smer);
        Vector3Int[] kicky =
        {
            Vector3Int.zero,
            new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1),
            new Vector3Int(1,0,1), new Vector3Int(-1,0,1),
            new Vector3Int(1,0,-1), new Vector3Int(-1,0,-1),
            new Vector3Int(0,1,0), new Vector3Int(0,-1,0)
        };
        for (int i = 0; i < kicky.Length; i++)
        {
            var o = puvod + kicky[i];
            if (board.CanPlace(o, rot))
            {
                lokal = rot;
                puvod = o;
                vizPuvod = puvod;
                if (aktivni != null) aktivni.Set(puvod, lokal, BarvaAktivni, false, tvar);
                AktualizujDucha();
                return true;
            }
        }
        return false;
    }

    void HardDrop()
    {
        int kles = 0;
        while (ZkusPosun(Vector3Int.down)) kles++;
        if (kles > 0) skore += kles * 2;
        vizPuvod = puvod;
        if (SoundFX.I != null) SoundFX.I.Drop();
        Zamknout();
    }

    void Zamknout(bool dalsiSpawn = true)
    {
        if (lokal == null) return;
        board.Place(puvod, lokal, tvar + 1);
        var nove = new List<Transform>();
        bool figura = KostkaSkin.IsCharacter && aktivni != null;
        FiguraLezici fig = null;
        if (figura)
        {
            fig = new FiguraLezici();
            fig.tvar = tvar;
            fig.barva = KostkaSkin.ColorOf(tvar);
            var wrap = new GameObject("FiguraLezici");
            wrap.transform.SetParent(leziciRoot, false);
            var detached = aktivni.DetachCharacter(wrap.transform);
            fig.go = wrap;
            if (detached == null)
            {
                Destroy(wrap);
                figura = false;
                fig = null;
            }
        }
        for (int i = 0; i < lokal.Length; i++)
        {
            var p = puvod + lokal[i];
            if (!board.InBounds(p)) continue;
            if (figura)
            {
                var mark = new GameObject("FigMark");
                mark.transform.SetParent(leziciRoot, false);
                mark.transform.position = KostkaVisual.World(p);
                var b = mark.AddComponent<KostkaBunka>();
                b.tvar = tvar;
                b.ghost = false;
                lezici[p] = mark.transform;
                fig.cells.Add(p);
                fig.markery.Add(mark.transform);
            }
            else
            {
                var go = NovaBunka(leziciRoot, p, tvar, "Bunka");
                lezici[p] = go.transform;
                nove.Add(go.transform);
            }
        }
        if (figura && fig != null) leziciFigury.Add(fig);
        if (aktivni != null) aktivni.Hide();
        if (duch != null) duch.Hide();
        if (nove.Count > 0) StartCoroutine(Punch(nove));
        if (SoundFX.I != null) SoundFX.I.Lock();

        var plne = board.NajdiPlneRadky();
        if (plne.Count > 0) StartCoroutine(SmazRadky(plne));
        else if (dalsiSpawn) Spawn();
        else
        {
            busy = true;
            if (aktivni != null) aktivni.Hide();
            if (duch != null) duch.Hide();
        }
        ObnovHud();
    }

    public void DemoKonecHry()
    {
        KonecHry();
    }

    public void DemoZamkni()
    {
        if (lokal == null) return;
        Zamknout(false);
    }

    void RozbitFiguru(FiguraLezici fig, HashSet<Vector3Int> odstranene)
    {
        if (fig == null) return;
        if (fig.go != null) Destroy(fig.go);
        fig.go = null;
        for (int i = 0; i < fig.cells.Count; i++)
        {
            var c = fig.cells[i];
            if (i < fig.markery.Count && fig.markery[i] != null)
                Destroy(fig.markery[i].gameObject);
            var go = NovaBunka(leziciRoot, c, fig.tvar, "Bunka");
            lezici[c] = go.transform;
        }
        fig.markery.Clear();
        fig.cells.Clear();
    }

    IEnumerator Punch(List<Transform> list)
    {
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            float k = 1f + 0.2f * Mathf.Sin(t / 0.18f * Mathf.PI);
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) list[i].localScale = Vector3.one * KostkaVisual.CubeScale * k;
            yield return null;
        }
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null) list[i].localScale = Vector3.one * KostkaVisual.CubeScale;
    }

    IEnumerator SmazRadky(List<Board.Rada> plne)
    {
        busy = true;
        stav = Stav.Mazani;
        int celkemRad = 0;
        int celkemPater = 0;
        int plus = 0;
        bool prvni = true;

        while (plne != null && plne.Count > 0)
        {
            int n = plne.Count;
            int pater = board.PocetPlnychPater();
            int add = (n == 1 ? 100 : n == 2 ? 300 : n == 3 ? 500 : n == 4 ? 800 : 1200 + (n - 5) * 200) * level;
            if (pater > 0) add += 500 * pater * level;
            plus += add;
            celkemRad += n;
            celkemPater += pater;

            if (prvni && jumpscareZapnuto)
                yield return Jumpscare();
            prvni = false;

            var posun = board.SmazASesun(plne, out var odstranene);
            if (leziciFigury.Count > 0)
            {
                for (int fi = leziciFigury.Count - 1; fi >= 0; fi--)
                {
                    var fig = leziciFigury[fi];
                    bool zasah = false;
                    for (int ci = 0; ci < fig.cells.Count; ci++)
                        if (odstranene.Contains(fig.cells[ci])) { zasah = true; break; }
                    if (zasah)
                    {
                        RozbitFiguru(fig, odstranene);
                        leziciFigury.RemoveAt(fi);
                    }
                }
            }
            var odlet = new List<Transform>();
            var klice = new List<Vector3Int>(lezici.Keys);
            for (int i = 0; i < klice.Count; i++)
            {
                var c = klice[i];
                if (!odstranene.Contains(c)) continue;
                if (lezici.TryGetValue(c, out var tr) && tr != null) odlet.Add(tr);
                lezici.Remove(c);
            }

            Vector3 stred = Vector3.zero;
            int cnt = 0;
            for (int i = 0; i < odlet.Count; i++)
            { stred += odlet[i].position; cnt++; }
            if (cnt > 0) stred /= cnt;

            float tAnim = 0f;
            var startP = new Vector3[odlet.Count];
            var vel = new Vector3[odlet.Count];
            var spin = new Vector3[odlet.Count];
            for (int i = 0; i < odlet.Count; i++)
            {
                startP[i] = odlet[i].position;
                Vector3 outw = startP[i] - stred; outw.y = 0f;
                if (outw.sqrMagnitude < 0.01f) outw = Random.insideUnitSphere;
                outw.y = 0; outw.Normalize();
                vel[i] = outw * Random.Range(6f, 10f) + Vector3.up * Random.Range(3f, 6f);
                spin[i] = Random.insideUnitSphere * 360f;
            }
            while (tAnim < 0.45f)
            {
                tAnim += Time.deltaTime;
                for (int i = 0; i < odlet.Count; i++)
                {
                    if (odlet[i] == null) continue;
                    float tt = tAnim;
                    odlet[i].position = startP[i] + vel[i] * tt + Vector3.down * (9f * tt * tt);
                    odlet[i].Rotate(spin[i] * Time.deltaTime);
                    odlet[i].localScale = Vector3.one * KostkaVisual.CubeScale * Mathf.Lerp(1f, 0.1f, tt / 0.45f);
                }
                yield return null;
            }
            for (int i = 0; i < odlet.Count; i++)
                if (odlet[i] != null) Destroy(odlet[i].gameObject);

            var noveDic = new Dictionary<Vector3Int, Transform>();
            foreach (var kv in lezici)
            {
                var from = kv.Key;
                var to = posun.ContainsKey(from) ? posun[from] : from;
                noveDic[to] = kv.Value;
            }
            lezici.Clear();
            foreach (var kv in noveDic) lezici[kv.Key] = kv.Value;

            var figFrom = new Dictionary<GameObject, Vector3>();
            var figTo = new Dictionary<GameObject, Vector3>();
            for (int fi = leziciFigury.Count - 1; fi >= 0; fi--)
            {
                var fig = leziciFigury[fi];
                Vector3Int? dlt = null;
                bool stejne = true;
                var noveC = new List<Vector3Int>();
                for (int ci = 0; ci < fig.cells.Count; ci++)
                {
                    var from = fig.cells[ci];
                    var to = posun.ContainsKey(from) ? posun[from] : from;
                    var d = to - from;
                    if (dlt == null) dlt = d;
                    else if (d != dlt.Value) stejne = false;
                    noveC.Add(to);
                }
                if (!stejne)
                {
                    fig.cells.Clear();
                    fig.cells.AddRange(noveC);
                    RozbitFiguru(fig, null);
                    leziciFigury.RemoveAt(fi);
                    continue;
                }
                if (fig.go != null)
                {
                    figFrom[fig.go] = fig.go.transform.position;
                    Vector3 midOld = Vector3.zero, midNew = Vector3.zero;
                    for (int ci = 0; ci < fig.cells.Count; ci++)
                    {
                        midOld += KostkaVisual.World(fig.cells[ci]);
                        midNew += KostkaVisual.World(noveC[ci]);
                    }
                    if (fig.cells.Count > 0)
                    {
                        midOld /= fig.cells.Count;
                        midNew /= fig.cells.Count;
                    }
                    figTo[fig.go] = fig.go.transform.position + (midNew - midOld);
                }
                fig.cells.Clear();
                fig.cells.AddRange(noveC);
                for (int ci = 0; ci < fig.markery.Count && ci < noveC.Count; ci++)
                    if (fig.markery[ci] != null)
                        lezici[noveC[ci]] = fig.markery[ci];
            }

            float t2 = 0f;
            var fromPos = new Dictionary<Transform, Vector3>();
            var toPos = new Dictionary<Transform, Vector3>();
            foreach (var kv in lezici)
            {
                if (kv.Value == null) continue;
                fromPos[kv.Value] = kv.Value.position;
                toPos[kv.Value] = KostkaVisual.World(kv.Key);
            }
            foreach (var kv in figTo)
            {
                if (kv.Key == null) continue;
                fromPos[kv.Key.transform] = figFrom[kv.Key];
                toPos[kv.Key.transform] = kv.Value;
            }
            while (t2 < 0.20f)
            {
                t2 += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t2 / 0.20f);
                foreach (var kv in toPos)
                    if (kv.Key != null) kv.Key.position = Vector3.Lerp(fromPos[kv.Key], kv.Value, k);
                yield return null;
            }
            foreach (var kv in lezici)
                if (kv.Value != null) kv.Value.position = KostkaVisual.World(kv.Key);
            foreach (var kv in figTo)
                if (kv.Key != null) kv.Key.transform.position = kv.Value;

            plne = board.NajdiPlneRadky();
        }

        skore += plus;
        rady += celkemRad;
        int novyLevel = 1 + rady / RadNaLevel;
        if (novyLevel > level) level = novyLevel;
        UlozRekord();
        ObnovHud();
        UkazeCombo(celkemRad, celkemPater, plus);
        if (SoundFX.I != null) SoundFX.I.Clear();

        busy = false;
        stav = Stav.Hra;
        Spawn();
        ObnovHud();
    }

    void UkazeCombo(int n, int pater, int plus)
    {
        if (txtCombo == null) return;
        string nazev = n == 1 ? "ŘADA!" : n == 2 ? "DVOJITÉ!" : n == 3 ? "TROJITÉ!" : "KOMBO!";
        if (pater > 0) nazev += "\nCELÉ PATRO!";
        txtCombo.text = nazev + "\n+" + plus;
        txtCombo.gameObject.SetActive(true);
        StopCoroutine(nameof(SkryjCombo));
        StartCoroutine(SkryjCombo());
    }

    IEnumerator SkryjCombo()
    {
        yield return new WaitForSeconds(1.1f);
        if (txtCombo != null) txtCombo.gameObject.SetActive(false);
    }

    public void PrepnoutJumpscare()
    {
        jumpscareZapnuto = !jumpscareZapnuto;
        PlayerPrefs.SetInt(PrefJumpscare, jumpscareZapnuto ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void PrepnoutStyl()
    {
        // video → animovaný → fotka → procedurální 3D → video
        if (jumpscareStyl == StylVideo) jumpscareStyl = StylAnim;
        else if (jumpscareStyl == StylAnim) jumpscareStyl = StylKlasik;
        else if (jumpscareStyl == StylKlasik) jumpscareStyl = Styl3D;
        else jumpscareStyl = StylVideo;
        PlayerPrefs.SetInt(PrefJumpscareStyl, jumpscareStyl);
        PlayerPrefs.Save();
        if (!busy) StartCoroutine(UkázatStylANahled());
    }

    IEnumerator UkázatStylANahled()
    {
        busy = true;
        string txt = jumpscareStyl == StylVideo ? "Pavouk: video"
                   : jumpscareStyl == StylAnim ? "Pavouk: animovaný"
                   : jumpscareStyl == StylKlasik ? "Pavouk: fotka"
                   : "Pavouk: 3D";
        yield return UkázatNapis(txt, 0.45f);
        yield return Jumpscare();
        busy = false;
    }

    IEnumerator UkázatNapis(string text, float cas)
    {
        if (txtNapis != null)
        {
            txtNapis.text = text;
            txtNapis.gameObject.SetActive(true);
        }
        yield return new WaitForSecondsRealtime(cas);
        if (txtNapis != null) txtNapis.gameObject.SetActive(false);
    }

    public IEnumerator ForceJumpscare()
    {
        bool was = busy;
        var prev = stav;
        busy = true;
        yield return Jumpscare();
        busy = was;
        stav = prev;
    }

    IEnumerator Jumpscare()
    {
        string content = KostkaSkin.JumpOfCurrent();
        if (content == "miku" && canvas != null)
        {
            var tex = KostkaSkin.FaceTex("SkinFaces/MikuFace");
            if (tex != null)
            {
                yield return JumpscarePavouk.HrajObrazek(canvas, tex, KostkaSkin.JumpscareClip("miku"));
                yield break;
            }
        }
        if (content == "astolfo" && canvas != null)
        {
            var tex = KostkaSkin.FaceTex("SkinFaces/AstolfoJump") ?? KostkaSkin.FaceTex("SkinFaces/AstolfoFace");
            if (tex != null)
            {
                yield return JumpscarePavouk.HrajObrazek(canvas, tex, KostkaSkin.JumpscareClip("astolfo"));
                yield break;
            }
        }
        if (content == "67" && canvas != null)
        {
            yield return JumpscarePavouk.Hraj67(canvas, KostkaSkin.JumpscareClip("67"));
            yield break;
        }
        if (jumpscareStyl == StylVideo && pavoukVideo != null && pavoukVideo.Length > 0 && canvas != null)
            yield return JumpscarePavouk.HrajVideo(canvas, pavoukVideo, pavoukVideoZvuk, 20f);
        else if (jumpscareStyl == StylAnim && pavoukAnim != null && pavoukAnim.Length > 0 && canvas != null)
            yield return JumpscarePavouk.HrajAnimovany(canvas, pavoukAnim, 1.25f);
        else if (jumpscareStyl == StylKlasik && pavoukFoto != null && canvas != null)
            yield return JumpscarePavouk.HrajKlasik(canvas, pavoukFoto, 1.05f);
        else
            yield return JumpscarePavouk.Hraj(Camera.main, 1.05f);
    }

    void KonecHry()
    {
        stav = Stav.Konec;
        UlozRekord();
        if (aktivni != null) aktivni.Hide();
        if (duch != null) duch.Hide();
        if (konecPanel != null)
        {
            konecPanel.gameObject.SetActive(true);
            if (txtKonecSkore != null) txtKonecSkore.text = "Skóre: " + skore + "\nRekord: " + rekord;
        }
        if (SoundFX.I != null) SoundFX.I.Over();
        ObnovHud();
    }

    void UlozRekord()
    {
        if (skore > rekord)
        {
            rekord = skore;
            PlayerPrefs.SetInt(PrefRekord, rekord);
            PlayerPrefs.Save();
        }
    }

    void ObsluzPohyb()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            return;
        var cam = CameraOrbit.I;
        Vector3Int fwd = cam != null ? cam.ForwardAxis : new Vector3Int(0, 0, -1);
        Vector3Int right = cam != null ? cam.RightAxis : new Vector3Int(1, 0, 0);

        int h = 0, v = 0;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h++;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h--;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v++;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v--;
        Vector3Int dir = right * h + fwd * v;

        bool down = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) ||
                    Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ||
                    Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) ||
                    Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

        if (dir == Vector3Int.zero)
        {
            das = 0f;
            dasDir = Vector3Int.zero;
            return;
        }
        if (down || dir != dasDir)
        {
            dasDir = dir;
            das = 0.20f;
            if (ZkusPosun(dir) && SoundFX.I != null) SoundFX.I.Move();
            return;
        }
        das -= Time.deltaTime;
        if (das <= 0f)
        {
            das = 0.065f;
            if (ZkusPosun(dir) && SoundFX.I != null) SoundFX.I.Move();
        }
    }

    void ObsluzRotaci()
    {
        bool ok = false;
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var cam = CameraOrbit.I;
        Vector3Int fwd = cam != null ? cam.ForwardAxis : new Vector3Int(0, 0, -1);
        Vector3Int right = cam != null ? cam.RightAxis : new Vector3Int(1, 0, 0);

        // Hlavní schéma: Q/E svislá osa, Shift+WASD překlopení vůči kameře.
        if (Input.GetKeyDown(KeyCode.Q)) ok = ZkusRotaci(1, -1);
        else if (Input.GetKeyDown(KeyCode.E)) ok = ZkusRotaci(1, 1);
        else if (shift && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            ok = FlipKolem(right, -1);
        else if (shift && (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
            ok = FlipKolem(right, 1);
        else if (shift && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)))
            ok = FlipKolem(fwd, -1);
        else if (shift && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)))
            ok = FlipKolem(fwd, 1);
        // Staré klávesy jako alternativa (světové osy).
        else if (Input.GetKeyDown(KeyCode.R)) ok = ZkusRotaci(0, 1);
        else if (Input.GetKeyDown(KeyCode.F)) ok = ZkusRotaci(0, -1);
        else if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Y)) ok = ZkusRotaci(2, 1);
        else if (Input.GetKeyDown(KeyCode.X)) ok = ZkusRotaci(2, -1);
        if (ok && SoundFX.I != null) SoundFX.I.Rotate();
    }

    bool FlipKolem(Vector3Int osaVec, int smer)
    {
        int osa = osaVec.x != 0 ? 0 : 2;
        int zn = (osaVec.x + osaVec.z) >= 0 ? 1 : -1;
        return ZkusRotaci(osa, zn * smer);
    }

    public void PredplnDemo()
    {
        var rng = new System.Random(3);
        for (int y = 0; y < 5; y++)
        for (int x = 0; x < Board.W; x++)
        for (int z = 0; z < Board.D; z++)
        {
            if (x == 3 && z == 3) continue;
            if ((x + z + y) % 7 == 0) continue;
            if (rng.NextDouble() > 0.78) continue;
            int sid = rng.Next(PieceShapes.All.Length);
            var p = new Vector3Int(x, y, z);
            board.Set(p, sid + 1);
            var go = NovaBunka(leziciRoot, p, sid, "Demo");
            lezici[p] = go.transform;
        }
        foreach (var r in board.NajdiPlneRadky())
        {
            var dira = r.podelX ? new Vector3Int(3, r.y, r.pevna) : new Vector3Int(r.pevna, r.y, 3);
            board.Set(dira, 0);
            if (lezici.TryGetValue(dira, out var tr) && tr != null) Destroy(tr.gameObject);
            lezici.Remove(dira);
        }
    }

    void PostavDekoraci()
    {
        SmazDekoraci();
        var rng = new System.Random(11);
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < Board.W; x++)
        for (int z = 0; z < Board.D; z++)
        {
            if (x == 3 || z == 3) continue;
            if (rng.NextDouble() > 0.55) continue;
            int sid = (x * 3 + z * 5 + y) % PieceShapes.All.Length;
            var go = NovaBunka(dekorRoot, new Vector3Int(x, y, z), sid, "Dekor");
            dekor.Add(go);
        }
    }

    void SmazDekoraci()
    {
        for (int i = 0; i < dekor.Count; i++)
            if (dekor[i] != null) Destroy(dekor[i]);
        dekor.Clear();
    }

    void SmazLezici()
    {
        foreach (var kv in lezici)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        lezici.Clear();
        for (int i = 0; i < leziciFigury.Count; i++)
            if (leziciFigury[i] != null && leziciFigury[i].go != null)
                Destroy(leziciFigury[i].go);
        leziciFigury.Clear();
    }

    void PostavSvet()
    {
        svet = new GameObject("Svet").transform;
        svet.SetParent(transform, false);
        leziciRoot = new GameObject("Lezici").transform;
        leziciRoot.SetParent(svet, false);
        dekorRoot = new GameObject("Dekor").transform;
        dekorRoot.SetParent(svet, false);

        // Země
        var zem = GameObject.CreatePrimitive(PrimitiveType.Plane);
        zem.name = "Zeme";
        zem.transform.SetParent(svet, false);
        zem.transform.position = new Vector3(0f, -0.01f, 0f);
        zem.transform.localScale = new Vector3(6f, 1f, 6f);
        zem.GetComponent<MeshRenderer>().sharedMaterial = KostkaVisual.GroundMat();
        Object.Destroy(zem.GetComponent<Collider>());

        // Podlaha šachty
        var pod = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pod.name = "Podlaha";
        pod.transform.SetParent(svet, false);
        pod.transform.position = new Vector3(0f, -0.04f, 0f);
        pod.transform.localScale = new Vector3(7.05f, 0.08f, 7.05f);
        pod.GetComponent<MeshRenderer>().sharedMaterial = KostkaVisual.FloorMat();
        Object.Destroy(pod.GetComponent<Collider>());

        // Rohové sloupky
        float[] xs = { -3.5f, 3.5f };
        for (int i = 0; i < 2; i++)
        for (int j = 0; j < 2; j++)
        {
            var sl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sl.name = "Sloupek";
            sl.transform.SetParent(svet, false);
            sl.transform.position = new Vector3(xs[i], 7.5f, xs[j]);
            sl.transform.localScale = new Vector3(0.10f, 15.1f, 0.10f);
            sl.GetComponent<MeshRenderer>().sharedMaterial = KostkaVisual.PostMat();
            Object.Destroy(sl.GetComponent<Collider>());
        }

        // Jemné stěny
        void Stena(string n, Vector3 pos, Vector3 sc)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Quad);
            w.name = n;
            w.transform.SetParent(svet, false);
            w.transform.position = pos;
            w.transform.localScale = sc;
            w.GetComponent<MeshRenderer>().sharedMaterial = KostkaVisual.WallMat();
            Object.Destroy(w.GetComponent<Collider>());
        }
        Stena("StenaZ+", new Vector3(0, 7.5f, 3.5f), new Vector3(7f, 15f, 1f));
        GameObject.Find("StenaZ+").transform.rotation = Quaternion.Euler(0, 180, 0);
        Stena("StenaZ-", new Vector3(0, 7.5f, -3.5f), new Vector3(7f, 15f, 1f));
        Stena("StenaX+", new Vector3(3.5f, 7.5f, 0), new Vector3(7f, 15f, 1f));
        GameObject.Find("StenaX+").transform.rotation = Quaternion.Euler(0, -90, 0);
        Stena("StenaX-", new Vector3(-3.5f, 7.5f, 0), new Vector3(7f, 15f, 1f));
        GameObject.Find("StenaX-").transform.rotation = Quaternion.Euler(0, 90, 0);

        var grid = new GameObject("Mrizka");
        grid.transform.SetParent(svet, false);
        var gmf = grid.AddComponent<MeshFilter>();
        gmf.sharedMesh = KostkaVisual.BuildWellGrid();
        var gmr = grid.AddComponent<MeshRenderer>();
        gmr.sharedMaterial = KostkaVisual.GridMat();
        gmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Obloha – velká koule s gradientem
        var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sky.name = "Obloha";
        sky.transform.SetParent(svet, false);
        sky.transform.localScale = Vector3.one * 90f;
        var smr = sky.GetComponent<MeshRenderer>();
        smr.sharedMaterial = KostkaVisual.SkyMat();
        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smr.receiveShadows = false;
        Object.Destroy(sky.GetComponent<Collider>());
        // otočit normály: koule je zevnitř, takže škálujeme záporně
        sky.transform.localScale = new Vector3(-90f, 90f, 90f);

        PostavNahledKameru();
    }

    void PostavNahledKameru()
    {
        var root = new GameObject("NahledSvet");
        root.transform.position = new Vector3(80f, 0f, 80f);
        nahled = new PieceView("NahledKus", root.transform, false);

        var camGo = new GameObject("NahledKamera");
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 2.2f, -5.2f);
        camGo.transform.LookAt(root.transform.position + Vector3.up * 0.4f);
        nahledCam = camGo.AddComponent<Camera>();
        nahledCam.clearFlags = CameraClearFlags.SolidColor;
        nahledCam.backgroundColor = new Color(0.99f, 0.95f, 0.91f, 1f);
        nahledCam.fieldOfView = 32f;
        nahledCam.nearClipPlane = 0.1f;
        nahledCam.farClipPlane = 20f;
        nahledCam.depth = -2;
        nahledCam.enabled = true;
        var camData = nahledCam.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.renderPostProcessing = false;
            camData.renderShadows = false;
            camData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
        }
        int layer = LayerMask.NameToLayer("Nahled");
        if (layer < 0) layer = 8;
        SetLayer(root, layer);
        nahledCam.cullingMask = 1 << layer;
        if (Camera.main != null)
            Camera.main.cullingMask &= ~(1 << layer);

        var svetlo = new GameObject("NahledSvetlo").AddComponent<Light>();
        svetlo.transform.SetParent(root.transform, false);
        svetlo.transform.localRotation = Quaternion.Euler(40f, -30f, 0f);
        svetlo.type = LightType.Directional;
        svetlo.color = new Color(1f, 0.95f, 0.9f);
        svetlo.intensity = 1.1f;
        svetlo.cullingMask = 1 << layer;
        svetlo.shadows = LightShadows.None;

        nahledRt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        nahledRt.name = "NahledRT";
        nahledRt.antiAliasing = 2;
        nahledCam.targetTexture = nahledRt;
        if (nahledImg != null) nahledImg.texture = nahledRt;
    }

    static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        var tr = go.transform;
        for (int i = 0; i < tr.childCount; i++) SetLayer(tr.GetChild(i).gameObject, layer);
    }

    void AktualizujNahled()
    {
        if (nahled == null) return;
        var def = PieceShapes.All[dalsiTvar];
        nahled.SetLocal(def.bunky, KostkaSkin.ColorOf(dalsiTvar), dalsiTvar);
        nahled.root.localPosition = Vector3.zero;
        nahled.root.localRotation = Quaternion.Euler(18f, 28f, 0f);
        int layer = LayerMask.NameToLayer("Nahled");
        if (layer < 0) layer = 8;
        SetLayer(nahled.root.gameObject, layer);
        if (nahledImg != null) nahledImg.texture = nahledRt;
        if (nahledCam != null)
        {
            nahledCam.enabled = true;
            nahledCam.Render();
        }
    }


    GameObject NovaBunka(Transform parent, Vector3Int p, int sid, string name)
    {
        var go = KostkaVisual.MakeCube(parent, KostkaVisual.World(p), KostkaSkin.Material(sid, false), name);
        var b = go.AddComponent<KostkaBunka>();
        b.tvar = sid;
        b.ghost = false;
        return go;
    }

    public void ObnovFigury()
    {
        KostkaSkin.RefreshRenderers();
        if (aktivni != null && lokal != null && stav != Stav.Titulka && stav != Stav.Skiny)
            aktivni.SetSmooth(vizPuvod, lokal, BarvaAktivni, false, tvar);
        if (duch != null && lokal != null && stav != Stav.Titulka && stav != Stav.Skiny)
            AktualizujDucha();
        if (nahled != null && stav == Stav.Hra) AktualizujNahled();
        // dekor na titulce
        if (stav == Stav.Titulka || stav == Stav.Skiny)
        {
            foreach (var go in dekor)
            {
                if (go == null) continue;
                var b = go.GetComponent<KostkaBunka>();
                var mr = go.GetComponent<MeshRenderer>();
                if (b != null && mr != null) mr.sharedMaterial = KostkaSkin.Material(b.tvar, false);
            }
        }
    }

    public void NavratNaTitulku() => DemoNavratNaTitulku();

    public void DemoNavratNaTitulku()
    {
        StopAllCoroutines();
        busy = false;
        Time.timeScale = 1f;
        SmazLezici();
        board.Reset();
        if (aktivni != null) aktivni.Hide();
        if (duch != null) duch.Hide();
        if (hud != null) hud.gameObject.SetActive(false);
        if (konecPanel != null) konecPanel.gameObject.SetActive(false);
        if (pauzaPanel != null) pauzaPanel.gameObject.SetActive(false);
        if (skinyPanel != null) skinyPanel.gameObject.SetActive(false);
        stav = Stav.Titulka;
        if (titulka != null) titulka.gameObject.SetActive(true);
        if (CameraOrbit.I != null) CameraOrbit.I.autoOrbit = true;
        PostavDekoraci();
        ObnovHud();
    }

    public void OpenSkinMenu()
    {
        if (stav != Stav.Titulka && stav != Stav.Skiny) return;
        stav = Stav.Skiny;
        skinKurzor = KostkaSkin.IndexOf(KostkaSkin.Current().id);
        skinKod = "";
        if (titulka != null) titulka.gameObject.SetActive(false);
        if (skinyPanel != null) skinyPanel.gameObject.SetActive(true);
        RefreshSkinMenu("Šipky nebo klik = výběr, Enter = zvolit, Esc = zpět");
    }

    public void CloseSkinMenu()
    {
        if (stav != Stav.Skiny) return;
        stav = Stav.Titulka;
        if (skinyPanel != null) skinyPanel.gameObject.SetActive(false);
        if (titulka != null) titulka.gameObject.SetActive(true);
        ObnovFigury();
    }

    public bool TryUnlockCode(string code)
    {
        skinKod = code ?? "";
        return SubmitSkinCode();
    }

    public bool SubmitSkinCode()
    {
        string typed = (skinKod ?? "").Trim();
        if (string.Equals(typed, "DEV", System.StringComparison.OrdinalIgnoreCase))
        {
            KostkaSkin.UnlockDev();
            skinKod = "";
            RefreshSkinMenu("Odemčen panel DEV / modifikace");
            return true;
        }
        var unlocked = KostkaSkin.UnlockWithCode(skinKod);
        if (unlocked == null)
        {
            RefreshSkinMenu("Neplatný kód");
            return false;
        }
        KostkaSkin.Select(unlocked.id);
        skinKurzor = KostkaSkin.IndexOf(unlocked.id);
        skinKod = "";
        ObnovFigury();
        RefreshSkinMenu("Odemčeno: " + unlocked.displayName + "!");
        return true;
    }

    public bool SelectSkinId(string id)
    {
        bool ok = KostkaSkin.Select(id);
        if (ok)
        {
            skinKurzor = KostkaSkin.IndexOf(id);
            ObnovFigury();
        }
        return ok;
    }

    // Pro screenshoty: nastaví aktuální tvar (třeba T / Miku T-póza).
    public void DemoNastavTvar(int index)
    {
        if (PieceShapes.All.Length == 0) return;
        tvar = ((index % PieceShapes.All.Length) + PieceShapes.All.Length) % PieceShapes.All.Length;
        lokal = PieceShapes.Copy(PieceShapes.All[tvar].bunky);
        int maxY = 0;
        for (int i = 0; i < lokal.Length; i++) if (lokal[i].y > maxY) maxY = lokal[i].y;
        puvod = new Vector3Int(Board.W / 2, 5, Board.D / 2);
        if (!board.CanPlace(puvod, lokal))
            puvod = new Vector3Int(Board.W / 2, 8, Board.D / 2);
        vizPuvod = puvod;
        busy = true; // ať figurka při screenshotu nespadne
        padani = 0f;
        if (aktivni == null) aktivni = new PieceView("Aktivni", svet, false);
        if (duch == null) duch = new PieceView("Duch", svet, true);
        aktivni.Set(puvod, lokal, BarvaAktivni, false, tvar);
        AktualizujDucha();
        AktualizujNahled();
    }

    void SkinMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { CloseSkinMenu(); return; }
        int n = KostkaSkin.All.Length;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        { skinKurzor = (skinKurzor + n - 1) % n; RefreshSkinMenu(); }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        { skinKurzor = (skinKurzor + 1) % n; RefreshSkinMenu(); }
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        { skinKurzor = (skinKurzor + n - 4) % n; RefreshSkinMenu(); }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        { skinKurzor = (skinKurzor + 4) % n; RefreshSkinMenu(); }
        foreach (char c in Input.inputString)
        {
            if (c == '\b') { if (skinKod.Length > 0) skinKod = skinKod.Substring(0, skinKod.Length - 1); }
            else if (c == '\n' || c == '\r')
            {
                if (skinKod.Length > 0) SubmitSkinCode();
                else TrySelectHighlighted();
                return;
            }
            else if (!char.IsControl(c) && skinKod.Length < 16) skinKod += c;
            RefreshSkinMenu();
        }
    }

    void TrySelectHighlighted()
    {
        var s = KostkaSkin.All[Mathf.Clamp(skinKurzor, 0, KostkaSkin.All.Length - 1)];
        if (!KostkaSkin.IsUnlocked(s))
        {
            RefreshSkinMenu("Tenhle skin je zamčený – zadej tajný kód");
            return;
        }
        KostkaSkin.Select(s.id);
        ObnovFigury();
        RefreshSkinMenu("Vybráno: " + s.displayName);
    }

    void RefreshSkinMenu(string hint = null)
    {
        if (txtSkinyKod != null)
            txtSkinyKod.text = "Tajný kód: " + (string.IsNullOrEmpty(skinKod) ? "_" : skinKod + "_");
        if (txtSkinyHint != null && hint != null) txtSkinyHint.text = hint;
        if (skinySloty == null) return;
        var cur = KostkaSkin.Current();
        for (int i = 0; i < skinySloty.childCount && i < KostkaSkin.All.Length; i++)
        {
            var s = KostkaSkin.All[i];
            bool unlocked = KostkaSkin.IsUnlocked(s);
            bool on = i == skinKurzor;
            bool used = cur != null && cur.id == s.id;
            var slot = skinySloty.GetChild(i);
            var img = slot.GetComponent<Image>();
            var label = slot.GetComponentInChildren<TextMeshProUGUI>();
            Color baseC = unlocked ? s.swatch : new Color(0.75f, 0.70f, 0.66f);
            if (img)
            {
                Color bg = on ? Color.Lerp(baseC, Color.white, 0.35f) : Color.Lerp(baseC, new Color(1f, 0.97f, 0.94f), 0.55f);
                if (!unlocked) bg = new Color(0.80f, 0.76f, 0.72f, 0.95f);
                img.color = bg;
            }
            if (label)
            {
                string name = unlocked ? s.displayName : "???";
                if (!unlocked) name += "\n<size=70%>zámek</size>";
                else if (used) name += "\n<size=70%>VYBRÁNO</size>";
                label.text = name;
                label.color = unlocked ? new Color(0.32f, 0.20f, 0.16f) : new Color(0.45f, 0.38f, 0.34f);
            }
        }
        if (devPanel != null)
        {
            devPanel.gameObject.SetActive(KostkaSkin.DevUnlocked);
            if (KostkaSkin.DevUnlocked)
            {
                string sid = cur != null ? cur.id : "pastel";
                string sfx = KostkaSkin.SfxOf(sid);
                string jmp = KostkaSkin.JumpOf(sid);
                for (int i = 0; i < devPanel.childCount; i++)
                {
                    var ch = devPanel.GetChild(i);
                    var img = ch.GetComponent<Image>();
                    if (img == null) continue;
                    if (ch.name.StartsWith("DevSfx_"))
                    {
                        bool on = ch.name == "DevSfx_" + sfx;
                        img.color = on ? new Color(0.90f, 0.48f, 0.36f) : new Color(0.92f, 0.78f, 0.70f);
                    }
                    if (ch.name.StartsWith("DevJump_"))
                    {
                        bool on = ch.name == "DevJump_" + jmp;
                        img.color = on ? new Color(0.90f, 0.48f, 0.36f) : new Color(0.92f, 0.78f, 0.70f);
                    }
                }
            }
        }
    }

    // ---------- UI ----------
    // ---------- UI ----------
    void PostavUi()
    {
        var canvGo = new GameObject("Canvas");
        canvas = canvGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvGo.AddComponent<GraphicRaycaster>();

        var dimSprite = KostkaVisual.RoundSprite();
        Color ink = new Color(0.38f, 0.26f, 0.22f);
        Color cream = new Color(1f, 0.97f, 0.94f, 0.94f);
        Color coral = new Color(0.90f, 0.48f, 0.36f);
        Color blush = new Color(0.98f, 0.90f, 0.84f, 0.92f);

        // TITULKA
        titulka = Panel("Titulka", canvGo.transform, Vector2.zero, new Vector2(1920, 1080), new Color(0.95f, 0.86f, 0.78f, 0.28f), null);
        Stretch(titulka);
        var karta = Panel("Karta", titulka, Vector2.zero, new Vector2(760, 840), cream, dimSprite);
        Txt("Nazev", karta, new Vector2(0, 290), new Vector2(700, 140), "KOSTKA", 120, fontTitle, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        Txt("Podtitul", karta, new Vector2(0, 195), new Vector2(700, 40), "3D skládačka", 34, fontUi, new Color(0.55f, 0.38f, 0.32f), FontStyles.Normal, TextAlignmentOptions.Center);
        txtTitulRekord = Txt("Rekord", karta, new Vector2(0, 145), new Vector2(700, 36), "Rekord: " + rekord, 28, fontUi, new Color(0.50f, 0.35f, 0.28f), FontStyles.Normal, TextAlignmentOptions.Center);

        var btnHrat = Tlacitko("Hrat", karta, new Vector2(0, 60), new Vector2(280, 74), "HRÁT", coral, Color.white, 42, () => StartGame(false));
        Tlacitko("Skiny", karta, new Vector2(0, -30), new Vector2(280, 70), "SKINY", new Color(0.92f, 0.62f, 0.50f), Color.white, 36, OpenSkinMenu);

        Txt("Ovl", karta, new Vector2(0, -230), new Vector2(700, 300),
            "Pohyb        WASD   ·   Otočení   Q / E\n" +
            "Překlopit    Shift + WASD\n" +
            "Pád          Mezerník   ·   Kamera  1 / 2  nebo myš\n" +
            "Pauza        P\n" +
            "Cíl: skládej řady 7 kostek vedle sebe.",
            22, fontUi, new Color(0.42f, 0.30f, 0.26f), FontStyles.Normal, TextAlignmentOptions.Center);

        // HUD
        hud = Panel("HUD", canvGo.transform, Vector2.zero, new Vector2(1920, 1080), Color.clear, null);
        Stretch(hud);
        hud.gameObject.SetActive(false);
        var hudLevy = Panel("Levy", hud, new Vector2(40, -40), new Vector2(360, 280), blush, dimSprite);
        SetAnchor(hudLevy, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        hudLevy.anchoredPosition = new Vector2(200, -180);
        txtSkore = Txt("Skore", hudLevy, new Vector2(0, 70), new Vector2(320, 60), "Skóre  0", 34, fontUi, ink, FontStyles.Bold, TextAlignmentOptions.Left);
        txtRekord = Txt("RekordH", hudLevy, new Vector2(0, 20), new Vector2(320, 40), "Rekord  0", 24, fontUi, new Color(0.5f, 0.35f, 0.28f), FontStyles.Normal, TextAlignmentOptions.Left);
        txtLevel = Txt("Level", hudLevy, new Vector2(0, -25), new Vector2(320, 40), "Level  1", 28, fontUi, ink, FontStyles.Normal, TextAlignmentOptions.Left);
        txtRady = Txt("Rady", hudLevy, new Vector2(0, -70), new Vector2(320, 40), "Řady  0", 28, fontUi, ink, FontStyles.Normal, TextAlignmentOptions.Left);

        var tab = Panel("Tabulka", hud, Vector2.zero, new Vector2(440, 360), blush, dimSprite);
        SetAnchor(tab, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
        tab.anchoredPosition = new Vector2(24, 24);
        Txt("TabNadpis", tab, new Vector2(0, 150), new Vector2(400, 36), "OVLÁDÁNÍ", 22, fontUi, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        txtTabulka = Txt("TabText", tab, new Vector2(0, -8), new Vector2(400, 280), TabulkaText(), 19, fontUi, new Color(0.38f, 0.26f, 0.22f), FontStyles.Normal, TextAlignmentOptions.Left);
        ObnovTabulku();

        var hudPravy = Panel("Pravy", hud, Vector2.zero, new Vector2(240, 300), blush, dimSprite);
        SetAnchor(hudPravy, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
        hudPravy.anchoredPosition = new Vector2(-150, -190);
        Txt("DalsiL", hudPravy, new Vector2(0, 120), new Vector2(200, 40), "DALŠÍ", 26, fontUi, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        var rawGo = new GameObject("NahledImg");
        rawGo.transform.SetParent(hudPravy, false);
        nahledImg = rawGo.AddComponent<RawImage>();
        nahledImg.texture = nahledRt;
        nahledImg.color = Color.white;
        var rrt = rawGo.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(180, 180);
        rrt.anchoredPosition = new Vector2(0, -20);

        txtCombo = Txt("Combo", hud, Vector2.zero, new Vector2(700, 200), "", 72, fontTitle, coral, FontStyles.Bold, TextAlignmentOptions.Center);
        txtCombo.gameObject.SetActive(false);

        txtNapis = Txt("Napis", canvGo.transform, new Vector2(0, 280), new Vector2(900, 80), "", 48, fontTitle, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        txtNapis.gameObject.SetActive(false);

        // SKINY
        skinyPanel = Panel("Skiny", canvGo.transform, Vector2.zero, new Vector2(1920, 1080), new Color(0.95f, 0.86f, 0.78f, 0.40f), null);
        Stretch(skinyPanel);
        skinyPanel.gameObject.SetActive(false);
        var skKarta = Panel("SkinyKarta", skinyPanel, Vector2.zero, new Vector2(1180, 1000), cream, dimSprite);
        Txt("SkinyT", skKarta, new Vector2(0, 450), new Vector2(1100, 60), "SKINY", 56, fontTitle, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        var slotGo = new GameObject("Sloty", typeof(RectTransform));
        skinySloty = slotGo.transform;
        skinySloty.SetParent(skKarta, false);
        var slotRt = slotGo.GetComponent<RectTransform>();
        slotRt.anchoredPosition = new Vector2(0, 160);
        slotRt.sizeDelta = new Vector2(1100, 430);
        float sw = 200f, sh = 108f, gapx = 16f, gapy = 16f;
        int cols = 4;
        for (int i = 0; i < KostkaSkin.All.Length; i++)
        {
            int idx = i;
            var s = KostkaSkin.All[i];
            int col = i % cols;
            int row = i / cols;
            float x = (col - (cols - 1) * 0.5f) * (sw + gapx);
            float y = 140f - row * (sh + gapy);
            Tlacitko("Slot_" + s.id, skinySloty, new Vector2(x, y), new Vector2(sw, sh), s.displayName, s.swatch, ink, 24, () =>
            {
                skinKurzor = idx;
                if (KostkaSkin.IsUnlocked(s)) TrySelectHighlighted();
                else RefreshSkinMenu("Tenhle skin je zamčený – zadej tajný kód");
            });
        }
        txtSkinyKod = Txt("Kod", skKarta, new Vector2(0, -90), new Vector2(900, 36), "Tajný kód: _", 26, fontUi, ink, FontStyles.Normal, TextAlignmentOptions.Center);
        txtSkinyHint = Txt("Hint", skKarta, new Vector2(0, -128), new Vector2(1100, 40), "Šipky nebo klik = výběr, Enter = zvolit, Esc = zpět", 20, fontUi, new Color(0.50f, 0.35f, 0.28f), FontStyles.Normal, TextAlignmentOptions.Center);

        devPanel = Panel("DevPanel", skKarta, new Vector2(0, -265), new Vector2(1080, 200), new Color(0.98f, 0.92f, 0.86f, 0.98f), dimSprite);
        Txt("DevT", devPanel, new Vector2(0, 78), new Vector2(1000, 32), "DEV / modifikace  (vybraný skin)", 22, fontUi, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        Txt("DevSfxL", devPanel, new Vector2(-430, 36), new Vector2(180, 28), "Zvuk řady:", 18, fontUi, ink, FontStyles.Normal, TextAlignmentOptions.Right);
        Txt("DevJumpL", devPanel, new Vector2(-430, -28), new Vector2(180, 28), "Jumpscare:", 18, fontUi, ink, FontStyles.Normal, TextAlignmentOptions.Right);
        for (int i = 0; i < KostkaSkin.SfxIds.Length; i++)
        {
            int ii = i;
            string id = KostkaSkin.SfxIds[i];
            Tlacitko("DevSfx_" + id, devPanel, new Vector2(-180 + i * 165, 32), new Vector2(150, 44), KostkaSkin.SfxLabels[i], new Color(0.92f, 0.78f, 0.70f), ink, 20, () =>
            {
                var cur = KostkaSkin.Current();
                if (cur != null) KostkaSkin.SetSfx(cur.id, KostkaSkin.SfxIds[ii]);
                RefreshSkinMenu("Zvuk řady uložen");
            });
        }
        for (int i = 0; i < KostkaSkin.JumpIds.Length; i++)
        {
            int ii = i;
            string id = KostkaSkin.JumpIds[i];
            Tlacitko("DevJump_" + id, devPanel, new Vector2(-180 + i * 165, -32), new Vector2(150, 44), KostkaSkin.JumpLabels[i], new Color(0.92f, 0.78f, 0.70f), ink, 20, () =>
            {
                var cur = KostkaSkin.Current();
                if (cur != null) KostkaSkin.SetJump(cur.id, KostkaSkin.JumpIds[ii]);
                RefreshSkinMenu("Jumpscare uložen");
            });
        }
        devPanel.gameObject.SetActive(false);

        Tlacitko("Zpet", skKarta, new Vector2(0, -430), new Vector2(240, 60), "ZPĚT", coral, Color.white, 30, CloseSkinMenu);
        RefreshSkinMenu();

        // PAUZA
        pauzaPanel = Panel("Pauza", canvGo.transform, Vector2.zero, new Vector2(1920, 1080), new Color(0.95f, 0.88f, 0.80f, 0.45f), null);
        Stretch(pauzaPanel);
        pauzaPanel.gameObject.SetActive(false);
        var pauzaKarta = Panel("PauzaKarta", pauzaPanel, Vector2.zero, new Vector2(520, 320), cream, dimSprite);
        Txt("PauzaT", pauzaKarta, new Vector2(0, 70), new Vector2(480, 80), "PAUZA", 72, fontTitle, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        Tlacitko("Pokracovat", pauzaKarta, new Vector2(0, -50), new Vector2(280, 70), "POKRAČOVAT", coral, Color.white, 32, PrepnoutPauzu);

        // KONEC
        konecPanel = Panel("Konec", canvGo.transform, Vector2.zero, new Vector2(1920, 1080), new Color(0.95f, 0.88f, 0.80f, 0.45f), null);
        Stretch(konecPanel);
        konecPanel.gameObject.SetActive(false);
        var konecKarta = Panel("KonecKarta", konecPanel, Vector2.zero, new Vector2(700, 440), cream, dimSprite);
        Txt("KonecT", konecKarta, new Vector2(0, 140), new Vector2(640, 80), "KONEC HRY", 64, fontTitle, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        txtKonecSkore = Txt("KonecS", konecKarta, new Vector2(0, 40), new Vector2(640, 80), "Skóre: 0", 32, fontUi, ink, FontStyles.Normal, TextAlignmentOptions.Center);
        Tlacitko("Znovu", konecKarta, new Vector2(-160, -120), new Vector2(240, 70), "ZNOVU", coral, Color.white, 32, () => StartGame(false));
        Tlacitko("Menu", konecKarta, new Vector2(160, -120), new Vector2(240, 70), "MENU", new Color(0.55f, 0.42f, 0.36f), Color.white, 32, NavratNaTitulku);

        var stinGo = new GameObject("JumpscareStin");
        stinGo.transform.SetParent(canvGo.transform, false);
        jumpscareStin = stinGo.AddComponent<Image>();
        jumpscareStin.color = new Color(0.15f, 0.02f, 0.02f, 0.40f);
        Stretch(stinGo.GetComponent<RectTransform>());
        stinGo.SetActive(false);

        _ = btnHrat;
    }

    string TabulkaText()
    {
        return
            "WASD          pohyb\n" +
            "Q / E         otočit (svisle)\n" +
            "Shift + W/S   překlopit vpřed/vzad\n" +
            "Shift + A/D   překlopit do strany\n" +
            "Mezerník      pád\n" +
            "Ctrl          pomalý pád\n" +
            "1 / 2         kamera o 90°\n" +
            "Myš (pravé)   otáčení kamery\n" +
            "P             pauza";
    }

    void ObnovTabulku()
    {
        if (txtTabulka != null) txtTabulka.text = TabulkaText();
    }

    void ObnovHud()
    {
        if (txtSkore != null) txtSkore.text = "Skóre  " + skore;
        if (txtRekord != null) txtRekord.text = "Rekord  " + rekord;
        if (txtLevel != null) txtLevel.text = "Level  " + level;
        if (txtRady != null) txtRady.text = "Řady  " + rady;
        if (txtTitulRekord != null) txtTitulRekord.text = "Rekord: " + rekord;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
    }

    RectTransform Panel(string name, Transform parent, Vector2 pos, Vector2 size, Color col, Sprite sp)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        if (sp != null)
        {
            img.sprite = sp;
            img.type = Image.Type.Sliced;
        }
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    TextMeshProUGUI Txt(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, TMP_FontAsset font, Color col, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = font != null ? font : fontUi;
        t.text = text;
        t.fontSize = fontSize;
        t.color = col;
        t.fontStyle = style;
        t.alignment = align;
        t.enableWordWrapping = true;
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return t;
    }

    Button Tlacitko(string name, Transform parent, Vector2 pos, Vector2 size, string label, Color bg, Color fg, float fontSize, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = KostkaVisual.RoundSprite();
        img.type = Image.Type.Sliced;
        img.color = bg;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
        cb.pressedColor = Color.Lerp(bg, Color.black, 0.12f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Txt("Label", go.transform, Vector2.zero, size, label, fontSize, fontTitle != null ? fontTitle : fontUi, fg, FontStyles.Bold, TextAlignmentOptions.Center);
        return btn;
    }
}
