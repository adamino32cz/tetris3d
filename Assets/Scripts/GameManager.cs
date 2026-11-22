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

    public enum Stav { Titulka, Hra, Pauza, Konec, Mazani }

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
    const string PrefJumpscareStyl = "Kostka.JumpscareStyl";
    public const int StylKlasik = 1;
    public const int Styl3D = 0;
    const int RadNaLevel = 8;

    int skore, rekord, level = 1, rady;
    public bool jumpscareZapnuto = true;
    public int jumpscareStyl = StylKlasik;
    Texture2D pavoukFoto;
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
    readonly List<GameObject> dekor = new List<GameObject>();

    PieceView aktivni, duch, nahled;
    Transform svet, leziciRoot, dekorRoot;
    Camera nahledCam;
    RenderTexture nahledRt;

    Canvas canvas;
    RectTransform titulka, hud, pauzaPanel, konecPanel;
    TextMeshProUGUI txtSkore, txtRekord, txtLevel, txtRady, txtKonecSkore, txtTitulRekord, txtCombo, txtTabulka, txtNapis;
    Image jumpscareStin;
    RawImage nahledImg;

    public Color BarvaAktivni => PieceShapes.All[tvar].barva;

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.names.Length - 1), true);
        rekord = PlayerPrefs.GetInt(PrefRekord, 0);
        jumpscareZapnuto = PlayerPrefs.GetInt(PrefJumpscare, 1) != 0;
        jumpscareStyl = PlayerPrefs.GetInt(PrefJumpscareStyl, StylKlasik);
        pavoukFoto = Resources.Load<Texture2D>("PavoukKlasik");
        KostkaVisual.SetTemplates(litTemplate, unlitTemplate);
        KostkaVisual.Init();
        ZajistiPisma();
    }

    void Start()
    {
        PostavUi();
        PostavSvet();
        if (CameraOrbit.I != null) CameraOrbit.I.autoOrbit = true;
        PostavDekoraci();
        ObnovHud();
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
        if (Input.GetKeyDown(KeyCode.J))
            PrepnoutJumpscare();
        if (Input.GetKeyDown(KeyCode.K))
            PrepnoutStyl();

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
        aktivni.Set(puvod, lokal, BarvaAktivni, false);
        // posun out-of-grid interpolace: přepiš pozice
        var root = aktivni.root;
        int n = Mathf.Min(root.childCount, lokal.Length);
        Vector3 d = vizPuvod - (Vector3)puvod;
        for (int i = 0; i < n; i++)
            root.GetChild(i).position = KostkaVisual.World(puvod + lokal[i]) + new Vector3(d.x, d.y, d.z);
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
        aktivni.Set(puvod, lokal, BarvaAktivni, false);
        AktualizujDucha();
        AktualizujNahled();
    }

    void AktualizujDucha()
    {
        if (duch == null || lokal == null) return;
        var g = puvod;
        while (board.CanPlace(g + Vector3Int.down, lokal)) g += Vector3Int.down;
        duch.Set(g, lokal, BarvaAktivni, true);
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
        if (aktivni != null) aktivni.Set(puvod, lokal, BarvaAktivni, false);
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
                if (aktivni != null) aktivni.Set(puvod, lokal, BarvaAktivni, false);
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

    void Zamknout()
    {
        if (lokal == null) return;
        board.Place(puvod, lokal, tvar + 1);
        var nove = new List<Transform>();
        for (int i = 0; i < lokal.Length; i++)
        {
            var p = puvod + lokal[i];
            if (!board.InBounds(p)) continue;
            var go = KostkaVisual.MakeCube(leziciRoot, KostkaVisual.World(p), KostkaVisual.Lit(BarvaAktivni), "Bunka");
            lezici[p] = go.transform;
            nove.Add(go.transform);
        }
        if (aktivni != null) aktivni.Hide();
        if (duch != null) duch.Hide();
        StartCoroutine(Punch(nove));
        if (SoundFX.I != null) SoundFX.I.Lock();

        var plne = board.NajdiPlneRadky();
        if (plne.Count > 0) StartCoroutine(SmazRadky(plne));
        else Spawn();
        ObnovHud();
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

            float t2 = 0f;
            var fromPos = new Dictionary<Transform, Vector3>();
            var toPos = new Dictionary<Transform, Vector3>();
            foreach (var kv in lezici)
            {
                if (kv.Value == null) continue;
                fromPos[kv.Value] = kv.Value.position;
                toPos[kv.Value] = KostkaVisual.World(kv.Key);
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
        jumpscareStyl = (jumpscareStyl == StylKlasik) ? Styl3D : StylKlasik;
        PlayerPrefs.SetInt(PrefJumpscareStyl, jumpscareStyl);
        PlayerPrefs.Save();
        if (!busy) StartCoroutine(UkázatStylANahled());
    }

    IEnumerator UkázatStylANahled()
    {
        busy = true;
        string txt = jumpscareStyl == StylKlasik ? "Pavouk: klasik" : "Pavouk: 3D";
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
        if (jumpscareStyl == StylKlasik && pavoukFoto != null && canvas != null)
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
            var go = KostkaVisual.MakeCube(leziciRoot, KostkaVisual.World(p), KostkaVisual.Lit(PieceShapes.All[sid].barva), "Demo");
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
            var go = KostkaVisual.MakeCube(dekorRoot, KostkaVisual.World(new Vector3Int(x, y, z)), KostkaVisual.Lit(PieceShapes.All[sid].barva), "Dekor");
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
        nahled.SetLocal(def.bunky, def.barva);
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
        var karta = Panel("Karta", titulka, Vector2.zero, new Vector2(760, 780), cream, dimSprite);
        Txt("Nazev", karta, new Vector2(0, 250), new Vector2(700, 160), "KOSTKA", 128, fontTitle, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        Txt("Podtitul", karta, new Vector2(0, 145), new Vector2(700, 50), "3D skládačka", 36, fontUi, new Color(0.55f, 0.38f, 0.32f), FontStyles.Normal, TextAlignmentOptions.Center);
        txtTitulRekord = Txt("Rekord", karta, new Vector2(0, 90), new Vector2(700, 40), "Rekord: " + rekord, 28, fontUi, new Color(0.50f, 0.35f, 0.28f), FontStyles.Normal, TextAlignmentOptions.Center);

        var btnHrat = Tlacitko("Hrat", karta, new Vector2(0, 10), new Vector2(280, 80), "HRÁT", coral, Color.white, 44, () => StartGame(false));

        Txt("Ovl", karta, new Vector2(0, -210), new Vector2(700, 320),
            "Pohyb        WASD   ·   Otočení   Q / E\n" +
            "Překlopit    Shift + WASD\n" +
            "Pád          Mezerník   ·   Kamera  1 / 2  nebo myš\n" +
            "Pauza        P\n" +
            "Cíl: skládej řady 7 kostek vedle sebe.",
            24, fontUi, new Color(0.42f, 0.30f, 0.26f), FontStyles.Normal, TextAlignmentOptions.Center);

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
        var konecKarta = Panel("KonecKarta", konecPanel, Vector2.zero, new Vector2(620, 420), cream, dimSprite);
        Txt("KonecT", konecKarta, new Vector2(0, 130), new Vector2(580, 80), "KONEC HRY", 64, fontTitle, ink, FontStyles.Bold, TextAlignmentOptions.Center);
        txtKonecSkore = Txt("KonecS", konecKarta, new Vector2(0, 30), new Vector2(580, 80), "Skóre: 0", 32, fontUi, ink, FontStyles.Normal, TextAlignmentOptions.Center);
        Tlacitko("Znovu", konecKarta, new Vector2(0, -110), new Vector2(280, 70), "ZNOVU", coral, Color.white, 32, () => StartGame(false));

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
