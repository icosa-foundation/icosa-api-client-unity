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

using System;
using System.Collections.Generic;
using UnityEngine;
using IcosaClientInternal;
using System.Collections;

namespace IcosaApiClient
{
    /// <summary>
    /// Represents a Poly asset (the information about a 3D object in Poly).
    /// </summary>
    /// <remarks>
    /// This is not the actual object that is added to the scene. This is just a container for
    /// the object's data, from which a GameObject can eventually be constructed.
    /// </remarks>
    [AutoStringifiable]
    public class IcosaAsset
    {
        /// <summary>
        /// Format of the URL to a particular asset, given its ID.
        /// </summary>
        private const string URL_FORMAT = "https://icosa.gallery/view/{0}";

        /// <summary>
        /// For backwards compatibility with Poly API - same as assetId but with "assets/" prefix.
        /// For example, "assets/L1o2e3m4I5p6s7u8m".
        /// Not to be confused with displayName which is the human readable name
        /// </summary>
        public string name;

        /// <summary>
        /// Identifier for the asset. This is an alphanumeric string that identifies the asset,
        /// but is not meant for display. For example, "L1o2e3m4I5p6s7u8m".
        /// </summary>
        public string assetId;

        /// <summary>
        /// Human-readable name of the asset.
        /// </summary>
        public string displayName;

        /// <summary>
        /// Name of the asset's author.
        /// </summary>
        public string authorName;

        /// <summary>
        /// Human-readable description of the asset.
        /// </summary>
        public string description;

        /// <summary>
        /// Date and time when the asset was created.
        /// </summary>
        public DateTime createTime;

        /// <summary>
        /// Date and time when the asset was last updated.
        /// </summary>
        public DateTime updateTime;

        /// <summary>
        /// A list of the available formats for this asset. Each format describes a content-type of a
        /// representation of the asset, and specifies where the underlying data files can be found.
        /// </summary>
        public List<IcosaFormat> formats = new List<IcosaFormat>();

        /// <summary>
        /// Thumbnail image information for this asset.
        /// </summary>
        public IcosaFile thumbnail;

        /// <summary>
        /// The license under which the author has made this asset available for use, if any.
        /// </summary>
        public IcosaAssetLicense license;

        /// <summary>
        /// Visibility of this asset (who can access it).
        /// </summary>
        public IcosaVisibility visibility;

        /// <summary>
        /// If true, the asset was manually curated by the Poly team.
        /// </summary>
        public bool isCurated;

        /// <summary>
        /// The texture with the asset's thumbnail image. Only available after successfully fetched.
        /// </summary>
        public Texture2D thumbnailTexture;

        /// <summary>
        /// Returns a PolyFormat of the given type, if it exists.
        /// If the asset has more than one format of the given type, returns the first one seen.
        /// If the asset does not have a format of the given type, returns null.
        /// </summary>
        public IcosaFormat GetFormatIfExists(IcosaFormatType type)
        {
            foreach (IcosaFormat format in formats)
            {
                if (format == null)
                {
                    continue;
                }

                if (format.formatType == type) return format;
            }

            return null;
        }

        /// <summary>
        /// Returns whether the asset is known to be mutable, due to its visibility.
        /// Public and unlisted assets are immutable. Private assets are mutable.
        /// </summary>
        /// <remarks>
        /// Immutable assets can be cached indefinitely, since they can't be modified.
        /// Depending on your use-case, you may wish to frequently re-download mutable assets, if you expect them to be
        /// changed while your app is running.
        /// </remarks>
        public bool IsMutable
        {
            get { return visibility == IcosaVisibility.PRIVATE || visibility == IcosaVisibility.UNSPECIFIED; }
        }

        /// <summary>
        /// Returns the Poly url of the asset.
        /// </summary>
        public string Url
        {
            get { return string.Format(URL_FORMAT, assetId); }
        }

