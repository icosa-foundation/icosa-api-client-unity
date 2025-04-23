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

using IcosaApiClient;
using UnityEngine;
using UnityEngine.Networking;

namespace IcosaClientInternal
{
    /// <summary>
    /// Fetches and converts a thumbnail for a particular given asset.
    /// </summary>
    public class ThumbnailFetcher
    {
        /// <summary>
        /// Maximum cache age for thumbnails of immutable assets.
        /// </summary>
        private const long CACHE_MAX_AGE_MILLIS = 14 * 24 * 60 * 60 * 1000L; // A fortnight.

        private const int MIN_REQUESTED_SIZE = 32;
        private const int MAX_REQUESTED_SIZE = 512;

        private IcosaAsset asset;
        private IcosaFetchThumbnailOptions options;
        private IcosaApi.FetchThumbnailCallback callback;

        /// <summary>
        /// Builds a ThumbnailFetcher that will fetch the thumbnail for the given asset
        /// and call the given callback when done. Building this object doesn't immediately
        /// start the fetch. To start, call Fetch().
        /// </summary>
        /// <param name="asset">The asset to fetch the thumbnail for.</param>
        /// <param name="callback">The callback to call when done. Can be null.</param>
        public ThumbnailFetcher(IcosaAsset asset, IcosaFetchThumbnailOptions options,
            IcosaApi.FetchThumbnailCallback callback)
        {
            this.asset = asset;
            this.options = options ?? new IcosaFetchThumbnailOptions();
            this.callback = callback;
        }

        /// <summary>
        /// Starts fetching the thumbnail (in the background).
        /// </summary>
        public void Fetch()
        {
            if (asset.thumbnail == null || string.IsNullOrEmpty(asset.thumbnail.url))
            {
                // Spoiler alert: if there's no thumbnail URL, our web request will fail, because
                // the URL is kind of an import part of a web request.
                // So fail early with a clear error message, rather than make a broken web request.
                if (callback != null)
                {
                    callback(asset, IcosaStatus.Error("Thumbnail URL not available for asset: {0}", asset));
                }

                return;
            }

            // Only use cache if fetching the thumbnail for an immutable asset.
            long cacheAgeMaxMillis = asset.IsMutable ? 0 : CACHE_MAX_AGE_MILLIS;
            IcosaMainInternal.Instance.webRequestManager.EnqueueRequest(MakeRequest, ProcessResponse,
                cacheAgeMaxMillis);
        }

        private UnityWebRequest MakeRequest()
        {
            string url = asset.thumbnail.url;
            // If an image size hint was provided, forward it to the server if the server supports it.
            if (options.requestedImageSize > 0 && url.Contains(".googleusercontent.com/"))
            {
                url += "=s" + Mathf.Clamp(options.requestedImageSize, MIN_REQUESTED_SIZE, MAX_REQUESTED_SIZE);
            }

            return IcosaMainInternal.Instance.IcosaClient.GetRequest(url);
        }

        private void ProcessResponse(IcosaStatus status, int responseCode, byte[] data)
        {
            if (data == null || data.Length <= 0)
            {
                status = IcosaStatus.Error("Thumbnail data was null or empty.");
            }

            if (status.ok)
            {
                asset.thumbnailTexture = new Texture2D(1, 1);
                asset.thumbnailTexture.LoadImage(data);
            }
            else
            {
                Debug.LogWarningFormat("Failed to fetch thumbnail for asset {0} ({1}): {2}",
                    asset.assetId, asset.displayName, status);
            }

            if (callback != null)
            {
                callback(asset, status);
            }
        }
    }
}