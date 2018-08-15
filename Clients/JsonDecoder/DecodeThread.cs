/*
 * Copyright 2011 ZXing authors
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
using System.Drawing;
using System.IO;

using ZXing;

namespace CommandLineDecoder
{
    /// <summary>
    /// One of a pool of threads which pulls images off the Inputs queue and decodes them in parallel.
    /// @see CommandLineRunner
    /// </summary>
    internal sealed class DecodeThread
    {
        private int successful;
        private readonly Config config;
        private readonly Inputs inputs;
        public string ResultString { get; private set; }

        public DecodeThread(Config config, Inputs inputs)
        {
            this.config = config;
            this.inputs = inputs;
        }

        public void run()
        {
            ResultString = String.Empty;
            while (true)
            {
                String input = inputs.getNextInput();
                if (input == null)
                {
                    break;
                }
                try
                {
                    Result[] results = decodeMulti(new Uri(Path.GetFullPath(input)), input, config.Hints);
                    if (results != null)
                    {
                        successful++;
                    }
                }
                catch (IOException exc)
                {
                    Console.WriteLine(exc.ToString());
                }
            }
        }

        public int getSuccessful()
        {
            return successful;
        }

        private static void dumpResultMulti(String input, Result[] results)
        {
            int pos = input.LastIndexOf('.');
            if (pos > 0)
            {
                input = input.Substring(0, pos);
            }
            using (var stream = File.CreateText(input + ".txt"))
            {
                foreach (var result in results)
                {
                    stream.WriteLine(result.Text);
                }
            }
        }

        private Result[] decodeMulti(Uri uri, string originalInput, IDictionary<DecodeHintType, object> hints)
        {
            Bitmap image;
            try
            {
                image = (Bitmap)Bitmap.FromFile(uri.LocalPath);
            }
            catch (Exception)
            {
                throw new FileNotFoundException("Resource not found: " + uri);
            }

            using (image)
            {
                LuminanceSource source;
                if (config.Crop == null)
                {
                    source = new BitmapLuminanceSource(image);
                }
                else
                {
                    int[] crop = config.Crop;
                    source = new BitmapLuminanceSource(image).crop(crop[0], crop[1], crop[2], crop[3]);
                }

                var reader = new BarcodeReader { AutoRotate = config.AutoRotate,TryInverted = true };
                foreach (var entry in hints)
                    reader.Options.Hints.Add(entry.Key, entry.Value);
                Result[] results = reader.DecodeMultiple(source);
                if (results != null && results.Length > 0)
                {
                    foreach (var result in results)
                    {
                        var points = "";
                        for (int i = 0; i < result.ResultPoints.Length; i++)
                        {
                            if (i > 0)
                            {
                                points += ",";
                            }
                            ResultPoint rp = result.ResultPoints[i];
                            points += "{\"x\":" + rp.X + ",\"y\":" + rp.Y + "}";
                        }
                        var resultString = "{\"type\":\"" + result.BarcodeFormat + "\",\"data\":\"" + result.Text + "\",\"orientation\":" + result.ResultMetadata[ResultMetadataType.ORIENTATION] + ",\"points\":["+points+"]}";
                        Console.Out.WriteLine(resultString);
                    }
                    return results;
                }
                return null;
            }
        }

    }
}