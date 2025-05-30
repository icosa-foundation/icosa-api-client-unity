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

using System.IO;
using UnityEngine;

namespace IcosaClientInternal
{
    public static class AttributionGeneration
    {
        /// <summary>
        /// Name of the static attribution file.
        /// </summary>
        public static readonly string ATTRIB_FILE_NAME = "IcosaAttributions.txt";

        /// <summary>
        /// File header.
        /// </summary>
        public static readonly string FILE_HEADER =
            "This project uses the following items from Icosa Gallery (http://icosa.gallery):";

        /// <summary>
        /// Information on the Creative Commons license for the attribution file.
        /// </summary>
        /// <summary>
        /// Returns a formatted string of the attribution information.
        /// </summary>
        public static string GenerateAttributionString(string displayName, string authorName, string url,
            string license)
        {
            return string.Format("Title: {0}\nAuthor: {1}\nURL: {2}\nLicense: {3}", displayName, authorName,
                url, license);
        }
    }
}