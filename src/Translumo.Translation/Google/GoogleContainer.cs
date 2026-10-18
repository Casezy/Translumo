using System.Net;
using Translumo.Translation.Configuration;
using Translumo.Utils.Http;

namespace Translumo.Translation.Google
{
    public sealed class GoogleContainer : TranslationContainer
    {
        public HttpReader Reader { get; set; }

        public GoogleContainer(Proxy proxy = null, bool isPrimary = false) : base(proxy, isPrimary)
        {
            Reader = CreateReader(proxy);
        }

        public override void Block()
        {
            base.Block();
            Reader.Cookies = new CookieContainer();
        }

        private HttpReader CreateReader(Proxy proxy)
        {
            var httpReader = new HttpReader();
            httpReader.Proxy = proxy?.ToWebProxy();

            httpReader.ContentType = null;
            httpReader.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";
            httpReader.Accept = "*/*";

            httpReader.OptionalHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            httpReader.OptionalHeaders.Add("Cache-Control", "no-cache");
            httpReader.OptionalHeaders.Add("Referer", "https://translate.google.com/");

            return httpReader;
        }
    }
}
