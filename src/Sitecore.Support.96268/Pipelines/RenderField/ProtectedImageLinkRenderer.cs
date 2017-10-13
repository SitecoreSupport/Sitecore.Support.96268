namespace Sitecore.Support.Pipelines.RenderField
{
    using System;
    using Sitecore.Configuration;
    using Sitecore.Diagnostics;
    using Sitecore.Pipelines.RenderField;
    using Sitecore.Resources.Media;
    using System.Text;

    public class ProtectedImageLinkRenderer
    {
        /// <summary>
        /// The quotes
        /// </summary>
        private readonly char[] quotes = new char[]
        {
            '\'',
            '"'
        };

        /// <summary>
        /// Runs the processor.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Process(RenderFieldArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!Settings.Media.RequestProtection.Enabled)
            {
                return;
            }
            if (args.FieldTypeKey.StartsWith("__"))
            {
                return;
            }
            args.Result.FirstPart = this.HashReferences(args.Result.FirstPart);
            args.Result.LastPart = this.HashReferences(args.Result.LastPart);
        }

        /// <summary>
        /// Gets the protected URL.
        /// </summary>
        /// <param name="url">The URL to protect.</param>
        /// <returns>The protected by hash parameter URL.</returns>
        protected virtual string GetProtectedUrl(string url)
        {
            Assert.IsNotNull(url, "url");
            return HashingUtils.ProtectAssetUrl(url);
        }

        /// <summary>
        /// Hashes the references.
        /// </summary>
        /// <param name="renderedText">The rendered text.</param>
        /// <returns>Fixed references</returns>
        protected string HashReferences(string renderedText)
        {
            Assert.ArgumentNotNull(renderedText, "renderedText");
            return this.HashLinkReferences(this.HashImageReferences(renderedText));
        }

        /// <summary>
        /// Hashes the image references.
        /// </summary>
        /// <param name="renderedText">The rendered text.</param>
        /// <returns>Fixed image references</returns>
        protected string HashImageReferences(string renderedText)
        {
            Assert.ArgumentNotNull(renderedText, "renderedText");
            return this.HashReferences(renderedText, "img", "src");
        }

        /// <summary>
        /// Hashes the anchor references.
        /// </summary>
        /// <param name="renderedText">The rendered text.</param>
        /// <returns>Fixed anchor references</returns>
        protected string HashLinkReferences(string renderedText)
        {
            Assert.ArgumentNotNull(renderedText, "renderedText");
            return this.HashReferences(renderedText, "a", "href");
        }

        /// <summary>
        /// Hashes the references.
        /// </summary>
        /// <param name="renderedText">The rendered text.</param>
        /// <param name="tagName">tag name for the element contins a reference</param>
        /// <param name="urlAttribute">attribute within tag element that contains the reference url</param>
        /// <returns>Fixed image references</returns>
        protected string HashReferences(string renderedText, string tagName, string urlAttribute)
        {
            Assert.ArgumentNotNull(renderedText, "renderedText");
            Assert.ArgumentNotNull(tagName, "tagName");
            Assert.ArgumentNotNull(urlAttribute, "urlAttribute");
            string text = string.Format("<{0}", tagName);
            if (renderedText.IndexOf(string.Format("{0} ", text), StringComparison.OrdinalIgnoreCase) < 0)
            {
                return renderedText;
            }
            int i = 0;
            bool flag = false;
            while (i < renderedText.Length && !flag)
            {
                int num = renderedText.IndexOf(text, i, StringComparison.OrdinalIgnoreCase);
                if (num < 0)
                {
                    break;
                }
                flag = this.CheckReferenceForParams(renderedText, num, tagName, urlAttribute);
                int num2 = renderedText.IndexOf(">", num, StringComparison.OrdinalIgnoreCase) + 1;
                i = num2;
            }
            if (!flag)
            {
                return renderedText;
            }
            i = 0;
            StringBuilder stringBuilder = new StringBuilder(renderedText.Length + 128);
            while (i < renderedText.Length)
            {
                int num3 = renderedText.IndexOf(text, i, StringComparison.OrdinalIgnoreCase);
                if (num3 > -1)
                {
                    int num4 = renderedText.IndexOf(">", num3, StringComparison.OrdinalIgnoreCase) + 1;
                    stringBuilder.Append(renderedText.Substring(i, num3 - i));
                    string tagHtml = renderedText.Substring(num3, num4 - num3);
                    stringBuilder.Append(this.ReplaceReference(tagHtml, urlAttribute));
                    i = num4;
                }
                else
                {
                    stringBuilder.Append(renderedText.Substring(i, renderedText.Length - i));
                    i = 2147483647;
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Checks the reference for parameters.
        /// </summary>
        /// <param name="renderedText">The rendered text.</param>
        /// <param name="tagStartIndex">The tag start index.</param>
        /// <returns><c>True</c> is reference contains dangerous parameters, <c>false</c> otherwise</returns>
        protected bool CheckReferenceForParams(string renderedText, int tagStartIndex)
        {
            return this.CheckReferenceForParams(renderedText, tagStartIndex, "img", "src");
        }

        /// <summary>
        /// Checks the reference for parameters.
        /// </summary>
        /// <param name="renderedText">The rendered text.</param>
        /// <param name="tagStartIndex">The tag start index.</param>
        /// <param name="tagName">tag name</param>
        /// <param name="urlAttribute">url attribute name</param>
        /// <returns><c>True</c> is reference contains dangerous parameters, <c>false</c> otherwise</returns>
        protected bool CheckReferenceForParams(string renderedText, int tagStartIndex, string tagName, string urlAttribute)
        {
            Assert.ArgumentNotNull(renderedText, "renderedText");
            Assert.ArgumentNotNull(tagName, "tagName");
            Assert.ArgumentNotNull(urlAttribute, "urlAttribute");
            int num = renderedText.IndexOf(urlAttribute, tagStartIndex, StringComparison.OrdinalIgnoreCase) + 3;
            num = renderedText.IndexOfAny(this.quotes, num) + 1;
            int num2 = renderedText.IndexOfAny(this.quotes, num);
            int num3 = renderedText.IndexOf('?', num, num2 - num);
            return num3 >= 0 && this.ContainsUnsafeParametersInQuery(renderedText.Substring(num3, num2 - num3).Replace("&amp;", "&"));
        }

        /// <summary>
        /// Determines whether specified URL query parameters contain parameters to protect.
        /// </summary>
        /// <param name="urlParameters">The URL parameters.</param>
        /// <returns>
        ///   <c>true</c> if specified URL query parameters contain parameters to protect; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool ContainsUnsafeParametersInQuery(string urlParameters)
        {
            return !HashingUtils.IsSafeUrl(urlParameters);
        }

        /// <summary>
        /// Replaces the reference.
        /// </summary>
        /// <param name="tagHtml">The <c>img</c> tag.</param>
        /// <param name="urlAttribute">url attribute name</param>
        /// <returns>Fixed image reference</returns>
        private string ReplaceReference(string tagHtml, string urlAttribute)
        {
            Assert.ArgumentNotNull(tagHtml, "tagHtml");
            Assert.ArgumentNotNull(urlAttribute, "urlAttribute");
            bool flag = true;
            string text = tagHtml;
            if (tagHtml.Contains("&amp;"))
            {
                text = text.Replace("&amp;", "&");
            }
            else if (tagHtml.Contains("&"))
            {
                flag = false;
            }
            int num = text.IndexOf(urlAttribute, StringComparison.OrdinalIgnoreCase) + 3;
            num = text.IndexOfAny(this.quotes, num) + 1;
            int num2 = text.IndexOfAny(this.quotes, num);
            string text2 = text.Substring(num, num2 - num);
            if (!text2.Contains("?"))
            {
                return tagHtml;
            }
            text2 = this.GetProtectedUrl(text2);
            if (flag)
            {
                text2 = text2.Replace("&", "&amp;");
            }
            return text.Substring(0, num) + text2 + text.Substring(num2, text.Length - num2);
        }
    }
}