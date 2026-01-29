# Projekt TBO - Implementacja bezpiecznego procesu CI/CD (DevSecOps)

Celem projektu było stworzenie środowiska DevSecOps dla aplikacji webowej (Backend: .NET, Frontend: React, Baza: PostgreSQL) oraz implementacja automatycznego pipeline'u wykrywającego podatności bezpieczeństwa.

---

## Zespół projektowy
1. **Jan Konarski** - Właściciel repozytorium
2. **Jakub Szewczyk**
3. **Jarek Jaworski**
4. **Karol Zębala**

---

## Zadanie 1: Projekt i Implementacja Procesu CI/CD

Proces CI/CD został zrealizowany przy użyciu **GitHub Actions**. Pipeline jest skonfigurowany w pliku [Prodction Pipeline](.github/workflows/security-pipeline.yaml) oraz [Development Pipeline](.github/workflows/security-pipeline-beta.yaml). 

Cały proces opiera się na strategii **"Secure by Design"** – wdrożenie (publikacja obrazów) jest możliwe tylko wtedy, gdy wszystkie poprzednie etapy bezpieczeństwa zakończą się sukcesem.

---

### 🔄 Przepływ pracy (Pipeline Workflow)

Pipeline składa się z czterech sekwencyjnych etapów (jobs), które gwarantują jakość i bezpieczeństwo kodu:

1.  🛡️ **Static Security** – Analiza statyczna kodu i skanowanie sekretów.
2.  🐳 **Build & Container Security** – Budowa obrazu Docker oraz jego skanowanie pod kątem podatności OS.
3.  🔥 **Dynamic Security** – Testy penetracyjne (DAST) na uruchomionej instancji aplikacji.
4.  📦 **Upload & Publish** – Publikacja zweryfikowanych i bezpiecznych obrazów do DockerHub.

---

### 🛡️ Zastosowane mechanizmy bezpieczeństwa

W procesie wykorzystaliśmy podejście wielowarstwowe (Defense in Depth), implementując następujące narzędzia:

#### 1. Wykrywanie Sekretów (Secret Scanning)
* **Narzędzie:** `Gitleaks` (wersja 8.18.2)
* **Cel:** Ochrona przed wyciekiem haseł, kluczy API i tokenów do repozytorium kodu.
* **Działanie:** Skanuje pełną historię commitów w poszukiwaniu wzorców wrażliwych danych. Wykrycie wycieku blokuje pipeline. Raport generowany jest w formacie **SARIF**.

#### 2. Statyczna Analiza Kodu (SAST)
* **Narzędzie:** `Semgrep`
* **Cel:** Wykrywanie błędów w kodzie źródłowym i podatności logicznych bez uruchamiania aplikacji.
* **Konfiguracja:** Zastosowany zestaw reguł:
    * `p/owasp-top-ten` oraz `p/cwe-top-25`
    * Dedykowane skanery pod kątem **SQL Injection** oraz **Command Injection**.
    * Specjalistyczne reguły dla języka **C#** (`security-code-scan`, `csharp`).

#### 3. Analiza Składników Oprogramowania (SCA - Filesystem)
* **Narzędzie:** `Trivy` (tryb `fs`)
* **Cel:** Weryfikacja bibliotek i zależności w systemie plików (przed budową) pod kątem znanych podatności (CVE), błędów konfiguracji oraz ukrytych sekretów.

#### 4. Bezpieczeństwo Kontenerów (Container Security)
* **Narzędzie:** `Trivy` (tryb `image`)
* **Cel:** Skanowanie zbudowanego obrazu Docker `apd.api` przed jego uruchomieniem.
* **Działanie:** Analiza warstw obrazu pod kątem podatności systemu operacyjnego (**OS vulnerabilities**) oraz pakietów systemowych. Wykrycie błędów na tym etapie blokuje przejście do testów dynamicznych.

#### 5. Dynamiczne Testy Bezpieczeństwa (DAST)
* **Narzędzie:** `OWASP ZAP` (Zed Attack Proxy) – **Full Scan**
* **Cel:** Aktywne testy penetracyjne ("black-box") uruchomionej aplikacji.
* **Działanie:**
    * Pipeline uruchamia środowisko przy pomocy `docker compose`.
    * ZAP wykonuje pełny skan, atakując endpointy API.
    * Weryfikacja nagłówków bezpieczeństwa, wycieków danych w odpowiedziach HTTP oraz odporności na iniekcje.

