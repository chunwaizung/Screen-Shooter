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
using System.Collections;
using System.IO;
using Borodar.ScreenShooter.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using GameViewSizeType = Borodar.ScreenShooter.Utils.GameViewUtil.GameViewSizeType;
using Format = Borodar.ScreenShooter.ScreenshotConfig.Format;

namespace Borodar.ScreenShooter
{    
    public class ScreenShooterWindow : EditorWindow
    {
        public const string RESOURCE_NAME = "ScreenShooterSettings";

        private static readonly string[] _fileTypes = { "PNG", "JPG" };

        private ScreenShooterSettings _settings;
        private ReorderableList _list;        

        //---------------------------------------------------------------------
        // Messages
        //---------------------------------------------------------------------

        [MenuItem("Window/Screen Shooter")]
        protected static void ShowWindow()
        {
            var window = (ScreenShooterWindow) GetWindow(typeof(ScreenShooterWindow));
            window.autoRepaintOnSceneChange = true;
            window.titleContent = new GUIContent("Screen Shooter");
            window.Show();
        }

        protected void OnEnable()
        {
            _settings = ScreenShooterSettings.Load();

            // Init reorderable list if required
            if (_list == null)
            {
                _list = new ReorderableList(_settings.ScreenshotConfigs, typeof(ScreenshotConfig), true, false, true, true)
                {
                    elementHeight = EditorGUIUtility.singleLineHeight + 4,
                    drawElementCallback = (position, index, isActive, isFocused) =>
                    {
                        const float textWidth = 12f;
                        const float dimensionWidth = 45f;
                        const float typeWidth = 45f;
                        const float space = 10f;

                        var element = _settings.ScreenshotConfigs[index];
                        var nameWidth = position.width - space - textWidth - 2 * dimensionWidth - space - typeWidth;

                        position.y += 2;
                        position.width = nameWidth;
                        position.height -= 4;
                        element.Name = EditorGUI.TextField(position, element.Name);

                        position.x += position.width + space;
                        position.width = dimensionWidth;
                        element.Width = EditorGUI.IntField(position, element.Width);

                        position.x += position.width;
                        position.width = textWidth;
                        EditorGUI.LabelField(position, "x");

                        position.x += position.width;
                        position.width = dimensionWidth;
                        element.Height = EditorGUI.IntField(position, element.Height);

                        position.x += position.width + space;
                        position.width = typeWidth;
                        element.Type = (Format)EditorGUI.Popup(position, (int)element.Type, _fileTypes);
                    }
                };
            }
        }

        protected void OnGUI()
        {
            GUI.changed = false;

            // -- Camera ------------------------------------------------

            GUILayout.Label("Camera", EditorStyles.boldLabel);
            _settings.Camera = (Camera) EditorGUILayout.ObjectField(_settings.Camera, typeof (Camera), true);
            EditorGUILayout.Space();

            // -- Screenshots -------------------------------------------

            GUILayout.Label("Screenshots", EditorStyles.boldLabel);
            _list.DoLayoutList();
            EditorGUILayout.Space();

            // -- Save Folder --------------------------------------------

            GUILayout.Label("Save To", EditorStyles.boldLabel);
            _settings.SaveFolder = EditorGUILayout.TextField(_settings.SaveFolder);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = Directory.Exists(_settings.SaveFolder);
            if (GUILayout.Button("Show", GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL("file://" + _settings.SaveFolder);
            }
            GUI.enabled = true;

            if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
            {
                _settings.SaveFolder = EditorUtility.SaveFolderPanel("Save screenshots to:", _settings.SaveFolder, Application.dataPath);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // -- Take Button ---------------------------------------------

            GUI.backgroundColor = new Color(0.5f, 0.8f, 0.77f);
            if (GUILayout.Button("Take Screenshots"))
            {
                    EditorCoroutine.Start(TakeScreenshots());
            }

            if (GUI.changed) EditorUtility.SetDirty(_settings);
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private IEnumerator TakeScreenshots()
        {
            var currentIndex = GameViewUtil.GetCurrentSizeIndex();

            foreach (var data in _settings.ScreenshotConfigs)
            {
                // apply custom resolution for game view
                var sizeType = GameViewSizeType.FixedResolution;
                var sizeGroupType = GameViewUtil.GetCurrentGroupType();
                var sizeName = "scr_" + data.Width + "x" + data.Height;

                if (!GameViewUtil.IsSizeExist(sizeGroupType, sizeName))
                {
                    GameViewUtil.AddCustomSize(sizeType, sizeGroupType, data.Width, data.Height, sizeName);
                }

                var index = GameViewUtil.FindSizeIndex(sizeGroupType, sizeName);
                GameViewUtil.SetSizeByIndex(index);

                // add some delay while applying changes
                var lastFrameTime = EditorApplication.timeSinceStartup;
                while (EditorApplication.timeSinceStartup - lastFrameTime < 0.1f) yield return null;

                TakeScreenshot(_settings.SaveFolder, data);

                // just clean it up
                GameViewUtil.RemoveCustomSize(sizeGroupType, index);
            }

            GameViewUtil.SetSizeByIndex(currentIndex);
        }

        private void TakeScreenshot(string folderName, ScreenshotConfig screenshotConfig)
        {
            var camera = _settings.Camera;
            var scrTexture = new Texture2D(screenshotConfig.Width, screenshotConfig.Height, TextureFormat.RGB24, false);
            var scrRenderTexture = new RenderTexture(scrTexture.width, scrTexture.height, 24);
            var camRenderTexture = camera.targetTexture;

            camera.targetTexture = scrRenderTexture;
            camera.Render();
            camera.targetTexture = camRenderTexture;

            RenderTexture.active = scrRenderTexture;
            scrTexture.ReadPixels(new Rect(0, 0, scrTexture.width, scrTexture.height), 0, 0);
            scrTexture.Apply();

            SaveTextureAsFile(scrTexture, folderName, screenshotConfig);
        }

        private static void SaveTextureAsFile(Texture2D texture, string folder, ScreenshotConfig screenshotConfig)
        {
            byte[] bytes;
            string extension;

            switch (screenshotConfig.Type)
            {
                case Format.PNG:
                    bytes = texture.EncodeToPNG();
                    extension = ".png";
                    break;
                case Format.JPG:
                    bytes = texture.EncodeToJPG();
                    extension = ".jpg";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var fileName = screenshotConfig.Name + "." + screenshotConfig.Width + "x" + screenshotConfig.Height;
            var imageFilePath = folder + "/" + fileName + extension;

            // ReSharper disable once PossibleNullReferenceException
            (new FileInfo(imageFilePath)).Directory.Create();
            File.WriteAllBytes(imageFilePath, bytes);

            Debug.Log("Image saved to: " + imageFilePath);
        }
    }
}