## Preamble
The current selection system returns only the selected object and if requested, the hitscan coordinates and ray-index. Most selection tags do not report subitems of selections., like

## The Selection Retention
The selections are not retained by the game when objects are changed. If implemented, left clicking an object will show its menu, like this:
```
╔════════════════════╗
║Section 123    p _ x║
╟────────────────────╢
║AADT: 5000/d        ║
║TIdle: 40%          ║
║~Speed: 30kph       ║
║SHealth: 98%        ║
╟────────────────────╢
║Edit markings       ║
║Edit contents       ║
║Edit topology       ║
║Edit geometry       ║
║Edit traffic lights ║
║Edit crossings      ║
║Edit priorities     ║
║Edit speeds         ║
║Edit roadside access║
╟────────────────────╢
║More... >           ║
║Delete              ║
║About this menu     ║
║Help                ║
║Bug report          ║
╚════════════════════╝
```

The menu will be different for each object. Example for a home:
```
╔════════════════════╗
║Church Ave 123 p _ x║
╟────────────────────╢
║4 residents         ║
║Wealth: 500 000$    ║
║Power: 2kW          ║
║Water: 100L/d       ║
║Heat: 5kW           ║
║Income: 4000$/mo    ║
║AADT: 6/d           ║
║Taxes: +6000$/y     ║
║Sewage: 99L/d       ║
║SHealth: 88%        ║
╟────────────────────╢
║More... >           ║
║Delete              ║
║About this menu     ║
║Help                ║
║Bug report          ║
╚════════════════════╝
```