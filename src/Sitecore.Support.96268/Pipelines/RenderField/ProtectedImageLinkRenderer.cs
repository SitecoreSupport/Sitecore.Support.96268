using System;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.RenderField;
using Sitecore.Resources.Media;
using System.Text;

namespace Sitecore.Support.Pipelines.RenderField
{
    public class ProtectedImageLinkRenderer
    {
        // Fields
        private readonly char[] quotes = new char[] { '\'', '"' };
        private static readonly string[] srcAttrs = new string[] { "src", "href" };

        // Methods
        protected bool CheckReferenceForParams(string renderedText, int tagStart)
        {
            Assert.ArgumentNotNull(renderedText, "renderedText");
            renderedText = renderedText.Replace("&amp;", "&");
            string str = srcAttrs.First<string>(p => renderedText.Contains(p + "="));
            int startIndex = renderedText.IndexOf(str, tagStart, StringComparison.OrdinalIgnoreCase) + str.Length;
            startIndex = renderedText.IndexOfAny(this.quotes, startIndex) + 1;
            int num2 = renderedText.IndexOfAny(this.quotes, startIndex);
            int num3 = renderedText.IndexOf('?', startIndex, num2 - startIndex);
            return ((num3 >= 0) && this.ContainsUnsafeParametersInQuery(renderedText.Substring(num3, num2 - num3)));
        }

        // the metod was added to the initial version of the patch to fix the issue, which appears when <a> tag does not contain "src" or "href" attributes.
        protected bool CheckReferenceForAttributes(string wholeTag)
        {
            Assert.ArgumentNotNull(wholeTag, "wholeTag");
            bool flag = false;
            for (int i = 0; i < srcAttrs.Length; i++)
            {
                if (wholeTag.Contains(srcAttrs[i] + "="))
                {
                    flag = true;
                }
            }
            return flag;
        }

        protected virtual bool ContainsUnsafeParametersInQuery(string urlParameters)
        {
            return !HashingUtils.IsSafeUrl(urlParameters);
        }

        protected virtual string GetProtectedUrl(string url)
        {
            Assert.IsNotNull(url, "url");
            return HashingUtils.ProtectAssetUrl(url);
        }

        protected string HashImageReferences(string renderedText)
        {
            Assert.ArgumentNotNull(renderedText, "renderedText");
            string str = "<img ";
            if (renderedText.IndexOf(str, StringComparison.OrdinalIgnoreCase) < 0)
            {
                str = "<a ";
                if (renderedText.IndexOf(str, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return renderedText;
                }
            }
            int startIndex = 0;
            bool flag = false;
            while ((startIndex < renderedText.Length) && !flag)
            {
                int tagStart = renderedText.IndexOf(str, startIndex, StringComparison.OrdinalIgnoreCase);
                if (tagStart < 0)
                {
                    break;
                }
                flag = this.CheckReferenceForParams(renderedText, tagStart);
                startIndex = renderedText.IndexOf(">", tagStart, StringComparison.OrdinalIgnoreCase) + 1;
            }
            if (!flag)
            {
                return renderedText;
            }
            startIndex = 0;
            StringBuilder builder = new StringBuilder(renderedText.Length + 0x80);
            while (startIndex < renderedText.Length)
            {
                int num3 = renderedText.IndexOf(str, startIndex, StringComparison.OrdinalIgnoreCase);
                if (num3 > -1)
                {
                    int num4 = renderedText.IndexOf(">", num3, StringComparison.OrdinalIgnoreCase) + 1;
                    builder.Append(renderedText.Substring(startIndex, num3 - startIndex));
                    string imgTag = renderedText.Substring(num3, num4 - num3);

                    //  Check whether "src" or "href" attributes exist in the tag

                    if (CheckReferenceForAttributes(imgTag))
                    {
                        imgTag = this.ReplaceReference(imgTag);
                    }
                    builder.Append(imgTag);
                    startIndex = num4;
                }
                else
                {
                    builder.Append(renderedText.Substring(startIndex, renderedText.Length - startIndex));
                    startIndex = 0x7fffffff;
                }
            }
            return builder.ToString();
        }

        public void Process(RenderFieldArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (Settings.Media.RequestProtection.Enabled && !args.FieldTypeKey.StartsWith("__"))
            {
                args.Result.FirstPart = this.HashImageReferences(args.Result.FirstPart);
                args.Result.LastPart = this.HashImageReferences(args.Result.LastPart);
            }
        }

        private string ReplaceReference(string imgTag)
        {
            Assert.ArgumentNotNull(imgTag, "imgTag");
            bool flag = true;
            string str = imgTag;
            if (imgTag.Contains("&amp;"))
            {
                str = str.Replace("&amp;", "&");
            }
            else if (imgTag.Contains("&"))
            {
                flag = false;
            }
            string str2 = srcAttrs.First<string>(p => imgTag.Contains(p + "="));
            int startIndex = imgTag.IndexOf(str2, StringComparison.OrdinalIgnoreCase) + str2.Length;
            startIndex = str.IndexOfAny(this.quotes, startIndex) + 1;
            int num2 = str.IndexOfAny(this.quotes, startIndex);
            string url = str.Substring(startIndex, num2 - startIndex);
            if (!url.Contains("?"))
            {
                return imgTag;
            }
            url = this.GetProtectedUrl(url);
            if (flag)
            {
                url = url.Replace("&", "&amp;");
            }
            return (str.Substring(0, startIndex) + url + str.Substring(num2, str.Length - num2));
        }
    }
}