        /// <summary>
        /// Returns attribution information about the asset.
        /// </summary>
        public string AttributionInfo
        {
            get
            {
                string licenceText;
                switch (license)
                {
                    case IcosaAssetLicense.UNKNOWN:
                        licenceText = "Unknown";
                        break;
                    case IcosaAssetLicense.CREATIVE_COMMONS_BY:
                        licenceText = "Creative Commons CC-BY\n" +
                                      "https://creativecommons.org/licenses/by/3.0/legalcode";
                        break;
                    case IcosaAssetLicense.ALL_RIGHTS_RESERVED:
                        licenceText = "All Rights Reserved";
                        break;
                    case IcosaAssetLicense.CREATIVE_COMMONS_BY_ND:
                        licenceText = "Creative Commons CC-BY-ND\n" +
                                      "https://creativecommons.org/licenses/by-nd/3.0/legalcode";
                        break;
                    case IcosaAssetLicense.CREATIVE_COMMONS_BY_SA:
                        licenceText = "Creative Commons CC-BY-SA\n" +
                                      "https://creativecommons.org/licenses/by-sa/3.0/legalcode";
                        break;
                    case IcosaAssetLicense.CREATIVE_COMMONS_BY_NC:
                        licenceText = "Creative Commons CC-BY-NC\n" +
                                      "https://creativecommons.org/licenses/by-nc/3.0/legalcode";
                        break;
                    case IcosaAssetLicense.CREATIVE_COMMONS_BY_NC_ND:
                        licenceText = "Creative Commons CC-BY-NC-ND\n" +
                                      "https://creativecommons.org/licenses/by-nc-nd/3.0/legalcode";
                        break;
                    case IcosaAssetLicense.CREATIVE_COMMONS_BY_NC_SA:
                        licenceText = "Creative Commons CC-BY-NC-SA\n" +
                                      "https://creativecommons.org/licenses/by-nc-sa/3.0/legalcode";
                        break;
                    case IcosaAssetLicense.CC0:
                        licenceText = "Creative Commons CC0 (Public Domain)\n" +
                                      "https://creativecommons.org/publicdomain/zero/1.0/legalcode";
                        break;
                    default:
                        licenceText = "All Rights Reserved";
                        break;
                }

                return AttributionGeneration.GenerateAttributionString(displayName, authorName, Url, licenceText);
            }
        }

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// A specific representation of an asset, containing all the information needed to retrieve and
    /// describe this representation.
    /// </summary>
    /// <remarks>
    /// Each format is a "package" of files, with one root file and any number of resource files that accompany
    /// it. For example, for the OBJ format, the root file is the OBJ file that contains the asset's geometry
    /// and the corresponding MTL files are resource files.
    /// </remarks>
    [AutoStringifiable]
    public class IcosaFormat
    {
        /// <summary>
        /// Format type (OBJ, GLTF, etc).
        /// </summary>
        public IcosaFormatType formatType;

        /// <summary>
        /// The root (main) file for this format.
        /// </summary>
        public IcosaFile root;

        /// <summary>
        /// The list of resource (auxiliary) files for this format.
        /// </summary>
        public List<IcosaFile> resources = new List<IcosaFile>();

        /// <summary>
        /// Complexity of this format.
        /// </summary>
        public IcosaFormatComplexity formatComplexity;

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// Represents a Poly file.
    /// </summary>
    [AutoStringifiable]
    public class IcosaFile
    {
        /// <summary>
        /// The relative path of the file in the local filesystem when it was uploaded.
        /// For resource files, the path is relative to the root file. This always includes the name fo the
        /// file, and may or may not include a directory path.
        /// </summary>
        public string relativePath;

        /// <summary>
        /// The URL at which the contents of this file can be retrieved.
        /// </summary>
        public string url;

        /// <summary>
        /// The content type of this file. For example, "text/plain".
        /// </summary>
        public string contentType;

        /// <summary>
        /// Binary contents of this file. Only available after fetched.
        /// </summary>
        [AutoStringifyAbridged] public byte[] contents;

        /// <summary>
        /// Cached text contents of this file (lazily decoded from binary).
        /// </summary>
        [AutoStringifyAbridged] private string text;

        public IcosaFile(string relativePath, string url, string contentType)
        {
            this.relativePath = relativePath;
            this.url = url;
            this.contentType = contentType;
        }

