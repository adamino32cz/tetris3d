# KOSTKA — 3D skládačka

3D Tetris ve stylu klasického Blockoutu: do šachty 7×7×15 padají prostorové kousky, otáčíš je kolem všech tří os a mažeš **řady** (7 kostek vedle sebe v patře, ve směru X nebo Z). Když se zaplní celé patro, dostaneš bonus.

Hrát v prohlížeči: https://adamino32cz.github.io/tetris3d/

Repozitář: https://github.com/adamino32cz/tetris3d

## Ovládání

Tabulka je ve hře pořád vidět v levém dolním rohu.

| Akce | Klávesy |
|---|---|
| Pohyb (4 strany) | **WASD** nebo **šipky** (relativně ke kameře) |
| Otočení kolem svislé osy | **Q / E** |
| Překlopit vpřed / vzad | **Shift + W / S** (vůči kameře) |
| Překlopit do strany | **Shift + A / D** (vůči kameře) |
| Rychlý pád | **Mezerník** |
| Pomalý pád | **Ctrl** |
| Kamera | pravé tlačítko myši, **1 / 2** pootočí o 90° |
| Pauza | **P** |
| Start / znovu | **Enter** nebo **Mezerník** (na titulce a po konci hry) |

Starší klávesy **R / F** a **Z / X** (na české klávesnici i **Y**) pořád otáčejí kolem světových os.

Cíl: skládej kousky a zaplňuj řady o 7 kostkách. Za víc řad najednou je kombo, za celé patro bonus. Rychlost roste s levelem (každých 8 řad). Rekord se ukládá v prohlížeči / v počítači.

Pomůcky: průhledný stín kusu na místě dopadu, mřížka na stěnách šachty, náhled dalšího kusu, tabulka ovládání.

## Jak otevřít projekt v Unity

Potřebuješ **Unity 2022.3.62f3**. Otevři složku projektu `tetris3d`. Scéna je `Assets/Scenes/Kostka.unity`. Pokud scéna chybí, v menu zvol **Tools → Kostka → Vytvořit scénu**.

## Sestavení

```bash
# Linux přehrávač → Build/Linux/Kostka.x86_64
Unity -batchmode -nographics -quit -projectPath . \
  -executeMethod TetrisBatch.BuildSceneAndLinux

# WebGL → Build/WebGL
Unity -batchmode -nographics -quit -projectPath . -buildTarget WebGL \
  -executeMethod TetrisBatch.BuildWebGL
```

Hotový WebGL se nahraje na GitHub Pages skriptem `Tools/publish_webgl.sh`. Když už build máš, stačí `SKIP_BUILD=1 Tools/publish_webgl.sh`.

## Poznámky

- Grafika je záměrně jiná než u hada: světlé pastelové kostky, krémové pozadí, měkké stíny.
- Ve hře je 8 tvarů (tyčka, čtverec, elko, téčko, es, roh, šroub, kříž).
- Testovací parametry přehrávače: `-autoplay`, `-screenshots`, `-shotdir <složka>`, `-demo`, `-jumpscare`, `-skinshots`, `-openskins`, `-skin <id>`.

## Pro učitele / vývojáře

Skryté klávesy (nejsou v tabulce ovládání ve hře):

- **J** — zapne / vypne jumpscare po smazání řady (výchozí: zapnuto, volba se pamatuje).
- **K** — ve hře přepíná styl jumpscaru v cyklu **video → animovaný → fotka → procedurální 3D** a hned přehraje ukázku. Výchozí styl je video. Na úvodní obrazovce **K** otevře menu **SKINY** (stejně jako tlačítko SKINY).

### Zdroj videa pavouka

Soubory `Assets/Resources/PavoukVideo/*.png` a `PavoukVideoZvuk.ogg` vycházejí z klipu:

