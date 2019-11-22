using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication4.Models;
using MicrosoftSpeechSDKSamples;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace WebApplication4.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration configuration;
        public HomeController(ILogger<HomeController> logger, IConfiguration iconfiguration)
        {
            _logger = logger;
            this.configuration = iconfiguration;
        }

        [TempData]
        public string Message2 { get; set; }
        public IActionResult Index()
        {
            //TranslationWithFileAsync().Wait();
            ViewData["Message2"] = "";
            return View();
        }
        [HttpPost]
        [ActionName("Index")]
        public IActionResult IndexPost(string Video1, string Video2)
        {
            string val = Video2;
            if(Video1!="" || Video1!=null)
                val = Video1;
            TranslationWithFileAsync(val).Wait();            
            return View();
        }
        public async Task TranslationWithFileAsync(string val)
        {
            // <TranslationWithFileAsync>
            // Translation source language.
            // Replace with a language of your choice.
            string fromLanguage = "en-US";

            var keysecret = configuration["cognitiveservicekey1"];//"baf1a73eba70448e829ca3c3c9ff885c";

            // Creates an instance of a speech translation config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechTranslationConfig.FromSubscription(keysecret, "westus2");
            config.SpeechRecognitionLanguage = fromLanguage;

            // Translation target language(s).
            // Replace with language(s) of your choice.
            config.AddTargetLanguage("de");
            config.AddTargetLanguage("fr");

            var stopTranslation = new TaskCompletionSource<int>();
            var finalVal = "";
            // Creates a translation recognizer using file as audio input.
            // Replace with your own audio file name.
            try
            {
                string path = "";
                if (val == "v1")
                    path = "wwwroot\\AudioFiles\\whatstheweatherlike.wav";
                else
                    path = "wwwroot\\AudioFiles\\wreck-a-nice-beach.wav";
                using (var audioInput = AudioConfig.FromWavFileInput(@path))
                {
                    using (var recognizer = new TranslationRecognizer(config, audioInput))
                    {
                        // Subscribes to events.
                        recognizer.Recognizing += (s, e) =>
                        {
                            Console.WriteLine($"RECOGNIZING in '{fromLanguage}': Text={e.Result.Text}");
                            foreach (var element in e.Result.Translations)
                            {
                                Console.WriteLine($"    TRANSLATING into '{element.Key}': {element.Value}");
                            }
                        };

                        recognizer.Recognized += (s, e) => {
                            if (e.Result.Reason == ResultReason.TranslatedSpeech)
                            {
                                Console.WriteLine($"RECOGNIZED in '{fromLanguage}': Text={e.Result.Text}");
                                foreach (var element in e.Result.Translations)
                                {
                                    Console.WriteLine($"    TRANSLATED into '{element.Key}': {element.Value}");
                                    finalVal = e.Result.Text;
                                }
                            }
                            else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                                Console.WriteLine($"    Speech not translated.");
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            }
                        };

                        recognizer.Canceled += (s, e) =>
                        {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                            }

                            stopTranslation.TrySetResult(0);
                        };

                        recognizer.SpeechStartDetected += (s, e) => {
                            Console.WriteLine("\nSpeech start detected event.");
                        };

                        recognizer.SpeechEndDetected += (s, e) => {
                            Console.WriteLine("\nSpeech end detected event.");
                        };

                        recognizer.SessionStarted += (s, e) => {
                            Console.WriteLine("\nSession started event.");
                        };

                        recognizer.SessionStopped += (s, e) => {
                            Console.WriteLine("\nSession stopped event.");
                            Console.WriteLine($"\nStop translation.");
                            stopTranslation.TrySetResult(0);
                        };

                        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                        Console.WriteLine("Start translation...");
                        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Task.WaitAny(new[] { stopTranslation.Task });

                        // Stops translation.
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                        Message2 = finalVal;
                        ViewData["Message2"] = finalVal;
                    }
                }
                // </TranslationWithFileAsync>
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }

        }

        public async Task TranslateSpeechToSpeech(string val)
        {
            // Creates an instance of a speech translation config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechTranslationConfig.FromSubscription("baf1a73eba70448e829ca3c3c9ff885c", "westus2");

            // Sets source and target languages.
            // Replace with the languages of your choice, from list found here: https://aka.ms/speech/sttt-languages
            string fromLanguage = "en-US";
            string toLanguage = "hi";
            config.SpeechRecognitionLanguage = fromLanguage;
            config.AddTargetLanguage(toLanguage);

            // Sets the synthesis output voice name.
            // Replace with the languages of your choice, from list found here: https://aka.ms/speech/tts-languages
            config.VoiceName = "hi-IN-Kalpana";

            var finalVal = "";
            string path = "";
            if (val == "v1")
                path = "wwwroot\\AudioFiles\\whatstheweatherlike.wav";
            else
                path = "wwwroot\\AudioFiles\\wreck-a-nice-beach.wav";
            using (var audioInput = AudioConfig.FromWavFileInput(@path))
            {
                // Creates a translation recognizer using the default microphone audio input device.
                using (var recognizer = new TranslationRecognizer(config, audioInput))
                {
                    // Prepare to handle the synthesized audio data.
                    recognizer.Synthesizing += (s, e) =>
                    {
                        var audio = e.Result.GetAudio();
                        Console.WriteLine(audio.Length != 0
                            ? $"AUDIO SYNTHESIZED: {audio.Length} byte(s)"
                            : $"AUDIO SYNTHESIZED: {audio.Length} byte(s) (COMPLETE)");
                    };

                    // Starts translation, and returns after a single utterance is recognized. The end of a
                    // single utterance is determined by listening for silence at the end or until a maximum of 15
                    // seconds of audio is processed. The task returns the recognized text as well as the translation.
                    // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
                    // shot recognition like command or query.
                    // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
                    Console.WriteLine("Say something...");
                    var result = await recognizer.RecognizeOnceAsync();

                    // Checks result.
                    if (result.Reason == ResultReason.TranslatedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED '{fromLanguage}': {result.Text}");
                        Console.WriteLine($"TRANSLATED into '{toLanguage}': {result.Translations[toLanguage]}");
                        finalVal = result.Translations[toLanguage];
                    }
                    else if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED '{fromLanguage}': {result.Text} (text could not be translated)");
                    }
                    else if (result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = CancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                    }
                    Message2 = finalVal;
                    ViewData["Message2"] = finalVal;
                }
            }
        }
        public string Message { get; set; }
        public async Task OnGetAsync()
        {
            Message = "Your application description page.";
            string Kvalue = "";
            int retries = 0;
            bool retry = false;
            try
            {
                /* The next four lines of code show you how to use AppAuthentication library to fetch secrets from your key vault */
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var secret = await  keyVaultClient.GetSecretAsync("https://cognitivedemokeyvault.vault.azure.net/secrets/AppSecret").ConfigureAwait(false);
            
                Kvalue = secret.Value;
                Message = secret.Value;
            }
            /* If you have throttling errors see this tutorial https://docs.microsoft.com/azure/key-vault/tutorial-net-create-vault-azure-web-app */
            /// <exception cref="KeyVaultErrorException">
            /// Thrown when the operation returned an invalid status code
            /// </exception>
            catch (KeyVaultErrorException keyVaultException)
            {
                Message = keyVaultException.Message;
            }
          // return Kvalue;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
