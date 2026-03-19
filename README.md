# Fatum

A Randonautica-style app that generates random coordinates using Kernel Density Estimation (KDE) to suggest points of interest: **Attractor**, **Void**, or **Power**. It supports generation with PRNG (C# Random) or QRNG (ANU) and is available for Android, iOS, and Windows.

> **Disclaimer (Independent Project):** This repository is an independent, community-driven open source project. It is **not** an official project, product, or endorsement of any third-party app, brand, or company (including "Randonautica").

If you're unfamiliar with Randonauting, the concepts of Probability Blind-Spots and Quantum Randomness, I recommend reading [fatum_theory.txt](https://github.com/anonyhoney/fatum-en/blob/master/docs/fatum_theory.txt) that came with the original Fatum project bot that inspired another well-known commercial randonauting app. If you have no idea what this is about and are completely new to this field, you should read [this article](https://medium.com/swlh/randonauts-how-a-random-number-generator-can-set-you-free-dfc2a2413e15).

---

## Features

- **Search types**: Attractor, Void, or Power based on KDE density.
- **Configurable radius**: between 1 and 10 km from your location or a chosen point.
- **Randomness generators**: PRNG (C# Random) or QRNG (ANU API; requires API Key).
- **History**: local storage in SQLite with export/import to JSON.
- **Map**: area view, GPS, and opening the generated point in Google Maps.
- **Cross-platform**: .NET MAUI (Android, iOS, Windows).

---

## Requirements

- .NET 10 SDK
- For Android: Android SDK (API 24+)
- For iOS: Xcode and Mac (Pair to Mac from Windows)
- For Windows: Windows 10/11 (SDK 19041+)

---

## How to build and run

```bash
# Clone the repository
git clone https://github.com/joejimball/fatum-engine.git
cd fatum-engine

# Restore and build
dotnet restore
dotnet build

# Run on Windows
dotnet run --project FatumApp.Maui -f net10.0-windows10.0.19041.0

# Run on Android (emulator or connected device)
dotnet run --project FatumApp.Maui -f net10.0-android
```

From **Visual Studio**: open `Fatum.sln`, select the profile (Windows / Android / iOS) and press F5.

---

## ANU (QRNG) API Key

If you use **QRNG** mode (quantum random numbers), the app uses the [ANU Quantum Random Number Generator](https://www.anu.edu.au/research/research-services/anu-quantum-random-number-generator) API. To use it you need an **API Key** and must configure it in the app.

### Requirements and limits

- **Obtaining the API Key**: You can register and create an API Key on the ANU QRNG website. The free tier usually allows a limited number of requests per month (e.g. 100 requests/month in the public offering; check the [ANU QRNG FAQ](https://qrng.anu.edu.au/contact/faq/) and their current terms).
- **Use in Fatum**: The API Key is sent in the `x-api-key` header to `https://api.quantumnumbers.anu.edu.au`. The app makes several requests per generation (up to 20480 numbers per request according to ANU limits) and applies a short delay between requests to avoid exceeding usage limits.
- **Where to configure it**: In the app, go to **Quantum Settings** (bottom menu) → select **QRNG (ANU)** → enter your API Key and tap **Save and validate API Key**. The key is stored securely on the device (SecureStorage).
- **Without API Key**: If you do not configure a valid API Key, QRNG mode may fail or return errors. For use without an API Key you can use **PRNG (C# Random)** in Quantum Settings.
- **More information**: See [ANU terms and conditions](https://qrng.anu.edu.au/contact/) and their official documentation for limits, commercial use, and contact (e.g. `cqc2t@anu.edu.au` for larger sequences or technical questions).

---

## Project structure

- **FatumCommon**: KDE logic, models (Fatum, Anomalia), point generation, PRNG/QRNG (ANU).
- **FatumApp.Maui**: Blazor MAUI hybrid app (UI, SQLite, services, Generate, History, Quantum Settings pages).

---

## Responsibility and responsible use

Fatum is a leisure app that suggests random coordinates. **Use is at your own risk.** By using the app you accept the following conditions and good practices, inspired by responsible use of similar apps (e.g. Randonautica):

### Use at your own risk

- **Not advice**: Fatum does not provide medical, psychological, legal, or safety advice. It does not replace the judgment of authorities or professionals.
- **User assumes risk**: Going to the generated coordinates is your decision. The developers and contributors of the project are not responsible for what happens at those locations or for any damage or harm arising from use of the app.
- **Private property**: **Never enter private property without permission.** Approach the point only via allowed roads and areas. Non-compliance is the user’s responsibility to owners and authorities, not the Fatum project.
- **Dangerous areas**: Do not approach railways, restricted electrical installations, unsafe structures, or abandoned buildings. Avoid any area you consider unsafe.
- **Time and visibility**: Exploring during the day is recommended so you can better assess the environment and reduce risks.
- **Charged device**: Keep your phone charged or bring a charger; GPS and the app drain the battery and you may end up in an unfamiliar area.
- **Common sense**: Do not go to places outside your comfort zone. If you feel unsafe, do not leave the vehicle or move away from the point. Respect your physical limits and stay hydrated.
- **Company**: Beginners are advised to go with others or in a small group.
- **Environment**: You are encouraged to leave the place as good as or better than you found it (e.g. do not leave litter).

By using Fatum you accept that you have read and understood this section and that you use the app at your own risk.

---

## License

This project is under the [GNU General Public License v3.0 (GPL-3.0)](LICENSE). It is free software: you may redistribute and/or modify it under the terms of the GPL 3.0. See the [LICENSE](LICENSE) file for the full text.

---

## Download



---

## Credits

- Conceptually inspired by apps that combine random coordinates and exploration (e.g. Randonautica).
- Optional QRNG via [ANU Quantum Random Numbers](https://www.anu.edu.au/research/research-services/anu-quantum-random-number-generator).
- Icons/styles: Bootstrap, project resources.
