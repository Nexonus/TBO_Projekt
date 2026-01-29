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


### Dowód skuteczności (Link do Failed Job)

Poniżej znajduje się link do uruchomienia pipeline'u, który zakończył się błędem (zablokowaniem wdrożenia) po wykryciu powyższych podatności:

🔗 **[LINK DO ZAKŁADKI ACTIONS Z CZERWONYM WYNIKIEM - WKLEJ TUTAJ]**

*(Możesz również dodać screenshot z logów pokazujący czerwoną informację o wykrytych błędach)*

---

## Wnioski

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