        /// <summary>
        /// Returns the contents of this file as text.
        /// </summary>
        public string Text
        {
            get
            {
                if (text == null) text = System.Text.Encoding.UTF8.GetString(contents);
                return text;
            }
        }

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// Information on the complexity of a format.
    /// </summary>
    [AutoStringifiable]
    public class IcosaFormatComplexity
    {
        /// <summary>
        /// Approximate number of triangles in the asset's geometry.
        /// </summary>
        public long triangleCount;

        /// <summary>
        /// Hint for the level of detail (LOD) of this format relative to the other formats in this
        /// same asset. 0 is the most detailed version.
        /// </summary>
        public int lodHint;

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// Possible format types that can be returned from the Poly REST API.
    /// </summary>
    public enum IcosaFormatType
    {
        UNKNOWN = 0,
        OBJ = 1,
        GLTF = 2,
        GLTF_2 = 3,
        TILT = 4,
    }

    /// <summary>
    /// Possible asset licenses.
    /// </summary>
    public enum IcosaAssetLicense
    {
        UNKNOWN = 0,
        CREATIVE_COMMONS_BY = 1,
        ALL_RIGHTS_RESERVED = 2,
        CREATIVE_COMMONS_BY_ND = 3,
        CREATIVE_COMMONS_BY_SA = 4,
        CREATIVE_COMMONS_BY_NC = 5,
        CREATIVE_COMMONS_BY_NC_ND = 6,
        CREATIVE_COMMONS_BY_NC_SA = 7,
        CC0 = 8
    }

    /// <summary>
    /// Visibility filters for a PolyListUserAssets request.
    /// </summary>
    public enum IcosaVisibilityFilter
    {
        /// <summary>
        /// No visibility specified. Returns all assets.
        /// </summary>
        UNSPECIFIED = 0,

        /// <summary>
        /// Return only private assets.
        /// </summary>
        PRIVATE = 1,

        /// <summary>
        /// Return only published assets, including unlisted assets.
        /// </summary>
        PUBLISHED = 2,
    }

    /// <summary>
    /// Visibility of a Poly asset.
    /// </summary>
    public enum IcosaVisibility
    {
        /// <summary>
        /// Unknown (and invalid) visibility.
        /// </summary>
        UNSPECIFIED = 0,

        /// <summary>
        /// Only the owner of the asset can access it.
        /// </summary>
        PRIVATE = 1,

        /// <summary>
        /// Read access to anyone who knows the asset ID (link to the asset), but the
        /// logged-in user's unlisted assets are returned in PolyListUserAssets.
        /// </summary>
        UNLISTED = 2,

        /// <summary>
        /// Read access for everyone.
        /// </summary>
        PUBLISHED = 3,
    }

    /// <summary>
    /// Category of a Poly asset.
    /// </summary>
    public enum IcosaCategory
    {
        UNSPECIFIED = 0,
        ANIMALS = 1,
        ARCHITECTURE = 2,
        ART = 3,
        FOOD = 4,
        NATURE = 5,
        OBJECTS = 6,
        PEOPLE = 7,
        PLACES = 8,
        TECH = 9,
        TRANSPORT = 10,
    }

    /// <summary>
    /// How the requested assets should be ordered in the response.
    /// </summary>
    public enum IcosaOrderBy
    {
        BEST,
        NEWEST,
        OLDEST,

        // Liked time is only a valid in a PolyListLikedAssetsRequest.
        LIKED_TIME
    }

    /// <summary>
    /// Options for filtering to return only assets that contain the given format.
    /// </summary>
    public enum IcosaFormatFilter
    {
        BLOCKS = 1,
        FBX = 2,
        GLTF = 3,
        GLTF_2 = 4,
        OBJ = 5,
        TILT = 6,
    }

    /// <summary>
    /// Options for filtering on the maximum complexity of the asset.
    /// </summary>
    public enum IcosaMaxComplexityFilter
    {
        UNSPECIFIED = 0,
        SIMPLE = 1,
        MEDIUM = 2,
        COMPLEX = 3,
    }

