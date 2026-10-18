[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![Github All Releases](https://img.shields.io/github/downloads/ramjke/Translumo/total.svg)]()

<p align="center">
  <img width="670" src="https://github.com/ramjke/Translumo/assets/29047281/8985049f-ea1c-428e-94be-042ece66cb54">
</p>
  <h2 align="center" style="border: 0">Advanced Real-Time Screen Translator</h2>

<p align="center"><strong>English</strong> | <a href="docs/README-RU.md"><strong>Русский</strong></a></p>

---

# 🚀 Version 1.6 Update: What's New & Fixed?

This version of **Translumo** has been updated to Version 1.6, fixed Google, Yandex and ONNX translator.

## ✨ New Features & UI Enhancements
1. **ONNX translator**: Added a repetition penalty to improve the translation. The text translation method has been changed from Greedy Search to Beam Search, enabling greater translation accuracy. The number of branches used for translation evaluation is user-configurable.
2. **ONNX Language Setup Guide**: Text update.
3. **Settings Window Languages**: remove vertical scroll bar. Text is wrapped.

## 🛠️ Major Bug Fixes
1. **Google translator**: Fixed a (404) Not Found error caused by changes made by Google on September, 2026.. 
2. **Yandex translator**: Fixed infinity loop browser window (based on pull request: Fix Yandex translator - #295 by no1d)
3. **ONNX translator**: Fix value pad token `54795` not work with all models. For example, the `EN-CS`, `ZH-EN`, and other models have completely different values. Therefore, a change was made so that token values and other parameters are loaded from the `generation_config.json` configuration file separately for each model.
4. **Internal**: Fixed some XAML binding failures.

---

# 🚀 Version 1.5 Update: What's New & Fixed?

This version of **Translumo** has been updated to Version 1.5, focusing heavily on ONNX offline translation stability, CPU optimization, and ease of use.

## ✨ New Features & UI Enhancements
1. **ONNX Language Setup Guide**: Added a built-in, step-by-step UI guide inside the Settings menu to help users easily install and configure new language models for the ONNX Opus-MT translation engine.

## 🛠️ Major Bug Fixes & CPU Optimizations
1. **ONNX CPU Spiking Resolved**: Fixed a severe issue where ONNX would aggressively consume 100% of all CPU cores. By enforcing `IntraOpNumThreads=1` and `InterOpNumThreads=1`, ONNX now runs stably on a single thread, greatly reducing system load and eliminating CPU spiking during gameplay.
2. **Infinite Loop & Garbage Text Fix**: Resolved a critical greedy-search decoding bug where the `bad_words_ids` (specifically the pad token `54795`) were not suppressed. This ensures the decoder properly terminates and produces clean text.
3. **Language Switch Detection**: Fixed a bug where ONNX model sessions were not properly disposed when users switched language pairs (e.g., from `en-id` to `ja-id`), preventing stale translations.
4. **Resilient Model Downloads**: Improved the model downloader by using `.tmp` files and atomic renaming. This prevents corrupted `.onnx` files if the application is closed or internet drops during a download.

---

# 🚀 Version 1.4 Update: What's New & Fixed?

This version of **Translumo** has been heavily upgraded to Version 1.4, bringing powerful new translation engines and critical crash fixes for offline usage.

## ✨ New Features
1. **NVIDIA NIM Integration**: Added support for NVIDIA NIM (NVIDIA Inference Microservices) as a translation engine, allowing blazing fast translations using NVIDIA's optimized AI containers.
2. **NVIDIA Riva Support**: Integrated NVIDIA Riva for advanced AI-driven workflows.
3. **ONNX Offline Translation (Opus-MT)**: Integrated completely offline translation capabilities using ONNX CPU/NPU execution for HuggingFace `opus-mt` models. The system automatically downloads the required models and dynamically patches Rust tokenizer incompatibilities on the fly!

## 🛠️ Major Bug Fixes & Stability Improvements
1. **ONNX Tokenizer Rust Incompatibility Fix**: Resolved a critical JSON parsing crash (`Tokenizers.DotNet.TokenizerException`) when loading HuggingFace models by implementing an on-the-fly JSON patcher that removes incompatible JS `normalizer` blocks without breaking the JSON structure.
2. **Configuration Save Crash (Gamepad Hotkey)**: Fixed a major bug that caused the application to Force Close when exiting. The issue was traced to an unimplemented `GetHashCode()` method in `GamepadHotKeyInfo.cs` during XML serialization, which has now been fully implemented with a stable hash calculation.

---

# 🚀 Version 1.3 Update: What's New & Fixed?
This version of **Translumo** has been customized and updated to Version 1.3. It includes exclusive features and major stability fixes that are not present in the original repository.

## ✨ New Features
1. **AI Translator Integration**: Added support for translating via Advanced AI Models (e.g., Gemini, ChatGPT/OpenAI compatible APIs) to achieve highly contextual and natural game translations.
2. **Local LibreTranslate Support**: Fully integrated a completely offline, local LibreTranslate server support. Translumo will automatically manage the server startup and shutdown.
3. **Clear Chat Hotkey (`ALT+C`)**: A newly implemented global hotkey that allows you to instantly clear the chat bubble/translation screen when it gets too cluttered.

## 🛠️ Major Bug Fixes & Stability Improvements
1. **Proxy Error Fixes**: Resolved persistent proxy errors that blocked connections to translators.
2. **LibreTranslate Lifecycle Manager**: Fixed a bug where simply opening or minimizing the Settings menu would aggressively kill the LibreTranslate server. The server is now safely managed by a background singleton manager and only shuts down when you completely exit Translumo from the tray.
3. **Restored Anti Double-Click**: Re-implemented the lock on the "Run LibreTranslate" button to prevent accidental spam-clicking that could cause duplicate server spawns.
4. **Dark Theme Removal**: Systematically removed the highly unstable, experimental Dark Theme feature that was causing Translumo to force-close and crash on startup. 

---
## Sibling Project
This project has a sibling called **[Lookupper](https://lookupper.com)** — a screen dictionary for language learning. It is similar to Translumo but built for a different purpose. Lookupper is built to help you *learn* a language, not just depend on a translator forever.

Lookupper is commercial project with a free version. If you find it useful and decide to grab the Pro version, you'll also be supporting the development of both Lookupper and Translumo.

## Download Translumo

**Direct download link to the latest version:**  
[Translumo_1.1.0.zip](https://github.com/ramjke/Translumo/releases/download/v.1.1.0/Translumo_1.1.0.zip)   
After downloading, unzip the archive and run `Translumo.exe`.

Version 1.1.0 moves DeepL and Yandex onto their official APIs, so both now need an API key. Google Translate still works with no setup. The full list of changes is on the [Releases page](https://github.com/ramjke/Translumo/releases).

## Main Features

- **High text recognition precision**  
  Translumo allows combining multiple OCR engines simultaneously. It uses a machine learning model to score each OCR result and selects the best one.  

  <p align="center">
    <img width="740" src="https://github.com/ramjke/Translumo/assets/29047281/649e5fab-a5de-4c54-a3d8-f7ea95b8f218">
  </p>

- **Game oriented**  
  Designed for real-time translation in PC games, but works anywhere on the screen with any application.

- **Low latency**  
  Several optimizations reduce system impact and minimize latency between text appearance and translation.

- **Integrated modern OCR engines**: Windows OCR (recommended), EasyOCR

- **Available translators**: Google Translate (works out of the box), DeepL (needs a free API key), Yandex Translate (needs a Yandex Cloud API key).

- **Supported recognition languages**: English, Russian, Japanese, Chinese (Simplified), Korean.

- **Supported translation languages**: English, Russian, Japanese, Chinese (Simplified), Korean, French, Spanish, German, Portuguese, Italian, Vietnamese, Thai, Turkish, Arabic, Greek, Brazilian Portuguese, Polish, Belarusian, Persian, Indonesian, Bulgarian, Czech, Danish, Estonian, Finnish, Hungarian, Lithuanian, Latvian, Dutch, Romanian, Slovak, Slovenian, Swedish, Ukrainian.

## System Requirements

### Minimal requirements to use Windows OCR
- Windows 10 version 2004 (build 19041) or later, or Windows 11
- DirectX 11 compatible GPU
- 2 GB RAM

### Minimal requirements to use EasyOCR
- NVIDIA GPU with CUDA SDK 11.8 support (GTX 750, 8xxM, 9xx series or newer)
- 8 GB RAM
- At least 5 GB of free storage space

## How to Use

![Preview](https://github.com/ramjke/Translumo/blob/7f4a73ffba0e5a0090ea0bfc3d72acb99832a0f4/docs/preview-EN.gif)

1. Open the Settings (**Alt+G**)
2. Select languages: source language for OCR and translation language
3. Select text recognition engines (see Usage Tips for recommended modes)
4. Define the capture area: press **Alt+Q** and select an area on the screen
5. Run translation (press **~**)

### Which OCR Engine to Use

**WindowsOCR** is fast, and for most text it is all you need.

EasyOCR is worth turning on when the text uses an unusual font or sits on a busy background — it copes with that noticeably better. In exchange it is slower and needs an Nvidia GPU.

### Select Minimum Capture Area
Reducing the capture area decreases the chance of picking up random letters from the background. Larger frames take longer to process.

### Set Up the DeepL API Key
DeepL works through its official API, so it needs a key. Create one for free at [deepl.com/pro-api](https://www.deepl.com/pro-api) — the free plan covers 500,000 characters per month — then paste it into **Languages -> DeepL API key**. The field only appears when DeepL is selected as the translator. Both free and paid keys work; Translumo picks the right endpoint automatically.

### Set Up the Yandex API Key
Yandex Translate also works through its official API. Create an API key in the [Yandex Cloud console](https://yandex.cloud/en/docs/iam/operations/api-key/create) for a service account with the `ai.translate.user` role, then paste it into **Languages -> Yandex API key**. The field only appears when Yandex is selected as the translator.

### Use Borderless or Windowed Modes in Games (Not Fullscreen)
These modes are required for correct translation overlay display. If your game does not support them, use tools like [Borderless Gaming](https://github.com/Codeusa/Borderless-Gaming).

## FAQ

**Q: I get "Failed to capture screen" or nothing happens after translation starts**  
A: Ensure the target window is active. Restart Translumo or reopen the target window if needed.

**Q: Borderless/windowed mode is set, but the translation window is under the game**  
A: With the game running and focused, press the hotkey (**Alt+T** by default) to hide and show the translation window.

**Q: EasyOCR package download failed**  
A: Try reinstalling while connected to a VPN.

**Q: DeepL does not translate**  
A: Check the API key in **Languages -> DeepL API key**. "The API key was rejected" means the key is wrong or expired; "translation quota exceeded" means the monthly character limit for that key is used up.

**Q: Yandex does not translate**  
A: Check the API key in **Languages -> Yandex API key**. "The API key was rejected" means the key is wrong; "no access to the translate service" means the service account is missing the `ai.translate.user` role.

**Q: Hotkeys don't work**  
A: Other applications may be intercepting hotkeys.

## Build

*Visual Studio 2022 and .NET 8 SDK are required.*

1. Clone the repository (the **master** branch always corresponds to the latest release):

    ```bash
    git clone https://github.com/ramjke/Translumo.git
    ```

> Note: During the build, **binaries_extract.bat** will automatically download and extract models and Python binaries (~400 MB) to the target output directory.

## Credits

- [Material Design In XAML Toolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)  
- [OpenCvSharp](https://github.com/shimat/opencvsharp)  
- [Python.NET](https://github.com/pythonnet/pythonnet)  
- [EasyOCR](https://github.com/JaidedAI/EasyOCR)  
- [Silero TTS](https://github.com/snakers4/silero-models)  

## Alternative Solutions

- [Lookupper](https://lookupper.com) — on-screen dictionary and translator for language learning.
- [ScreTran](https://github.com/PavlikBender/ScreTran) — simple screen translator.
- [ScreenTranslator](https://github.com/OneMoreGres/ScreenTranslator) - screen capture, OCR and translation tool.
