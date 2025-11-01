# Lista ulepszeń dla projektu TranSimCS

    ## 🐛 Błędy krytyczne

    ### 1. Crash w RTree przy usuwaniu węzłów drogowych
    **Priorytet: Wysoki**
    - **Lokalizacja:** `TranSimCS/Spatial/RTree.cs:223`
    - **Problem:** Assertion failure w metodzie `ChooseSubtree()` podczas usuwania węzłów drogowych
    - **Stack trace:** Dostępny w `stacktrace.txt`
    - **Wpływ:** Aplikacja crashuje przy użyciu narzędzia demolki dróg

    ### 2. Bug MeshIntersectTriangle
    **Priorytet: Średni**
    - **Lokalizacja:** `TranSimCS/Program.cs:38`
    - **Problem:** Odnotowany bug bez szczegółów
    - **Status:** Wymaga dalszej analizy

    ## 🔧 Zadania TODO w kodzie

    ### 3. Brak rozszerzalności InspectTool
    **Priorytet: Średni**
    - **Lokalizacja:** `TranSimCS/Tools/InspectTool.cs:63`
    - **TODO:** "Add expandability for more inspectors"
    - **Opis:** System inspekcji wymaga mechanizmu rozszerzalności

    ### 4. Brak inicjalizacji w Game1
    **Priorytet: Niski**
    - **Lokalizacja:** `TranSimCS/Game1.cs:84`
    - **TODO:** "Add your initialization logic here"
    - **Opis:** Placeholder z szablonu MonoGame

    ## 🏗️ Architektura i jakość kodu

    ### 5. Debug code w kodzie produkcyjnym
    **Priorytet: Średni**
    - **Lokalizacja:** `TranSimCS/Roads/SegmentRenderer.cs`
    - **Problem:** Wiele wywołań `Debug.Print()` w kodzie produkcyjnym (linie 111, 121, 127, 195)
    - **Rozwiązanie:** Usunąć lub przenieść do warunkowej kompilacji (#if DEBUG)

    ### 6. Hardcoded opcje debugowania
    **Priorytet: Niski**
    - **Lokalizacja:** `TranSimCS/Debugging/DebugOptions.cs:9`
    - **Problem:** `DebugIslands` jako stała zamiast konfigurowalnej opcji
    - **Rozwiązanie:** Przenieść do pliku konfiguracyjnego lub menu deweloperskiego

    ### 7. Puste foldery w projekcie
    **Priorytet: Niski**
    - **Lokalizacja:** `TranSimCS.csproj:51-52`
    - **Foldery:** `Menus/Gizmo/`, `Menus/MainMenu/`
    - **Rozwiązanie:** Usunąć lub zaimplementować brakującą funkcjonalność

    ### 8. Nieużywane pliki ikon
    **Priorytet: Niski**
    - **Lokalizacja:** Katalog główny TranSimCS
    - **Pliki:** `bad icon.ico`, `sus.ico`
    - **Rozwiązanie:** Usunąć lub przenieść do folderu archiwum

    ## 📚 Dokumentacja

    ### 9. Niekompletny README.md
    **Priorytet: Średni**
    - **Problem:** Brak implementacji sekcji wymienionych w Table of Contents:
      - Contributing
      - Support
      - License
    - **Rozwiązanie:** Dodać brakujące sekcje lub usunąć z TOC

    ### 10. Niespójność wersji .NET w dokumentacji
    **Priorytet: Wysoki**
    - **Problem:** README.md wymienia .NET Framework 4.7.2, projekt używa .NET 8.0
    - **Lokalizacja:** `README.md:39` vs `TranSimCS.csproj:4`
    - **Rozwiązanie:** Zaktualizować dokumentację do .NET 8.0

    ### 11. Rozproszona dokumentacja
    **Priorytet: Niski**
    - **Problem:** Wiele plików dokumentacji w katalogu głównym:
      - `CHANGELOG_SAVE_SYSTEM.md`
      - `CONVERTER_REFERENCE.md`
      - `FIX_SERIALIZATION_EXCEPTION.md`
      - `MIGRATION_COMPLETE.md`
      - `Solving SplineFrame.md`
      - `The Tutorial.odt`
      - `SplineFrame solution.odf`
      - `SplineFrame 2nd solution.odf`
    - **Rozwiązanie:** Utworzyć folder `docs/` i uporządkować dokumentację

    ### 12. Pliki tymczasowe w repozytorium
    **Priorytet: Średni**
    - **Pliki:**
      - `obliczenia blokowiska transim.xlsx`
      - `Raport*.diagsession` (3 pliki)
      - `save load dialog.odg`
      - `Here's a new idea for managing road.txt`
    - **Rozwiązanie:** Przenieść do .gitignore lub usunąć

    ## 🔒 Bezpieczeństwo i konfiguracja

    ### 13. AllowUnsafeBlocks włączone
    **Priorytet: Średni**
    - **Lokalizacja:** `TranSimCS.csproj:14`
    - **Problem:** Unsafe code włączony bez widocznego uzasadnienia
    - **Rozwiązanie:** Sprawdzić czy jest potrzebny, jeśli nie - wyłączyć

    ### 14. Brak pliku .gitignore
    **Priorytet: Wysoki**
    - **Problem:** Brak standardowego .gitignore dla C#/.NET
    - **Wpływ:** Pliki binarne, cache, raporty mogą trafiać do repozytorium
    - **Rozwiązanie:** Dodać .gitignore z szablonu Visual Studio/Rider

    ## 🧪 Testowanie i CI/CD

    ### 15. Brak testów jednostkowych
    **Priorytet: Wysoki**
    - **Problem:** Brak projektu testowego w solution
    - **Rozwiązanie:** Utworzyć projekt `TranSimCS.Tests` z xUnit/NUnit/MSTest

    ### 16. Brak CI/CD
    **Priorytet: Średni**
    - **Problem:** Brak automatyzacji buildów i testów
    - **Rozwiązanie:** Dodać GitHub Actions workflow:
      - Build verification
      - Uruchamianie testów
      - Code quality checks
      - Automated releases

    ### 17. Brak analizy statycznej kodu
    **Priorytet: Średni**
    - **Problem:** Brak konfiguracji code analyzers
    - **Rozwiązanie:** Dodać pakiety:
      - StyleCop.Analyzers
      - Roslynator
      - SonarAnalyzer.CSharp
    - **Konfiguracja:** Utworzyć `.editorconfig` z regułami

    ## 📦 Zależności i pakiety

    ### 18. Potencjalnie przestarzałe zależności
    **Priorytet: Niski**
    - **Problem:** Niektóre pakiety mogą wymagać aktualizacji
    - **Pakiety do sprawdzenia:**
      - `System.Linq: 4.3.0` (bardzo stara wersja)
      - `System.ObjectModel: 4.3.0` (bardzo stara wersja)
      - `MLEM: 8.0.0-ci.222` (wersja CI/beta)
      - `Arch: 2.1.0-beta` (wersja beta)
    - **Rozwiązanie:** Przejrzeć i zaktualizować do stabilnych wersji

    ### 19. Brak pliku LICENSE
    **Priorytet:** Średni
    - **Problem:** README wspomina licencję MIT, ale brak pliku LICENSE
    - **Rozwiązanie:** Dodać plik LICENSE z pełnym tekstem licencji MIT

    ## 🎯 Ulepszenia funkcjonalne

    ### 20. Brak systemu logowania błędów
    **Priorytet: Średni**
    - **Obecny stan:** NLog jest skonfigurowany (`NLog.config`)
    - **Problem:** Brak widocznego użycia w kodzie dla błędów krytycznych
    - **Rozwiązanie:** Dodać try-catch z logowaniem w krytycznych miejscach (np. RTree operations)

    ### 21. Brak walidacji danych wejściowych
    **Priorytet: Średni**
    - **Problem:** Brak widocznej walidacji w serializacji/deserializacji
    - **Rozwiązanie:** Dodać walidację w converterach JSON

    ## 📊 Podsumowanie priorytetów

    ### Krytyczne (do natychmiastowej naprawy):
    1. Crash w RTree przy usuwaniu węzłów
    2. Brak .gitignore
    3. Niespójność dokumentacji .NET

    ### Wysokie (ważne dla jakości):
    4. Brak testów jednostkowych
    5. Brak pliku LICENSE
    6. Debug code w produkcji

    ### Średnie (ulepszenia):
    7. Brak CI/CD
    8. Rozproszona dokumentacja
    9. Analiza statyczna kodu
    10. AllowUnsafeBlocks

    ### Niskie (nice to have):
    11. Puste foldery
    12. Nieużywane ikony
    13. Hardcoded debug options
    14. Aktualizacja zależności