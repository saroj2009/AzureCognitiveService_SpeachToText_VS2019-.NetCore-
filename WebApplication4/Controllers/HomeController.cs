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

namespace WebApplication4.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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

            // Creates an instance of a speech translation config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechTranslationConfig.FromSubscription("8fe93a8c99734245a2b103109ae0290f", "southcentralus");
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
                    path = "AudioFiles\\whatstheweatherlike.wav";
                else
                    path = "AudioFiles\\wreck-a-nice-beach.wav";
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
        // public IActionResult Index2()
        //{
        //    return View();
        //}

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