    /// <summary>
    /// Base class that all request types derive from.
    /// </summary>
    public abstract class IcosaRequest
    {
        /// <summary>
        /// How to sort the results.
        /// </summary>
        public IcosaOrderBy orderBy = IcosaOrderBy.NEWEST;

        /// <summary>
        /// Size of each returned page.
        /// </summary>
        public int pageSize = 45;

        /// <summary>
        /// Page continuation token for pagination.
        /// </summary>
        public string pageToken = null;
    }

    /// <summary>
    /// Represents a set of Poly request parameters determining which assets should be returned.
    /// null values mean "don't filter by this parameter".
    /// </summary>
    [AutoStringifiable]
    public class IcosaListAssetsRequest : IcosaRequest
    {
        public string keywords = "";
        public bool curated = false;

        /// <summary>
        /// Category can be any of the PolyCategory object categories (e.g. "PolyCategory.ANIMALS").
        /// </summary>
        public IcosaCategory category = IcosaCategory.UNSPECIFIED;

        public IcosaMaxComplexityFilter maxComplexity = IcosaMaxComplexityFilter.UNSPECIFIED;
        public IcosaFormatFilter? formatFilter = null;

        public IcosaListAssetsRequest()
        {
        }

        /// <summary>
        /// Returns a ListAssetsRequest that requests the featured assets. This approximates what the
        /// user would see in the Poly main page, but the ordering might be different.
        /// </summary>
        public static IcosaListAssetsRequest Featured()
        {
            IcosaListAssetsRequest featured = new IcosaListAssetsRequest();
            featured.curated = true;
            featured.orderBy = IcosaOrderBy.BEST;
            return featured;
        }

        /// <summary>
        /// Returns a ListAssetsRequest that requests the latest assets. This query is not curated,
        /// so it will return the latest assets regardless of whether they have been reviewed.
        /// If you wish to enable curation, set curated=true on the returned object.
        /// </summary>
        public static IcosaListAssetsRequest Latest()
        {
            IcosaListAssetsRequest latest = new IcosaListAssetsRequest();
            latest.orderBy = IcosaOrderBy.NEWEST;
            return latest;
        }

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// Represents a set of Poly request parameters determining which of the user's assets should be returned.
    /// null values mean "don't filter by this parameter".
    /// </summary>
    [AutoStringifiable]
    public class IcosaListUserAssetsRequest : IcosaRequest
    {
        public IcosaFormatType format = IcosaFormatType.UNKNOWN;
        public IcosaVisibilityFilter visibility = IcosaVisibilityFilter.UNSPECIFIED;
        public IcosaFormatFilter? formatFilter = null;

        public IcosaListUserAssetsRequest()
        {
        }

        /// <summary>
        /// Returns a ListUserAssetsRequest that requests the user's latest assets.
        /// </summary>
        public static IcosaListUserAssetsRequest MyNewest()
        {
            IcosaListUserAssetsRequest myNewest = new IcosaListUserAssetsRequest();
            myNewest.orderBy = IcosaOrderBy.NEWEST;
            return myNewest;
        }

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// Represents a set of Poly request parameters determining which liked assets should be returned.
    /// Currently, only requests for the liked assets of the logged in user are supported.
    /// null values mean "don't filter by this parameter".
    /// </summary>
    [AutoStringifiable]
    public class IcosaListLikedAssetsRequest : IcosaRequest
    {
        /// <summary>
        // A valid user id. Currently, only the special value 'me', representing the
        // currently-authenticated user is supported. To use 'me', you must pass
        // an OAuth token with the request.
        /// </summary>
        public string name = "me";

        public IcosaListLikedAssetsRequest()
        {
        }

