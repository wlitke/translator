# Quickstart: Translate speech in C# for .NET Framework for Windows

This project is a console application for translating speech with C# under the .NET Framework (version 4.6.1 or above) using the Speech SDK for Windows.
See the [accompanying article](https://docs.microsoft.com/azure/cognitive-services/speech-service/get-started-speech-translation?tabs=script%2Cwindowsinstall&pivots=programming-language-csharp) on the SDK documentation page which describes how to build a sample from scratch in Visual Studio 2017.

## Prerequisites

* A subscription key for the Speech service. See [Try the speech service for free](https://docs.microsoft.com/azure/cognitive-services/speech-service/get-started).
* A Windows PC with a working microphone.
* [Microsoft Visual Studio 2017](https://www.visualstudio.com/), Community Edition or higher.
* The **.NET desktop development** workload in Visual Studio.
  You can enable it in **Tools** \> **Get Tools and Features**.

## Build the project

* **By building this project you will download the Microsoft Cognitive Services Speech SDK. By downloading you acknowledge its license, see [Speech SDK license agreement](https://aka.ms/csspeech/license201809).**
* Start Microsoft Visual Studio 2017 and select **File** \> **Open** \> **Project/Solution**.
* Navigate to the folder containing this project, and select the solution file contained within it.
* Edit the XML section `appSettings` of the `App.config` file:
  * Change the value of `SubscriptionKey` with your own subscription key. Use the `Speech` resource in Azure (not the `Speech Recognition` resource).
  * Change the value of `Region` with the service region of your subscription.
    For example, replace with `westus` if you are using the 30-day free trial subscription. Make sure the region in your Azure resource matches the region you put into the `App.config`, otherwise you'll get a 401 unauthorized access error.
  * Change the value of `FromLanguage` to the language which should be detected for translation, e.g., `de-DE`, `en-US`, `uk-UA`. For all supported languages see [https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=stt-tts](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=stt-tts).
  * Change the value of `TargetLanguage` to the language to which translation should be performed.
  * Change the value of `TargetVoice` to the voice which should be used for translation, e.g., `de-DE-BerndNeural`, `en-US-ChristopherNeural`, `uk-UA-OstapNeural`. 
* Set the active solution configuration and platform to the desired values under **Build** \> **Configuration Manager**:
  * On a 64-bit Windows installation, choose `x64` as active solution platform.
  * On a 32-bit Windows installation, choose `x86` as active solution platform.
* Press Ctrl+Shift+B, or select **Build** \> **Build Solution**.

## Run the project

To debug the app and then run it, press F5 or use **Debug** \> **Start Debugging**. To run the app without debugging, press Ctrl+F5 or use **Debug** \> **Start Without Debugging**. In general, the user can select both, the audio input device and audio output device that should be used for in- and output of translation. To use a wave file as audio input for translation the user can specify a path to the wave file as command line argument, e.g., `translator.exe "C:\...\file.wav"`. 

## References

* [Quickstart article on the SDK documentation site](https://docs.microsoft.com/azure/cognitive-services/speech-service/get-started-speech-translation?tabs=script%2Cwindowsinstall&pivots=programming-language-csharp)
* [Speech SDK API reference for C#](https://aka.ms/csspeech/csharpref)