#### 6. Publikacja i Dystrybucja (Registry)
* **Narzędzie:** `DockerHub`
* **Cel:** Dostarczenie bezpiecznych i zweryfikowanych artefaktów.
* **Działanie:** Ostatni krok uruchamiany **wyłącznie** po sukcesie wszystkich skanów.
    * Bezpieczne logowanie przez `DOCKERHUB_TOKEN`.
    * Publikacja obrazów: **Backend** (`apd.api:latest` lub `apd.api:beta`) oraz **Frontend** (`frontend-latest` lub `frontend-beta`).
    * Optymalizacja czasu budowania dzięki wykorzystaniu **GitHub Actions Cache** (`type=gha`).

---

## Zadanie 2: Weryfikacja działania (Symulacja ataku)

Zgodnie z wymaganiami projektu, utworzyliśmy osobną gałąź, na której celowo wprowadziliśmy podatności, aby udowodnić skuteczność zabezpieczeń.

* **Nazwa gałęzi testowej:** `test`

### Wprowadzone podatności

1.  **Podatność SQl Injection:**
    Został do projektu dodany endpoint z tą podatnością - użytkownik może pobrać więcej danych niż przewidział autor kodu.
    Implementacja podatności:
    ```csharp
    [HttpGet("users")]
    public IActionResult Get(string email)
    {
        var users = _apdDbContext.Users
            .FromSqlRaw($"SELECT * FROM \"AspNetUsers\" WHERE \"Email\" = '{email}'")
            .ToList();
        
        return Ok(users);
    }
    ```
    Oraz pokazanie działania - zamiast pojedyńczego adresu mailowego zwracana jest lista wszystkich adresów z bazy:
    ![sql-injection](https://scontent-waw2-1.xx.fbcdn.net/v/t1.15752-9/609254378_1540891477026946_5859120313486244890_n.png?_nc_cat=111&ccb=1-7&_nc_sid=9f807c&_nc_ohc=9B-duimjTf4Q7kNvwEk5rt6&_nc_oc=AdmUdP4YQ3GuNrn_FwD5Jzzmm8E2iQE0oRZKNpxq8V2TsdoQm3_iQoaslOqL9VpYvRY&_nc_zt=23&_nc_ht=scontent-waw2-1.xx&oh=03_Q7cD4QFfbKbgjcF1KXJP0Igpz-k9r3eODamYGNp-VhpXdEdn7Q&oe=69A2F25E)

2.  **Command Injection:**
    Drugą podatnością jest command injection - możliwość wykonania dowolnego polecenia na serwerze.
    Podatny kod:
    ```csharp
    [HttpGet("ping")]
    public IActionResult PingHost(string hostname)
    {
        try
        {
            bool isWindows = OperatingSystem.IsWindows();
    
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = isWindows ? "cmd.exe" : "/bin/bash",
                    Arguments = isWindows
                        ? $"/c ping {hostname}"
                        : $"-c \"ping -c 4 {hostname}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
    
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
    
            return Content(string.IsNullOrWhiteSpace(output) ? error : output, "text/plain");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    ```
    Efekt działania:
    ![command-injection](https://scontent-waw2-1.xx.fbcdn.net/v/t1.15752-9/618802918_1561598261625299_4699893866115953101_n.png?_nc_cat=111&ccb=1-7&_nc_sid=9f807c&_nc_ohc=60-G2CheNFIQ7kNvwE6sMan&_nc_oc=AdmwD5kt13BUjwoG7CnQ-OFvXA1MtgEMcJdPmuMSn8I_Yy3vhF_-BkqBzD4GZ0IJwao&_nc_zt=23&_nc_ht=scontent-waw2-1.xx&oh=03_Q7cD4QGQIWaAt95KNov6dSbYxmNT7-xzswqCkacT0v1FV5PINA&oe=69A2F2E4)

---

## Wnioski

Zaimplementowany pipeline DevSecOps skutecznie realizuje założenia bezpieczeństwa. Dzięki zastosowaniu narzędzi na różnych etapach:
1.  Unikamy wdrażania kodu z jawnymi błędami (SAST).
2.  Eliminujemy przestarzałe i dziurawe biblioteki (SCA).
3.  Zapewniamy, że kontenery produkcyjne są zgodne z dobrymi praktykami (Container Scan).
4.  Weryfikujemy ostateczny stan aplikacji (DAST).