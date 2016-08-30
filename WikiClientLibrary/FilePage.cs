﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WikiClientLibrary.Client;

namespace WikiClientLibrary
{
    /// <summary>
    /// Represents a file page on MediaWiki site.
    /// </summary>
    public class FilePage : Page
    {
        // Use FilePage to distinguish from System.IO.File
        public FilePage(Site site, string title) : base(site, title, BuiltInNamespaces.File)
        {
        }

        internal FilePage(Site site) : base(site)
        {
        }

        /// <summary>
        /// Asynchronously uploads a file from an external URL.
        /// </summary>
        /// <param name="site">MedaiWiki site.</param>
        /// <param name="url">The URL of the file to be uploaded.</param>
        /// <param name="title">Title of the file, with or without File: prefix.</param>
        /// <param name="comment">Comment of the upload, as well as the page content if it doesn't exist.</param>
        /// <param name="ignoreWarnings">Ignore any warnings. This must be set to upload a new version of an existing image.</param>
        /// <exception cref="UploadException">
        /// There's warning from server.
        /// Check <see cref="UploadException.UploadResult"/> for the warning message or continuing the upload.
        /// </exception>
        /// <exception cref="OperationFailedException"> There's an error while uploading the file. </exception>
        /// <exception cref="TimeoutException">
        /// Timeout specified in <see cref="WikiClient.Timeout"/> has been reached. Note in this
        /// invocation, there will be no retries.
        /// </exception>
        /// <returns>An <see cref="UploadResult"/>.</returns>
        /// <remarks>
        /// Upload from external source may take a while, so be sure to set a long <see cref="WikiClient.Timeout"/>
        /// in case the response from the server is delayed.
        /// </remarks>
        public static Task<UploadResult> UploadAsync(Site site, string url, string title,
            string comment, bool ignoreWarnings)
        {
            return UploadAsyncInternal(site, url, title, comment, ignoreWarnings, AutoWatchBehavior.Default);
        }

        /// <summary>
        /// Asynchronously uploads a file, again.
        /// </summary>
        /// <param name="site">MedaiWiki site.</param>
        /// <param name="previousResult">An <see cref="UploadResult"/> returned from previous failed (or warned) upload.</param>
        /// <param name="title">Title of the file, with or without File: prefix.</param>
        /// <param name="comment">Comment of the upload, as well as the page content if it doesn't exist.</param>
        /// <param name="ignoreWarnings">Ignore any warnings. This must be set to upload a new version of an existing image.</param>
        /// <exception cref="UploadException">
        /// There's warning from server.
        /// Check <see cref="UploadException.UploadResult"/> for the warning message or continuing the upload.
        /// </exception>
        /// <exception cref="OperationFailedException"> There's an error while uploading the file. </exception>
        /// <exception cref="TimeoutException">
        /// Timeout specified in <see cref="WikiClient.Timeout"/> has been reached. Note in this
        /// invocation, there will be no retries.
        /// </exception>
        /// <returns>An <see cref="UploadResult"/>.</returns>
        /// <remarks>
        /// You should have obtained the previous upload result via <see cref="UploadException.UploadResult"/>.
        /// </remarks>
        public static Task<UploadResult> UploadAsync(Site site, UploadResult previousResult, string title,
            string comment, bool ignoreWarnings)
        {
            return UploadAsyncInternal(site, previousResult, title, comment, ignoreWarnings, AutoWatchBehavior.Default);
        }

        /// <summary>
        /// Asynchronously uploads a file.
        /// </summary>
        /// <param name="site">MedaiWiki site.</param>
        /// <param name="content">Content of the file.</param>
        /// <param name="title">Title of the file, with or without File: prefix.</param>
        /// <param name="comment">Comment of the upload, as well as the page content if it doesn't exist.</param>
        /// <param name="ignoreWarnings">Ignore any warnings. This must be set to upload a new version of an existing image.</param>
        /// <exception cref="UploadException">
        /// There's warning from server.
        /// Check <see cref="UploadException.UploadResult"/> for the warning message or continuing the upload.
        /// </exception>
        /// <exception cref="OperationFailedException"> There's an error while uploading the file. </exception>
        /// <exception cref="TimeoutException">
        /// Timeout specified in <see cref="WikiClient.Timeout"/> has been reached. Note in this
        /// invocation, there will be no retries.
        /// </exception>
        /// <returns>An <see cref="UploadResult"/>.</returns>
        public static Task<UploadResult> UploadAsync(Site site, Stream content, string title,
            string comment, bool ignoreWarnings)
        {
            return UploadAsyncInternal(site, content, title, comment, ignoreWarnings, AutoWatchBehavior.Default);
        }

