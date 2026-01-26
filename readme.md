# Projekt TBO - Implementacja bezpiecznego procesu CI/CD (DevSecOps)

Celem projektu było stworzenie środowiska DevSecOps dla aplikacji webowej (Backend: .NET, Frontend: React, Baza: PostgreSQL) oraz implementacja automatycznego pipeline'u wykrywającego podatności bezpieczeństwa.

---

## 👨‍💻 Zespół projektowy
1. **Jan Konars** - Właściciel repozytorium
2. **Jakub Szewczyk**
3. **Jarek Jaworski**
4. **Karol Zębala**

---

## 🚀 Zadanie 1: Projekt i Implementacja Procesu CI/CD

Proces CI/CD został zrealizowany przy użyciu **GitHub Actions**. Pipeline jest skonfigurowany w pliku `.github/workflows/security-pipeline.yaml` i realizuje podejście **Shift-Left Security**, blokując wdrożenie w przypadku wykrycia zagrożeń.

### Zastosowane mechanizmy bezpieczeństwa

W procesie wykorzystaliśmy podejście wielowarstwowe, implementując następujące skanery:

#### 1. Wykrywanie Sekretów (Secret Scanning)
* **Narzędzie:** `Gitleaks`
* **Cel:** Ochrona przed wyciekiem haseł, kluczy API i tokenów do repozytorium kodu.
* **Działanie:** Skanuje historię commitów w poszukiwaniu wzorców wrażliwych danych.

#### 2. Statyczna Analiza Kodu (SAST)
* **Narzędzie:** `Semgrep` (konfiguracja dla C# i ogólnych reguł bezpieczeństwa)
* **Cel:** Wykrywanie błędów w kodzie źródłowym (np. SQL Injection, XSS, niebezpieczne funkcje) bez uruchamiania aplikacji.

#### 3. Analiza Składników Oprogramowania (SCA - Filesystem)
* **Narzędzie:** `Trivy` (tryb `fs`)
* **Cel:** Weryfikacja bibliotek i zależności (frontend/backend) pod kątem znanych podatności (CVE) oraz błędów konfiguracji (Misconfiguration).

#### 4. Bezpieczeństwo Kontenerów (Container Security)
* **Narzędzie:** `Trivy` (tryb `image`)
* **Cel:** Skanowanie zbudowanego obrazu Docker (`apd.api`) przed jego wdrożeniem. Sprawdza podatności systemu operacyjnego (Debian/Alpine) oraz warstw obrazu.

#### 5. Dynamiczne Testy Bezpieczeństwa (DAST)
* **Narzędzie:** `OWASP ZAP` (Zed Attack Proxy)
* **Cel:** "Atak" na uruchomioną w kontenerach aplikację.
* **Działanie:** Pipeline uruchamia pełne środowisko (`docker compose up`), a następnie skaner ZAP wykonuje testy penetracyjne na działającym API, szukając błędów konfiguracji nagłówków, wycieków informacji itp.

---

## 🛡️ Zadanie 2: Weryfikacja działania (Symulacja ataku)

Zgodnie z wymaganiami projektu, utworzyliśmy osobną gałąź, na której celowo wprowadziliśmy podatności, aby udowodnić skuteczność zabezpieczeń.

* **Nazwa gałęzi testowej:** `[NAZWA_TWOJEJ_GAŁĘZI_NP_SECURITY-TEST]`

### Wprowadzone podatności (Proof of Concept)

1.  **Podatność SCA (Biblioteki):**
    * Dodano bibliotekę `form-data` w wersji `3.0.2` (podatność CVE-2025-7783 - Critical/High).
    * **Wynik:** Trivy zablokował pipeline na etapie skanowania systemu plików.

2.  **Podatność Kontenerowa (Misconfiguration):**
    * Uruchomienie kontenera z uprawnieniami `root` (brak dyrektywy `USER` w Dockerfile).
    * **Wynik:** Trivy Image Scan zgłosił błąd `AVD-DS-0002` (Running as root).

3.  **[OPCJONALNIE] Hardcoded Secret / SAST:**
    * [Opis, np. Pozostawiono hasło w kodzie C#]
    * **Wynik:** Gitleaks/Semgrep wykrył zagrożenie.

### 🛑 Dowód skuteczności (Link do Failed Job)

Poniżej znajduje się link do uruchomienia pipeline'u, który zakończył się błędem (zablokowaniem wdrożenia) po wykryciu powyższych podatności:

🔗 **[LINK DO ZAKŁADKI ACTIONS Z CZERWONYM WYNIKIEM - WKLEJ TUTAJ]**

*(Możesz również dodać screenshot z logów pokazujący czerwoną informację o wykrytych błędach)*

---

## 📝 Wnioski

Zaimplementowany pipeline DevSecOps skutecznie realizuje założenia bezpieczeństwa. Dzięki zastosowaniu narzędzi na różnych etapach (kod, zależności, obraz docker, działająca aplikacja):
1.  Unikamy wdrażania kodu z jawnymi błędami (SAST).
2.  Eliminujemy przestarzałe i dziurawe biblioteki (SCA).
3.  Zapewniamy, że kontenery produkcyjne są zgodne z dobrymi praktykami (Container Scan).
4.  Weryfikujemy ostateczny stan aplikacji "z zewnątrz" (DAST).

Proces jest w pełni zautomatyzowany i blokuje wdrożenie (Exit Code 1) w przypadku wykrycia zagrożeń o poziomie High lub Critical.

---

## ⚙️ Uruchomienie projektu lokalnie

Aby uruchomić aplikację lokalnie (wymagany Docker Desktop):

```bash
git clone [LINK_DO_REPO]
cd TBO_Projekt
docker compose up --build