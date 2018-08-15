/*
 * Copyright 2012 ZXing.Net authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Threading;

using ZXing;

namespace CommandLineDecoder
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                printUsage();
                return;
            }

            Config config = new Config();
            Inputs inputs = new Inputs();
            config.AutoRotate = true;
            foreach (var arg in args)
            {
                if (arg.StartsWith("--crop"))
                {
                    int[] crop = new int[4];
                    String[] tokens = arg.Substring(7).Split(',');
                    for (int i = 0; i < crop.Length; i++)
                    {
                        crop[i] = int.Parse(tokens[i]);
                    }
                    config.Crop = crop;
                }
                else if (arg.StartsWith("--threads") && arg.Length >= 10)
                {
                    int threadsCount = int.Parse(arg.Substring(10));
                    if (threadsCount > 1)
                    {
                        config.Threads = threadsCount;
                    }
                }
                else if (arg.StartsWith("-"))
                {
                    Console.Error.WriteLine("Unknown command line option " + arg);
                    printUsage();
                    return;
                }
            }

            config.Hints = buildHints(config);

            foreach (var arg in args)
            {
                if (!arg.StartsWith("--"))
                {
                    addArgumentToInputs(arg, config, inputs);
                }
            }

            var threads = new Dictionary<Thread, DecodeThread>(Math.Min(config.Threads, inputs.getInputCount()));
            var decodeObjects = new List<DecodeThread>();
            for (int x = 0; x < config.Threads; x++)
            {
                var decodeThread = new DecodeThread(config, inputs);
                decodeObjects.Add(decodeThread);
                var thread = new Thread(decodeThread.run);
                threads.Add(thread, decodeThread);
                thread.Start();
            }

            foreach (var thread in threads.Keys)
            {
                thread.Join();
                threads[thread].getSuccessful();
            }
        }

        // Build all the inputs up front into a single flat list, so the threads can atomically pull
        // paths/URLs off the queue.
        private static void addArgumentToInputs(String argument, Config config, Inputs inputs)
        {
            inputs.addInput(argument);
        }

        // Manually turn on all formats, even those not yet considered production quality.
        private static IDictionary<DecodeHintType, object> buildHints(Config config)
        {
            var hints = new Dictionary<DecodeHintType, Object>();
            var vector = new List<BarcodeFormat>(8)
                    {
                       BarcodeFormat.UPC_A,
                       BarcodeFormat.UPC_E,
                       BarcodeFormat.EAN_13,
                       BarcodeFormat.EAN_8,
                       BarcodeFormat.RSS_14,
                       BarcodeFormat.CODABAR,
                       BarcodeFormat.RSS_EXPANDED,
                       BarcodeFormat.CODE_39
                    };
            hints[DecodeHintType.POSSIBLE_FORMATS] = vector;
            hints[DecodeHintType.TRY_HARDER] = true;
            return hints;
        }

        private static void printUsage()
        {
            Console.Out.WriteLine("Decode barcode images using the ZXing library\n");
            Console.Out.WriteLine("usage: CommandLineRunner { file | dir | url } [ options ]");
            Console.Out.WriteLine("  --crop=left,top,width,height: Only examine cropped region of input image(s)");
            Console.Out.WriteLine("  --threads=n: The number of threads to use while decoding");
        }
    }
}
