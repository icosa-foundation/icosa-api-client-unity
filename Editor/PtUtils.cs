// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.IO;
using System.Text;
using IcosaApiClient;
using IcosaClientInternal;
using UnityEditor;

namespace IcosaClientEditor
{
    public static class PtUtils
    {
        /// <summary>
        /// Name of the Icosa API Client manifest file.
        /// </summary>
        private const string MANIFEST_FILE_NAME = "icosa_toolkit_manifest.dat";

        /// <summary>
        /// Icosa API Client base path (normally Assets/PolyToolkit, unless the user moved it).
        /// This is computed lazily.
        /// </summary>
        private static string basePath = null;

        /// <summary>
        /// Normalizes a Unity local asset path: trims, converts back slashes into forward slashes,
        /// removes trailing slash.
        /// </summary>
        /// <param name="path">The path to normalize (e.g., " Assets\Foo/Bar\Qux  ").</param>
        /// <returns>The normalized path (e.g., "Assets/Foo/Bar/Qux")</returns>
        public static string NormalizeLocalPath(string path)
        {
            path = path.Trim().Replace('\\', '/');
            // Strip the trailing slash, if there is one.
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
            return path;
        }

        /// <summary>
        /// Converts a local path (like "Assets/Foo/Bar") into an absolute system-dependent path
        /// (like "C:\Users\foo\MyUnityProject\Assets\Foo\Bar").
        /// </summary>
        /// <param name="localPath">The local path to convert.</param>
        /// <returns>The absolute path.</returns>
        public static string ToAbsolutePath(string localPath)
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.dataPath).Replace('/', Path.DirectorySeparatorChar),
                localPath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Sanitizes the given string to use as a file name.
        /// </summary>
        /// <param name="str">The string to sanitize</param>
        /// <returns>The sanitized version, with invalid characters converted to _.</returns>
        public static string SanitizeToUseAsFileName(string str)
        {
            if (str == null)
            {
                throw new System.Exception("Can't sanitize a null string");
            }

            StringBuilder sb = new StringBuilder();
            bool lastCharElided = false;
            for (int i = 0; i < Mathf.Min(str.Length, 40); i++)
            {
                char c = str[i];
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    lastCharElided = false;
                }
                else
                {
                    if (lastCharElided) continue;
                    c = '_';
                    lastCharElided = true;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns the default asset path for the given asset.
        /// </summary>
        public static string GetDefaultPtAssetPath(IcosaAsset asset)
        {
            return string.Format("{0}/{1}.asset",
                NormalizeLocalPath(PtSettings.Instance.assetObjectsPath),
                GetPtAssetBaseName(asset));
        }

        public static string GetPtAssetBaseName(IcosaAsset asset)
        {
            return string.Format("{0}_{1}_{2}",
                SanitizeToUseAsFileName(asset.displayName),
                SanitizeToUseAsFileName(asset.authorName),
                SanitizeToUseAsFileName(asset.assetId).Replace("assets_", ""));
        }

        public static string GetPtBaseLocalPath()
        {
            if (basePath != null) return basePath;
            // Get the root path of the project. Something like C:\Foo\Bar\MyUnityProject
            string rootPath = Path.GetDirectoryName(Application.dataPath);
            // Find the icosa_toolkit_manifest.data file. That marks the installation path of Icosa API Client.
            string[] matches = Directory.GetFiles(Path.Combine(Application.dataPath, "../Packages"), MANIFEST_FILE_NAME,
                SearchOption.AllDirectories);
            if (matches == null || matches.Length == 0)
            {
                throw new System.Exception(
                    "Could not find base directory for Icosa API Client (icosa_toolkit_manifest.data missing).");
            }
            else if (matches.Length > 1)
            {
                Debug.LogError(
                    "Found more than one Icosa API Client installation in your project. Make sure there is only one.");
                // Continue anyway (by "best effort" -- arbitrarily use the first one).
            }

            // Found it. Now we have calculate the path relative to rootPath. For that, we have to normalize the
            // separators because on Windows we normally get an inconsistent mix of '/' and '\'.
            rootPath = rootPath.Replace('\\', '/');
            if (!rootPath.EndsWith("/")) rootPath += "/";
            string manifestPath = matches[0].Replace('\\', '/').Replace(MANIFEST_FILE_NAME, "");
            // Now rootPath is something like "C:/Foo/Bar/MyUnityProject/"
            // and manifestPath is something like "C:/Foo/Bar/MyUnityProject/Some/Path/IcosaClient".
            // We want to extract the "Some/Path/IcosaClient" part.
            if (!manifestPath.StartsWith(rootPath))
            {
                throw new System.Exception(string.Format("Could not find local path from '{0}' (data path is '{1}')",
                    matches[0], Application.dataPath));
            }

            // Cache it.
            basePath = Path.Combine(manifestPath, "..");
            basePath = Path.GetFullPath(basePath);
            return basePath;
        }

        /// <summary>
        /// Loads a texture file from the Icosa API Client installation folder given a relative path
        /// from the installation folder to the texture.
        /// </summary>
        /// <param name="relativePath">The relative path were the texture is located. For example,
        /// this could be Editor/Textures/IcosaClientTitle.png.</param>
        /// <returns>The texture.</returns>
        public static Texture2D LoadTexture2DFromRelativePath(string relativePath)
        {
            var path = $"Packages/com.icosa.icosa-api-client-unity/{relativePath}";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}