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
- Testovací parametry přehrávače: `-autoplay`, `-screenshots`, `-shotdir <složka>`, `-demo`, `-jumpscare`.

## Pro učitele / vývojáře

Skryté klávesy (nejsou v tabulce ovládání ve hře):

- **J** — zapne / vypne jumpscare po smazání řady (výchozí: zapnuto, volba se pamatuje).
- **K** — přepne styl jumpscaru mezi klasickým (fotka) a procedurálním 3D pavoukem a hned přehraje ukázku. Výchozí styl je klasický fotojumpscare.

### Zdroj fotografie pavouka

Soubor `Assets/Resources/PavoukKlasik.jpg` vychází z fotografie:

- **Soubor:** [Phidippus_regius_female_01.jpg](https://commons.wikimedia.org/wiki/File:Phidippus_regius_female_01.jpg) (Wikimedia Commons)
- **Autor:** [Nosferattus](https://commons.wikimedia.org/wiki/User:Nosferattus)
- **Licence:** [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/) (Public Domain Dedication) — lze upravovat i použít komerčně bez uvedení autora
- Úprava ve hře: ořez a ztmavení pozadí.