        /// <summary>
        /// Asynchronously uploads a file.
        /// </summary>
        /// <param name="site">MedaiWiki site.</param>
        /// <param name="content">Content of the file.</param>
        /// <param name="title">Title of the file, with or without File: prefix.</param>
        /// <param name="comment">Comment of the upload, as well as the page content if it doesn't exist.</param>
        /// <param name="ignoreWarnings">Ignore any warnings. This must be set to upload a new version of an existing image.</param>
        /// <param name="watch">Whether to add the file into your watchlist.</param>
        /// <exception cref="UploadException">
        /// There's warning from server.
        /// Check <see cref="UploadException.UploadResult"/> for the warning message or continuing the upload.
        /// </exception>
        /// <exception cref="OperationFailedException"> There's an error while uploading the file. </exception>
        /// <exception cref="TimeoutException">
        /// Timeout specified in <see cref="WikiClient.Timeout"/> has been reached. Note in this
        /// invocation, there will be no retries.
        /// </exception>
        /// <returns>An <see cref="UploadResult"/>.</returns>
        public static Task<UploadResult> UploadAsync(Site site, Stream content, string title,
            string comment, bool ignoreWarnings, AutoWatchBehavior watch)
        {
            return UploadAsyncInternal(site, content, title, comment, ignoreWarnings, watch);
        }

        //content can be
        //  Stream          file content
        //  string          url to fetch
        //  UploadResult    the previous failed upload
        private static async Task<UploadResult> UploadAsyncInternal(Site site, object content, string title, string comment,
            bool ignoreWarnings, AutoWatchBehavior watch)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (title == null) throw new ArgumentNullException(nameof(title));
            var link = new WikiLink(site, title);
            if (link.Namespace.Id != BuiltInNamespaces.File)
                throw new ArgumentException($"Invalid namespace for file title: {title} .", nameof(title));
            var token = await site.GetTokenAsync("edit");
            var requestContent = new MultipartFormDataContent
            {
                {new StringContent("json"), "format"},
                {new StringContent("upload"), "action"},
                {new StringContent(token), "token"},
                {new StringContent(link.Title), "filename"},
                {new StringContent(comment), "comment"},
            };
            if (content is Stream)
                requestContent.Add(new StreamContent((Stream) content), "file", title);
            else if (content is string)
                requestContent.Add(new StringContent((string) content), "url");
            else if (content is UploadResult)
            {
                var key = ((UploadResult) content).FileKey;
                if (string.IsNullOrEmpty(key))
                    throw new InvalidOperationException("The specified UploadResult has no valid FileKey.");
                // sessionkey: Same as filekey, maintained for backward compatibility (deprecated in 1.18)
                requestContent.Add(new StringContent(key),
                    site.SiteInfo.Version >= new Version(1, 18) ? "filekey" : "sessionkey");
            }
            else
                Debug.Assert(false);
            if (ignoreWarnings) requestContent.Add(new StringContent(""), "ignorewarnings");
            site.Logger?.Trace($"Uploading: {link.Title} .");
            var jresult = await site.WikiClient.GetJsonAsync(requestContent);
            var result = jresult["upload"].ToObject<UploadResult>(Utility.WikiJsonSerializer);
            site.Logger?.Trace($"Upload[{link.Title}]: {result}.");
            switch (result.ResultCode)
            {
                case UploadResultCode.Warning:
                    throw new UploadException(result);
                default:
                    // UploadResult.Result setter should have thrown an exception.
                    Debug.Assert(result.ResultCode == UploadResultCode.Success ||
                                 result.ResultCode == UploadResultCode.Continue);
                    break;
            }
            return result;
        }

        protected override void OnLoadPageInfo(JObject jpage)
        {
            base.OnLoadPageInfo(jpage);
            var jfile = jpage["imageinfo"];
            var lastRev = jfile.LastOrDefault();
            if (lastRev != null)
            {
                LastFileRevision = lastRev.ToObject<FileRevision>(Utility.WikiJsonSerializer);
            }
            else
            {
                // Possibly not a valid file.
                LastFileRevision = null;
            }
        }