- **Video:** [SPIDER JUMP SCARE!](https://youtu.be/uV_k8yK92dI) (`https://www.youtube.com/watch?v=uV_k8yK92dI`)
- **Kanál:** Tigran ([@tigrann8287](https://www.youtube.com/@tigrann8287))
- **Kanál URL:** https://www.youtube.com/channel/UC4fmEd2byg3YgWrB1hW_eHQ
- Úpravy ve hře: výřez cca 2,50–4,12 s (skok do kamery, bez statiky na konci), 20 fps, odstraněné pozadí (rembg + luma-key u skoku), vlastní zvuk z klipu.
- Poznámka ke zdroji je i v `Assets/Resources/PavoukVideo_ZDROJ.txt`.

### Zdroj fotografie pavouka

Soubory `Assets/Resources/PavoukKlasik.jpg` a `PavoukAnim0.jpg`–`PavoukAnim3.jpg` vycházejí z fotografie:

- **Soubor:** [Phidippus_regius_female_01.jpg](https://commons.wikimedia.org/wiki/File:Phidippus_regius_female_01.jpg) (Wikimedia Commons)
- **Autor:** [Nosferattus](https://commons.wikimedia.org/wiki/User:Nosferattus)
- **Licence:** [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/) (Public Domain Dedication) — lze upravovat i použít komerčně bez uvedení autora
- Úpravy ve hře: ořez, ztmavení pozadí; u animovaného stylu navíc varianty kontrastu, otočení a zvýraznění očí pro stop-motion pohyb.
- Poznámka ke zdroji je i v `Assets/Resources/PavoukKlasik_ZDROJ.txt` a `PavoukAnim_ZDROJ.txt`.

### Skiny kostek

Na úvodní obrazovce je tlačítko **SKINY** (nebo klávesa **K**; ve hře K dál cykluje styl pavoučího jumpscaru). Jsou tu palety **Pastel** (výchozí), **Neon**, **Černobílá**, **Bonbóny**, **Oceán** a **Les**. Výběr se ukládá do `PlayerPrefs` (`Kostka.Skin`).

V menu SKINY jde napsat tajný kód a potvrdit Enterem (stejně jako u hada):

- **67** — na všech stěnách kostek (i na duchu) běží vlastní animace „6 7“ (poskakující čísla a mávající ruka). Žádný cizí meme GIF: původní animace, viz `Assets/Resources/Skin67_ZDROJ.txt`.
- **MIKU** — na všech stěnách kostek je stylizovaný portrét Miku (původní kresba pro školní hru).
- **MIKU EXTRA** — každá figurka je jedna pózovaná Hatsune Miku, která zhruba kopíruje tvar kusu (téčko = T-póza, tyčka = ležící/natáhlá, čtverec = klubíčko, elko = sed/kopnutí atd.). Přečnívání je záměr. Položené buňky zůstávají kostkami v barvách Miku. Model Tda式初音ミクV4X, viz `Assets/Resources/TdaMiku/TdaMiku_ZDROJ.txt` a `License.txt` (Tda / Crypton Piapro Character License).
- **ASTOLFO** — na všech stěnách kostek je stylizovaný portrét inspirovaný Astolfem (Fate/Apocrypha, Type-Moon); původní kresba, není oficiální artwork.
- **ASTOLFO EXTRA** — každá figurka je jedna pózovaná zjednodušená 3D figurka (růžové vlasy, černá mašle, fialová mikina, černá sukně, bílé podkolenky, jeden cop a velká černá mašle) v póze podle tvaru kusu. Oficiální artwork ani volně redistribuovatelný VRM/FBX se použít nedaly, proto je portrét i model původní, inspirované typickým vzhledem postavy. Viz `Assets/Resources/SkinFaces/SkinFaces_ZDROJ.txt`.
- **DEV** — neodemyká skin, ale skrytý panel **DEV / modifikace** v menu SKINY. Pro právě vybraný skin lze nastavit:
  - zvuk při smazání řady: výchozí akord, **mňau** (CC0, Joseph Sardin / BigSoundBank č. 494), krátké neexplicitní **yamete kudasai** (vlastní TTS espeak-ng; YouTube klip pvF-nQh1Yyc nelze redistribuovat), nebo hláška **67** (vlastní TTS). Dokud panel není odemčený nebo je u skinu „Výchozí“, hraje se původní akord.
  - obsah jumpscaru: výchozí **pavouk** (stávající módy video/anim/fotka/3D, klávesa K ve hře), nebo vyskakovací **Astolfo** / **Miku** / **67** se zvukem.
  Nastavení se ukládá (`Kostka.SkinSfx_*`, `Kostka.SkinJump_*`, `Kostka.DevUnlocked`).

Test: `-skin neon`, `-skin 67`, `-skin miku`, `-skin miku_extra`, `-skin astolfo`, `-skin astolfo_extra`, `-dev`, `-openskins`, `-skinshots`.

Zdroje zvuků: `Assets/Resources/SkinSfx/SkinSfx_ZDROJ.txt`.
