using LemmaSharp;
using Semantic.API.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ZeroLevel.Services.Semantic;
using ZeroLevel.Services.Web;
using ZeroLevel.WebAPI;

namespace Semantic.API.WebAPI
{
    [RequestFirewall]
    public sealed class LemmaController : BaseController
    {
        public class LemmaLexer : ILexer
        {
            private readonly ILemmatizer _lemmatizer;

            public LemmaLexer()
            {
                _lemmatizer = new LemmatizerPrebuiltFull(LanguagePrebuilt.Russian);
            }

            public string Lex(string word) { return _lemmatizer.Lemmatize(word.Trim().ToLowerInvariant()); }
        }      

        private readonly static ILexProvider _provider = LexProviderFactory.CreateProvider(new LemmaLexer());

        public LemmaController()
        {
        }

        #region GET
        [HttpGet]
        [Route("api/lemma")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetLemmas(HttpRequestMessage request)
        {
            try
            {
                var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
                var text = keys.ContainsKey("text") ? keys["text"] : string.Empty;
                return request.CreateSelfDestroyingResponse(_provider.ExtractLexTokens(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/lemma/unique")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetUniqueLemmas(HttpRequestMessage request)
        {
            try
            {
                var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
                var text = keys.ContainsKey("text") ? keys["text"] : string.Empty;
                return request.CreateSelfDestroyingResponse(_provider.ExtractUniqueLexTokens(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/lemma/clean")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetUniqueCleanLemmas(HttpRequestMessage request)
        {
            try
            {
                var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
                var text = keys.ContainsKey("text") ? keys["text"] : string.Empty;
                return request.CreateSelfDestroyingResponse(_provider.ExtractUniqueLexTokensWithoutStopWords(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/lemma/occurences/words")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken>>))]
        public HttpResponseMessage GetLemmasByWordsOccerencesInText(HttpRequestMessage request, [FromUri]string text, [FromUri]string[] words)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.SearchLexTokensByWords(text, words));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/lemma/occurences/phrases")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken[]>>))]
        public HttpResponseMessage GetLemmasByPhrasesOccerencesInText(HttpRequestMessage request, [FromUri]string text, [FromUri]string[] words)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.SearchLexTokensByPhrases(text, words));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }
        #endregion

        #region POST
        [HttpPost]
        [Route("api/lemma")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostLemmas(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.ExtractLexTokens(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/lemma/unique")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostUniqueLemmas(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.ExtractUniqueLexTokens(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/lemma/clean")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostUniqueCleanLemmas(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.ExtractUniqueLexTokensWithoutStopWords(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/lemma/occurences/words")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken>>))]
        public HttpResponseMessage PostLemmasByWordsOccerencesInText(HttpRequestMessage request, [FromBody]WordsSearchRequest query)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.SearchLexTokensByWords(query.Text, query.Words));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/lemma/occurences/phrases")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken[]>>))]
        public HttpResponseMessage PostLemmasByPhrasesOccerencesInText(HttpRequestMessage request, [FromBody]WordsSearchRequest query)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(_provider.SearchLexTokensByPhrases(query.Text, query.Words));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }
        #endregion
    }
}