        public FileRevision LastFileRevision { get; private set; }
    }

    /// <summary>
    /// Represents a revision of a file or image.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class FileRevision
    {
        /// <summary>
        /// The time and date of the revision.
        /// </summary>
        [JsonProperty]
        public DateTime TimeStamp { get; private set; }

        [JsonProperty("user")]
        public string UserName { get; private set; }

        [JsonProperty]
        public string Comment { get; private set; }

        /// <summary>
        /// Url of the file.
        /// </summary>
        [JsonProperty]
        public string Url { get; private set; }

        /// <summary>
        /// Url of the description page.
        /// </summary>
        [JsonProperty]
        public string DescriptionUrl { get; private set; }

        /// <summary>
        /// Size of the file. In bytes.
        /// </summary>
        [JsonProperty]
        public int Size { get; private set; }

        [JsonProperty]
        public int? Width { get; private set; }

        [JsonProperty]
        public int Height { get; private set; }

        /// <summary>
        /// The file's SHA-1 hash.
        /// </summary>
        [JsonProperty]
        public string Sha1 { get; private set; }
    }

    /// <summary>
    /// Contains the result from server after an upload operation.
    /// </summary>
    /// <remarks>See https://www.mediawiki.org/wiki/API:Upload .</remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public class UploadResult
    {
        private static readonly Dictionary<string, string> UploadWarnings = new Dictionary<string, string>
        {
            // Referenced from pywikibot, site.py
            // map API warning codes to user error messages
            // {0} will be replaced by message string from API responsse
            {"duplicate-archive", "The file is a duplicate of a deleted file {0}."},
            {"was-deleted", "The file {0} was previously deleted."},
            {"emptyfile", "File {0} is empty."},
            {"exists", "File {0} already exists."},
            {"duplicate", "Uploaded file is a duplicate of {0}."},
            {"badfilename", "Target filename {0} is invalid."},
            {"filetype-unwanted-type", "File {0} type is unwanted type."},
            {"exists-normalized", "File exists with different extension as \"{0}\"."},
        };

        private static readonly KeyValuePair<string, string>[] EmptyWarnings = {};

        /// <summary>
        /// Try to convert the specified warning code and context into a user-fridendly
        /// warning message.
        /// </summary>
        /// <param name="warningCode">Case-sensitive warning code.</param>
        /// <param name="context">The extra content of the warning.</param>
        /// <returns>
        /// It tries to match the warningCode with well-known ones, and returns a
        /// user-fridendly warning message. If there's no match, a string containing
        /// warningCode and context will be returned.
        /// </returns>
        public static string FormatWarning(string warningCode, string context)
        {
            string msg;
            if (UploadWarnings.TryGetValue(warningCode, out msg))
                return string.Format(msg, warningCode, context);
            return $"{warningCode}: {context}";
        }

        /// <summary>
        /// A brief word describing the result of the operation.
        /// </summary>
        public UploadResultCode ResultCode { get; private set; }

        [JsonProperty("result")]
        private string Result
        {
            set
            {
                switch (value)
                {
                    case "Success":
                        ResultCode = UploadResultCode.Success;
                        break;
                    case "Warning":
                        ResultCode = UploadResultCode.Warning;
                        break;
                    case "Continue":
                        ResultCode = UploadResultCode.Continue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown result: " + value);
                }
            }
        }

        [JsonProperty]
        public string FileKey { get; private set; }

        // Same as filekey, maintained for backward compatibility (deprecated in 1.18)
        [JsonProperty]
        private string SessionKey
        {
            set
            {
                if (FileKey == null) FileKey = value;
                else Debug.Assert(FileKey == value);
            }
        }

        /// <summary>
        /// Gets a list of key-value pairs, indicating the warning code and its context.
        /// </summary>
        /// <value>
        /// A readonly list of warning code - context pairs.
        /// The list is guaranteed not to be <c>null</c>,
        /// but it can be empty.
        /// </value>
        /// <remarks>
        /// <para>You can use <see cref="FormatWarning"/> to get user-friendly warning messages.</para>
        /// <para>If you have supressed warnings, the warnings will still be here, but <see cref="ResultCode"/> will be <see cref="UploadResultCode.Success"/>.</para>
        /// </remarks>
        public IList<KeyValuePair<string, string>> Warnings { get; private set; } = EmptyWarnings;

        [JsonProperty("warnings")]
        private JObject RawWarnings
        {
            set
            {
                var l = value.Properties().Select(p => new KeyValuePair<string, string>(p.Name, (string) p.Value)).ToList();
                Warnings = new ReadOnlyCollection<KeyValuePair<string, string>>(l);
            }
        }

        /// <summary>
        /// 返回表示当前对象的字符串。
        /// </summary>
        /// <returns>
        /// 表示当前对象的字符串。
        /// </returns>
        public override string ToString()
        {
            return $"{ResultCode}; {string.Join(",", Warnings.Select(p => p.Key + "=" + p.Value))}";
        }
    }

    /// <summary>
    /// General results of an upload operation.
    /// </summary>
    public enum UploadResultCode
    {
        Success = 0,
        Warning,
        Continue
    }
}