        /// <summary>
        /// Returns a ListUserAssetsRequest that requests the user's most recently liked assets.
        /// </summary>
        public static IcosaListLikedAssetsRequest MyLiked()
        {
            IcosaListLikedAssetsRequest myLiked = new IcosaListLikedAssetsRequest();
            myLiked.orderBy = IcosaOrderBy.LIKED_TIME;
            return myLiked;
        }

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// Represents the status of an operation: success or failure + error message.
    ///
    /// A typical pattern is to return a IcosaStatus to indicate the success of an operation, instead of just a bool.
    /// So your code would do something like:
    ///
    /// @{
    /// IcosaStatus MyMethod() {
    ///   if (somethingWentWrong) {
    ///     return IcosaStatus.Error("Failed to reticulate spline.");
    ///   }
    ///   ...
    ///   return IcosaStatus.Success();
    /// }
    /// @}
    ///
    /// You can also chain IcosaStatus failures, using one IcosaStatus as the cause of another:
    ///
    /// @{
    /// IcosaStatus MyMethod() {
    ///   IcosaStatus status = TesselateParabolicNonUniformDarkMatterQuantumSuperManifoldWithCheese();
    ///   if (!status.ok) {
    ///     return IcosaStatus.Error(status, "Tesselation failure.");
    ///   }
    ///   ...
    ///   return IcosaStatus.Success();
    /// }
    /// @}
    ///
    /// Using IcosaStatus vs. throwing exceptions: IcosaStatus typically represents an "expected" failure, that is,
    /// an operation where failure is common and acceptable. For example, validating user input, consuming some
    /// external file which might or might not be well formatted, sending a web request, etc. For unexpected
    /// failures (logic errors, assumption violations, etc), it's best to use exceptions.
    /// </summary>
    public struct IcosaStatus
    {
        /// <summary>
        /// Indicates whether the operation succeeded.
        /// </summary>
        public bool ok;

        /// <summary>
        /// If the operation failed, this is the error message. This is an error message suitable for
        /// logging, not necessarily a user-friendly message.
        /// </summary>
        public string errorMessage;

        /// <summary>
        /// Creates a new IcosaStatus with the given success status and error message.
        /// </summary>
        /// <param name="ok">Whether the operation succeeded.</param>
        /// <param name="errorMessage">The error message (only relevant if ok == false).</param>
        public IcosaStatus(bool ok, string errorMessage = "")
        {
            this.ok = ok;
            this.errorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a new success status.
        /// </summary>
        public static IcosaStatus Success()
        {
            return new IcosaStatus(true);
        }

        /// <summary>
        /// Creates a new error status with the given error message.
        /// </summary>
        public static IcosaStatus Error(string errorMessage)
        {
            return new IcosaStatus(false, errorMessage);
        }

        /// <summary>
        /// Creates a new error status with the given error message.
        /// </summary>
        public static IcosaStatus Error(string format, params object[] args)
        {
            return new IcosaStatus(false, string.Format(format, args));
        }

        /// <summary>
        /// Creates a new error status with the given error message and cause.
        /// The error message will automatically include all error messages in the causal chain.
        /// </summary>
        public static IcosaStatus Error(IcosaStatus cause, string errorMessage)
        {
            return new IcosaStatus(false, errorMessage + "\nCaused by: " + cause.errorMessage);
        }

        /// <summary>
        /// Creates a new error status with the given error message and cause.
        /// The error message will automatically include all error messages in the causal chain.
        /// </summary>
        public static IcosaStatus Error(IcosaStatus cause, string format, params object[] args)
        {
            return new IcosaStatus(false, string.Format(format, args) + "\nCaused by: " + cause.errorMessage);
        }

        public override string ToString()
        {
            return ok ? "OK" : string.Format("ERROR: {0}", errorMessage);
        }
    }

    /// <summary>
    /// A union of a IcosaStatus and a type. Used to represent the result of an operation, which can either
    /// be an error (represented as a IcosaStatus), or a result object (the parameter type T).
    /// </summary>
    /// <typeparam name="T">The result object.</typeparam>
    public class IcosaStatusOr<T>
    {
        private IcosaStatus status;
        private T value;

        /// <summary>
        /// Creates a IcosaStatusOr with the given error status.
        /// </summary>
        /// <param name="status">The error status with which to create it.</param>
        public IcosaStatusOr(IcosaStatus status)
        {
            if (status.ok)
            {
                throw new Exception("IcosaStatusOr(IcosaStatus) can only be used with an error status.");
            }

            this.status = status;
            this.value = default(T);
        }

