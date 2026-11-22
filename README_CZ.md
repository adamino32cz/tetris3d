# KOSTKA — 3D skládačka

3D Tetris ve stylu klasického Blockoutu: do šachty 7×7×15 padají prostorové kousky, otáčíš je kolem všech tří os a mažeš celá patra.

Hrát v prohlížeči: https://adamino32cz.github.io/tetris3d/

Repozitář: https://github.com/adamino32cz/tetris3d

## Ovládání

| Akce | Klávesy |
|---|---|
| Pohyb (4 strany) | **WASD** nebo **šipky** (relativně ke kameře) |
| Rychlý pád | **Mezerník** |
| Pomalý pád | **Shift** |
| Otáčení kolem os | **Q / E** (svislá), **R / F**, **Z / X** (na české klávesnici jde i **Y** místo Z) |
| Kamera | pravé tlačítko myši, **1 / 2** pootočí o 90° |
| Pauza | **P** |
| Start / znovu | **Enter** nebo **Mezerník** (na titulce a po konci hry) |

Cíl: skládej kousky, zaplňuj celá patra (7×7) a sbírej body. Za víc pater najednou je kombo. Rychlost roste s levelem (každých 8 pater). Rekord se ukládá v prohlížeči / v počítači.

Pomůcky: průhledný stín kusu na místě dopadu, mřížka na stěnách šachty, náhled dalšího kusu.

## Jak otevřít projekt v Unity

Potřebuješ **Unity 2022.3.62f3** (stejná verze jako u hada). Otevři složku projektu `tetris3d`. Scéna je `Assets/Scenes/Kostka.unity`. Pokud scéna chybí, v menu zvol **Tools → Kostka → Vytvořit scénu**.

## Sestavení

Z editoru nebo z příkazové řádky (Unity musí být nainstalované):

```bash
# Linux přehrávač → Build/Linux/Kostka.x86_64
Unity -batchmode -nographics -quit -projectPath . \
  -executeMethod TetrisBatch.BuildSceneAndLinux

# WebGL → Build/WebGL
Unity -batchmode -nographics -quit -projectPath . -buildTarget WebGL \
  -executeMethod TetrisBatch.BuildSceneAndWebGL
```

Hotový WebGL se nahraje na GitHub Pages skriptem `Tools/publish_webgl.sh`. Když už build máš, stačí `SKIP_BUILD=1 Tools/publish_webgl.sh`.

## Poznámky

- Grafika je záměrně jiná než u hada: světlé pastelové kostky, krémové pozadí, měkké stíny.
- Ve hře je 8 tvarů (tyčka, čtverec, elko, téčko, es, roh, šroub, kříž).
- Testovací parametry přehrávače: `-autoplay`, `-screenshots`, `-shotdir <složka>`, `-demo`.
