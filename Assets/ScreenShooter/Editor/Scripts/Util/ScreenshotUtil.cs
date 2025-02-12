﻿/*
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using System;
using System.IO;
using Borodar.ScreenShooter.Configs;
using UnityEngine;

namespace Borodar.ScreenShooter.Utils
{
    public static class ScreenshotUtil
    {
        public static void TakeScreenshot(ScreenShooterSettings settings, ScreenshotConfig config)
        {
            var suffix = settings.AppendTimestamp ? "." + DateTime.Now.ToString("yyyyMMddHHmmssfff") : "";
            TakeScreenshot(settings.SaveFolder, settings.Tag, suffix, config);
        }

        public static void TakeScreenshot(string folderName, string prefix, string suffix, ScreenshotConfig screenshotConfig)
        {
            SaveScreenShotTextureAsFile(folderName, prefix, suffix, screenshotConfig);
        }

        public static void SaveTextureAsFile(Texture2D texture, string folder, string prefix, string suffix, ScreenshotConfig screenshotConfig)
        {
            byte[] bytes;
            string extension;

            switch (screenshotConfig.Type)
            {
                case ScreenshotConfig.Format.PNG:
                    bytes = texture.EncodeToPNG();
                    extension = ".png";
                    break;
                case ScreenshotConfig.Format.JPG:
                    bytes = texture.EncodeToJPG();
                    extension = ".jpg";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var fileName = prefix + screenshotConfig.Name + "." + screenshotConfig.Width + "x" + screenshotConfig.Height + suffix;
            var imageFilePath = folder + "/" + MakeValidFileName(fileName + extension);

            // ReSharper disable once PossibleNullReferenceException
            (new FileInfo(imageFilePath)).Directory.Create();
            File.WriteAllBytes(imageFilePath, bytes);

            Debug.Log("Image saved to: " + imageFilePath);
        }
        
        public static void SaveScreenShotTextureAsFile(string folder, string prefix, string suffix, ScreenshotConfig screenshotConfig)
        {
            string extension;

            switch (screenshotConfig.Type)
            {
                case ScreenshotConfig.Format.PNG:
                    extension = ".png";
                    break;
                case ScreenshotConfig.Format.JPG:
                    extension = ".jpg";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var fileName = prefix + screenshotConfig.Name + "." + screenshotConfig.Width + "x" + screenshotConfig.Height + suffix;
            var imageFilePath = folder + "/" + MakeValidFileName(fileName + extension);
            UnityEngine.ScreenCapture.CaptureScreenshot(imageFilePath);
        }

        private static string MakeValidFileName(string name)
        {
            var invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }
    }
}