        /// <summary>
        /// Creates a IcosaStatusOr with the given value.
        /// The status will be set to success.
        /// </summary>
        /// <param name="value">The value with which to create it.</param>
        public IcosaStatusOr(T value)
        {
            this.status = IcosaStatus.Success();
            this.value = value;
        }

        /// <summary>
        /// Returns the status.
        /// </summary>
        public IcosaStatus Status
        {
            get { return status; }
        }

        /// <summary>
        /// Shortcut to Status.ok.
        /// </summary>
        public bool Ok
        {
            get { return status.ok; }
        }

        /// <summary>
        /// Returns the value. The value can only be obtained if the status is successful. If the status
        /// is an error, reading this property will throw an exception.
        /// </summary>
        public T Value
        {
            get
            {
                if (!status.ok)
                {
                    throw new Exception("Can't get value from an unsuccessful IcosaStatusOr: " + this);
                }

                return value;
            }
        }

        public override string ToString()
        {
            return string.Format("IcosaStatusOr<{0}>: {1}{2}", typeof(T).Name, status,
                status.ok ? (value == null ? "(null)" : value.ToString()) : "");
        }
    }

    /// <summary>
    /// Base class for all result types.
    /// </summary>
    public abstract class IcosaBaseResult
    {
        /// <summary>
        /// The status of the operation (success or failure).
        /// </summary>
        public IcosaStatus status;
    }

    /// <summary>
    /// Represents the result of a PolyListAssetsRequest or PolyListUserAssetsRequest.
    /// </summary>
    [AutoStringifiable]
    public class IcosaListAssetsResult : IcosaBaseResult
    {
        /// <summary>
        /// A list of assets that match the criteria specified in the request.
        /// </summary>
        public List<IcosaAsset> assets;

        /// <summary>
        /// The total number of assets in the list, without pagination.
        /// </summary>
        public int totalSize;

        /// <summary>
        /// The token to retrieve the next page of results, if any.
        /// If there is no next page, this will be null.
        /// </summary>
        public string nextPageToken;

        public IcosaListAssetsResult(IcosaStatus status, int totalSize = 0, List<IcosaAsset> assets = null,
            string nextPageToken = null)
        {
            this.status = status;
            this.assets = assets;
            this.totalSize = totalSize;
            this.nextPageToken = nextPageToken;
        }

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// Represents the result of importing an asset.
    /// </summary>
    public class IcosaImportResult
    {
        /// <summary>
        /// The GameObject representing the imported asset.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// The main thread throttler object, if importing in "throttled" mode. This will be null if not
        /// in throttled mode. Enumerate this on the main thread to gradually perform necessary main
        /// thread operations like creating meshes, textures, etc (see documentation for PolyImportOptions for
        /// more details).
        ///
        /// IMPORTANT: this enumerator is not designed to be used across scene (level) loads. Always finish
        /// enumerating it before loading a new scene.
        /// </summary>
        public IEnumerable mainThreadThrottler;

        public IcosaImportResult(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }
    }

    /// <summary>
    /// Represents the result of fetching files for an asset.
    /// </summary>
    [AutoStringifiable]
    public class IcosaFormatTypeFetchResult : IcosaBaseResult
    {
        public IcosaAsset asset;

        public IcosaFormatTypeFetchResult(IcosaStatus status, IcosaAsset asset)
        {
            this.status = status;
            this.asset = asset;
        }

        public override string ToString()
        {
            return AutoStringify.Stringify(this);
        }
    }

    /// <summary>
    /// Options for fetching a thumbnail.
    /// </summary>
    public class IcosaFetchThumbnailOptions
    {
        /// <summary>
        /// If nonzero, this is the requested thumbnail image size, in pixels. This is the size
        /// of the image's largest dimension (width, for most thumbnails).
        /// This is just a hint that the implementation will try (but is not guaranteed) to honor.
        /// </summary>
        public int requestedImageSize { get; private set; }

        public IcosaFetchThumbnailOptions()
        {
        }

        public void SetRequestedImageSize(int requestedImageSize)
        {
            this.requestedImageSize = requestedImageSize;
        }
    }
}