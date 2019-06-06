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
    public sealed class TextController : BaseController
    {
        private readonly static ILexProvider _provider = LexProviderFactory.CreateSimpleTextProvider();

        #region GET
        [HttpGet]
        [Route("api/text/words")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetTextWords(HttpRequestMessage request)
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
        [Route("api/text/words/unique")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetTextUniqueWords(HttpRequestMessage request)
        {
            try
            {
                var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
                var text = keys.ContainsKey("text") ? keys["text"] : string.Empty;
                return request.CreateSelfDestroyingResponse(
                    _provider.ExtractUniqueLexTokens(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/text/words/clean")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage GetTextUniqueCleanWords(HttpRequestMessage request)
        {
            try
            {
                var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
                var text = keys.ContainsKey("text") ? keys["text"] : string.Empty;
                return request.CreateSelfDestroyingResponse(
                    _provider.ExtractUniqueLexTokensWithoutStopWords(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/text/occurences/words")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken>>))]
        public HttpResponseMessage GetWordsOccerencesInText(HttpRequestMessage request, [FromUri]string text, [FromUri]string[] words)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.SearchLexTokensByWords(text, words));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpGet]
        [Route("api/text/occurences/phrases")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken[]>>))]
        public HttpResponseMessage GetPhrasesOccerencesInText(HttpRequestMessage request, [FromUri]string text, [FromUri]string[] words)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.SearchLexTokensByPhrases(text, words));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex); 
            }
        }
        #endregion

        #region POST
        [HttpPost]
        [Route("api/text/words")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostTextWords(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.ExtractLexTokens(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/text/words/unique")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostTextUniqueWords(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.ExtractUniqueLexTokens(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/text/words/clean")]
        [ResponseType(typeof(IEnumerable<LexToken>))]
        public HttpResponseMessage PostTextUniqueCleanWords(HttpRequestMessage request, [FromBody]string text)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.ExtractUniqueLexTokensWithoutStopWords(text));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/text/occurences/words")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken>>))]
        public HttpResponseMessage PostWordsOccerencesInText(HttpRequestMessage request, [FromBody]WordsSearchRequest query)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.SearchLexTokensByWords(query.Text, query.Words));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }

        [HttpPost]
        [Route("api/text/occurences/phrases")]
        [ResponseType(typeof(IDictionary<string, IEnumerable<LexToken[]>>))]
        public HttpResponseMessage PostPhrasesOccerencesInText(HttpRequestMessage request, [FromBody]WordsSearchRequest query)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(
                    _provider.SearchLexTokensByPhrases(query.Text, query.Words));
            }
            catch(Exception ex)
            {
                return BadRequestAnswer(request, ex);
            }
        }
        #endregion
    }
}
